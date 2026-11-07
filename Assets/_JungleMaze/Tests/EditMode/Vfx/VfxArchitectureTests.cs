using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Vfx;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using JungleMaze.Presentation.Vfx;
using NUnit.Framework;
using UnityEngine;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>AC12/AC14: VFX code never writes HUD/inventory/effect state, never reads HP, never searches the scene.</summary>
    public sealed class VfxArchitectureTests
    {
        private static readonly Type[] Forbidden =
        {
            typeof(IHudStateWriter), typeof(IInventoryStateWriter), typeof(IEffectStateWriter), typeof(IHudState)
        };

        private static IEnumerable<Type> VfxTypes()
        {
            var assemblies = new[] { typeof(VfxCue).Assembly, typeof(VfxDirector).Assembly, typeof(VfxStage).Assembly };
            foreach (var asm in assemblies)
            foreach (var t in asm.GetTypes())
            {
                var ns = t.Namespace ?? "";
                if (ns.EndsWith(".Vfx") || ns.EndsWith(".Vfx.Mocks") || ns.EndsWith(".CombatFeedback") ||
                    ns.EndsWith(".Visibility") || ns.EndsWith(".Visibility.Mocks"))
                    yield return t;
            }
        }

        [Test]
        public void NoVfxType_DependsOnHudWritersOrHudState()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var offenders = new List<string>();
            foreach (var t in VfxTypes())
            {
                foreach (var f in t.GetFields(all))
                    if (Forbidden.Contains(f.FieldType)) offenders.Add(t.Name + "." + f.Name);
                foreach (var c in t.GetConstructors(all))
                foreach (var p in c.GetParameters())
                    if (Forbidden.Contains(p.ParameterType)) offenders.Add(t.Name + " ctor " + p.Name);
                foreach (var m in t.GetMethods(all))
                foreach (var p in m.GetParameters())
                    if (Forbidden.Contains(p.ParameterType)) offenders.Add(t.Name + "." + m.Name + " " + p.Name);
            }
            CollectionAssert.IsEmpty(offenders);
            Assert.Greater(VfxTypes().Count(), 20, "the scan found the VFX types");
        }

        private static IEnumerable<string> VfxSources()
        {
            var root = Path.Combine(Application.dataPath, "_JungleMaze");
            var dirs = new[]
            {
                "Scripts/Domain/Vfx", "Scripts/Domain/CombatFeedback", "Scripts/Application/Vfx", "Scripts/Application/Visibility",
                "Scripts/Presentation/Vfx", "Scripts/Bootstrap/Vfx" // runtime only: the editor sanitizer names ETFX to remove it
            };
            foreach (var d in dirs)
            {
                var path = Path.Combine(root, d);
                if (!Directory.Exists(path)) continue;
                foreach (var f in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)) yield return f;
            }
        }

        [Test]
        public void VfxSources_NoSceneSearch_NoImportedScripts_NoHp()
        {
            var offenders = new List<string>();
            foreach (var file in VfxSources())
            {
                var text = File.ReadAllText(file);
                foreach (var token in new[] { "FindObjectOfType", "FindAnyObjectByType", "FindObjectsByType", "FindFirstObjectByType", "ETFX", "OwnHp", ".Hp", "SetHp(" })
                    if (text.Contains(token)) offenders.Add(Path.GetFileName(file) + ": " + token);
            }
            CollectionAssert.IsEmpty(offenders);
            Assert.Greater(VfxSources().Count(), 20);
        }

        [Test]
        public void ContractEvents_AreStructs()
        {
            Assert.IsTrue(typeof(HitConfirmedEvent).IsValueType);
            Assert.IsTrue(typeof(ExplosionEvent).IsValueType);
            Assert.IsTrue(typeof(StatusEvent).IsValueType);
        }
    }
}
