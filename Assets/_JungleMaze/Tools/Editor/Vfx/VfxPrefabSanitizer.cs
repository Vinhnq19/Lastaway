using System.Collections.Generic;
using System.Text;
using JungleMaze.Presentation.Vfx;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// VFX-01 plan 4.5 step 2: the mechanical clean-up of a JSurviv/Epic Toon VFX copy so it fits the pool and the 2D
    /// URP rules (AC9). Removes Light, Collider/Rigidbody and every MonoBehaviour whose script lives under
    /// <c>Assets/Imported/</c> (ETFX*), turns Mesh render mode into Billboard, sets stopAction None, playOnAwake off,
    /// scaling Hierarchy, the approved sorting layer/order, root scale 1 and z 0, and adds <see cref="VfxParticleEffect"/>.
    /// Never touches the source asset: it works on prefab contents of the copy in <c>Prefabs/Vfx</c>.
    /// </summary>
    public static class VfxPrefabSanitizer
    {
        /// <summary>Cleans <paramref name="root"/> in place and returns a report of what changed.</summary>
        public static string Sanitize(GameObject root, string sortingLayer, int sortingOrder)
        {
            var report = new StringBuilder();
            foreach (var light in root.GetComponentsInChildren<Light>(true)) { report.AppendLine("removed Light on " + light.name); Object.DestroyImmediate(light); }
            foreach (var c in root.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Rigidbody2D>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var c in root.GetComponentsInChildren<Collider2D>(true)) Object.DestroyImmediate(c);
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                var script = MonoScript.FromMonoBehaviour(behaviour);
                var path = script != null ? AssetDatabase.GetAssetPath(script) : "";
                if (!path.StartsWith("Assets/Imported/")) continue;
                report.AppendLine("removed script " + behaviour.GetType().Name + " on " + behaviour.name);
                Object.DestroyImmediate(behaviour);
            }
            var layerId = SortingLayer.NameToID(sortingLayer);
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.None;
                main.playOnAwake = false;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer == null) continue;
                if (renderer.renderMode == ParticleSystemRenderMode.Mesh)
                {
                    report.AppendLine("mesh render mode → billboard on " + ps.name + " (" + (renderer.mesh != null ? renderer.mesh.name : "none") + ")");
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.mesh = null;
                }
                renderer.sortingLayerID = layerId;
                renderer.sortingOrder = sortingOrder;
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                r.sortingLayerID = layerId;
                r.sortingOrder = sortingOrder;
            }
            root.transform.localScale = Vector3.one;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            if (root.GetComponent<VfxParticleEffect>() == null) root.AddComponent<VfxParticleEffect>();
            return report.ToString();
        }

        [MenuItem("Jungle Maze/VFX/Sanitize Selected Prefab")]
        private static void SanitizeSelected()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab") || path.StartsWith("Assets/Imported/"))
            {
                Debug.LogWarning("[VFX] Select a VFX prefab copy outside Assets/Imported/.");
                return;
            }
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var report = Sanitize(contents, "VFX", 20);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("[VFX] Sanitized " + path + "\n" + report);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        /// <summary>Renderers left in Mesh mode (for the audit).</summary>
        public static List<string> MeshRenderers(GameObject root)
        {
            var list = new List<string>();
            foreach (var r in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
                if (r.renderMode == ParticleSystemRenderMode.Mesh) list.Add(r.name);
            return list;
        }
    }
}
