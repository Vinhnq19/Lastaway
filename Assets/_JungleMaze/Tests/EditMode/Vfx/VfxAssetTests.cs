using System.Collections.Generic;
using System.Linq;
using JungleMaze.App.Vfx;
using JungleMaze.Domain.CombatFeedback;
using JungleMaze.Domain.Vfx;
using JungleMaze.Presentation.Vfx;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.Tests.Vfx
{
    /// <summary>AC1: every kept inventory row (plan 4.12.4) has its prefab, wired in the shipped catalog.</summary>
    public sealed class VfxCatalogTests
    {
        private const string Folder = "Assets/_JungleMaze/Prefabs/Vfx";

        /// <summary>Kept rows of 4.12.4 → prefab name (ImmunityVfx is a tint of the shield, no copy).</summary>
        private static readonly Dictionary<string, string> Kept = new Dictionary<string, string>
        {
            { "StunnedVfx", "Vfx_Status_Stun" },
            { "ReverseVfx", "Vfx_Status_Reverse" },
            { "ImmortalVfx", "Vfx_Status_Shield" },
            { "ImmunityVfx", "Vfx_Status_Shield" },
            { "PoisonsVfx", "Vfx_Status_Poison" },
            { "Epic Toon Prefabs 2D/Explosions", "Vfx_Hit_Spark" },
            { "Epic Toon Prefabs 2D/Explosions (small)", "Vfx_Attack_Puff" },
            { "new: area frame", "Vfx_FuseWarning" },
            { "new: TMP number", "Vfx_DamageNumber" },
            { "ImmortalVfx sparkles", "Vfx_Status_Invisible" }
        };

        private static VfxCatalog Catalog()
        {
            var set = Resources.Load<VfxRuntimeSet>(VfxRuntimeSet.ResourcePath);
            Assert.IsNotNull(set, "Resources/VfxRuntimeSet");
            Assert.IsTrue(set.IsComplete);
            return set.Catalog;
        }

        [Test]
        public void EveryKeptInventoryRowHasPrefab()
        {
            foreach (var row in Kept)
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<VfxParticleEffect>(Folder + "/" + row.Value + ".prefab"), row.Key + " → " + row.Value);
            var all = AssetDatabase.FindAssets("t:Prefab", new[] { Folder });
            Assert.AreEqual(9, all.Length, "exactly the nine VFX-01 prefabs (no blast/smoke/zone copies, D1/D2)");
        }

        [Test]
        public void ShippedCatalog_EveryCueHasAPrefab_AndStatusMapMatchesThePlan()
        {
            var catalog = Catalog();
            foreach (var cue in new[] { VfxCue.HitSpark, VfxCue.DamageNumber, VfxCue.AttackPuff, VfxCue.FuseWarning, VfxCue.AuraStun,
                         VfxCue.AuraReverse, VfxCue.AuraCcImmune, VfxCue.AuraInvulnerable, VfxCue.AuraInvisible, VfxCue.AuraPoison })
            {
                Assert.IsTrue(catalog.TryGetEntry(cue, out var entry), cue.ToString());
                Assert.IsNotNull(entry.prefab, cue.ToString());
                Assert.AreEqual(cue == VfxCue.FuseWarning ? "VFX" : "Default", entry.sortingLayer, cue + " layer (T-2)");
            }
            var rules = catalog.BuildRules();
            Assert.IsTrue(rules.TryGet(VfxCue.FuseWarning, out var fuse) && fuse.Priority == VfxPriority.Critical && fuse.Persistent);
            Assert.IsTrue(rules.TryGetStatus(StatusKind.Stun, out var c, out _) && c == VfxCue.AuraStun);
            Assert.IsTrue(rules.TryGetStatus(StatusKind.Root, out c, out _) && c == VfxCue.None, "vines are ThrowableView's");
            Assert.IsTrue(rules.TryGetStatus(StatusKind.Slow, out c, out _) && c == VfxCue.None);
            Assert.IsTrue(rules.TryGetStatus(StatusKind.Adrenaline, out c, out _) && c == VfxCue.None, "Q9: no P0 visual");
            Assert.IsTrue(rules.TryGetStatus(StatusKind.CcImmune, out c, out var white) && c == VfxCue.AuraCcImmune);
            Assert.IsTrue(rules.TryGetStatus(StatusKind.Invulnerable, out c, out var gold) && c == VfxCue.AuraInvulnerable);
            Assert.AreNotEqual(white, gold, "immune and invulnerable read differently");
            Assert.IsTrue(catalog.TryGetTint(white, out var w) && w.r > .9f && w.g > .9f && w.b > .9f, "CC immune aura is white (Equipments 13)");
            foreach (var kind in new[] { VfxCue.HitSpark, VfxCue.DamageNumber, VfxCue.AttackPuff })
                Assert.IsTrue(rules.TryGet(kind, out var r) && r.Priority == VfxPriority.Low && !r.Persistent);
        }

        [Test]
        public void TuningValues_MatchG1()
        {
            var t = Resources.Load<VfxRuntimeSet>(VfxRuntimeSet.ResourcePath).TuningValues;
            Assert.AreEqual(.16f, t.FlashSeconds, 1e-4f);
            Assert.AreEqual(.7f, t.NumberSeconds, 1e-4f);
            Assert.AreEqual(.6f, t.NumberRiseCells, 1e-4f);
            Assert.AreEqual(.15f, t.ShakeMaxCells, 1e-4f);
            Assert.AreEqual(.3f, t.ShakeSeconds, 1e-4f);
            Assert.AreEqual(5f, t.DedupeSeconds, 1e-4f);
            Assert.AreEqual(256, t.DedupeCapacity);
            Assert.AreEqual(64, t.Budget);
        }
    }

    /// <summary>AC9: every VFX-01 prefab is pool-safe 2D URP content (no Light, collider, imported script, mesh, Destroy).</summary>
    public sealed class VfxPrefabAuditTests
    {
        private static IEnumerable<string> Prefabs() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_JungleMaze/Prefabs/Vfx" }).Select(AssetDatabase.GUIDToAssetPath);

        private static string LayerFor(GameObject prefab)
        {
            var set = Resources.Load<VfxRuntimeSet>(VfxRuntimeSet.ResourcePath);
            foreach (var e in set.Catalog.Cues)
                if (e.prefab != null && e.prefab.gameObject == prefab) return e.sortingLayer;
            return null;
        }

        [Test]
        public void EveryPrefab_FollowsThe2DPoolRules()
        {
            var problems = new List<string>();
            foreach (var path in Prefabs())
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var name = go.name;
                if (go.GetComponent<VfxParticleEffect>() == null) problems.Add(name + ": no VfxParticleEffect");
                if (go.transform.localScale != Vector3.one) problems.Add(name + ": root scale");
                if (go.transform.localPosition.z != 0f) problems.Add(name + ": root z");
                if (go.GetComponentsInChildren<Light>(true).Length > 0) problems.Add(name + ": Light");
                if (go.GetComponentsInChildren<Collider>(true).Length + go.GetComponentsInChildren<Collider2D>(true).Length > 0) problems.Add(name + ": collider");
                if (go.GetComponentsInChildren<Rigidbody>(true).Length + go.GetComponentsInChildren<Rigidbody2D>(true).Length > 0) problems.Add(name + ": rigidbody");
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) { problems.Add(name + ": missing script"); continue; }
                    var script = MonoScript.FromMonoBehaviour(mb);
                    var sp = script != null ? AssetDatabase.GetAssetPath(script) : "";
                    if (sp.StartsWith("Assets/Imported/") || mb.GetType().Name.StartsWith("ETFX")) problems.Add(name + ": imported script " + mb.GetType().Name);
                }
                var systems = go.GetComponentsInChildren<ParticleSystem>(true);
                if (systems.Length > 4) problems.Add(name + ": more than 4 particle systems");
                foreach (var ps in systems)
                {
                    var main = ps.main;
                    if (main.stopAction == ParticleSystemStopAction.Destroy) problems.Add(name + "/" + ps.name + ": stopAction Destroy");
                    if (main.playOnAwake) problems.Add(name + "/" + ps.name + ": playOnAwake");
                    if (main.scalingMode != ParticleSystemScalingMode.Hierarchy) problems.Add(name + "/" + ps.name + ": scaling");
                    var r = ps.GetComponent<ParticleSystemRenderer>();
                    if (r != null && r.renderMode == ParticleSystemRenderMode.Mesh) problems.Add(name + "/" + ps.name + ": mesh render mode");
                }
                var layer = LayerFor(go);
                if (layer == null) problems.Add(name + ": not in the catalog");
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (layer != null && r.sortingLayerName != layer) problems.Add(name + "/" + r.name + ": layer " + r.sortingLayerName + " ≠ " + layer);
                    foreach (var m in r.sharedMaterials)
                    {
                        if (r is ParticleSystemRenderer psr && !psr.enabled) continue;
                        if (m == null) { problems.Add(name + "/" + r.name + ": null material"); continue; }
                        if (m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported)
                            problems.Add(name + "/" + r.name + ": bad shader " + (m.shader != null ? m.shader.name : "null"));
                    }
                }
            }
            CollectionAssert.IsEmpty(problems);
        }

        [Test]
        public void FuseFrame_IsTheAreaSize_WithinFivePercent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_JungleMaze/Prefabs/Vfx/Vfx_FuseWarning.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var fuse = instance.GetComponent<FuseWarningVfx>();
                foreach (var size in new[] { 3f, 7f })
                {
                    fuse.OnVfxBegin(new VfxSpawn(0f, 0f, size, size, seconds: 1.5f), Color.white, false, null);
                    Assert.AreEqual(size, fuse.Size.x, size * .05f);
                    Assert.AreEqual(size, fuse.Size.y, size * .05f);
                    var bounds = fuse.Frame.bounds.size;
                    Assert.AreEqual(size, bounds.x, size * .05f, "drawn bounds");
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
