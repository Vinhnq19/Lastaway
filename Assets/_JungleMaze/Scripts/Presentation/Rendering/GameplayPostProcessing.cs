using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace JungleMaze.Presentation.Rendering
{
    /// <summary>
    /// Gameplay post-processing: a global URP volume (bloom, colour adjustments, white balance, vignette) built at runtime
    /// from <see cref="GameplayPostTuning"/>, and post-processing switched on for the main camera, only in the scenes the
    /// tuning lists. Lives on the Bootstrap object (DontDestroyOnLoad); outside those scenes the volume has no weight and
    /// cameras are left as authored. Presentation only.
    /// </summary>
    // After HeroCameraFollow (900) and the VFX camera shake (1100), so the sunlight overlay sits on this frame's view.
    [DefaultExecutionOrder(1200)]
    public sealed class GameplayPostProcessing : MonoBehaviour
    {
        private GameplayPostTuning _tuning;
        private Volume _volume;
        private VolumeProfile _profile;
        private Bloom _bloom;
        private ColorAdjustments _color;
        private WhiteBalance _white;
        private Vignette _vignette;
        private Tonemapping _tonemapping;
        private ShadowsMidtonesHighlights _grade;
        private SunlightOverlay _sun;
        private bool _active;
        private Camera _camera;

        /// <summary>Active only when the current scene is listed and the stack is enabled.</summary>
        public bool Active => _active;

        public GameplayPostProcessing Configure(GameplayPostTuning tuning)
        {
            _tuning = tuning;
            if (_volume == null) Build();
            Apply();
            Refresh(SceneManager.GetActiveScene());
            return this;
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;
        private void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;
        private void OnSceneChanged(Scene _, Scene next) { _camera = null; Refresh(next); }

        private void Build()
        {
            var go = new GameObject("Gameplay Post Volume");
            go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "Gameplay Post (runtime)";
            _bloom = _profile.Add<Bloom>(true);
            _color = _profile.Add<ColorAdjustments>(true);
            _white = _profile.Add<WhiteBalance>(true);
            _vignette = _profile.Add<Vignette>(true);
            _grade = _profile.Add<ShadowsMidtonesHighlights>(true);
            // Pixel art keeps its palette: no tone curve, grading stays in the colour adjustments above.
            _tonemapping = _profile.Add<Tonemapping>(true);
            _tonemapping.mode.Override(TonemappingMode.None);
            _volume.sharedProfile = _profile;
            _sun = new GameObject("Sunlight").AddComponent<SunlightOverlay>();
            _sun.transform.SetParent(transform, false);
            _sun.Configure(_tuning);
        }

        /// <summary>Copies the tuning into the profile (cheap; also run every frame in the Editor so Inspector edits show live).</summary>
        private void Apply()
        {
            if (_profile == null || _tuning == null) return;
            var t = _tuning;
            _bloom.threshold.Override(t.BloomThreshold);
            _bloom.intensity.Override(t.BloomIntensity);
            _bloom.scatter.Override(t.BloomScatter);
            _bloom.tint.Override(t.BloomTint);
            _bloom.clamp.Override(t.BloomClamp);
            _bloom.highQualityFiltering.Override(t.BloomHighQuality);
            _color.postExposure.Override(t.PostExposure);
            _color.contrast.Override(t.Contrast);
            _color.saturation.Override(t.Saturation);
            _color.colorFilter.Override(t.ColorFilter);
            _white.temperature.Override(t.Temperature);
            _white.tint.Override(t.Tint);
            _vignette.intensity.Override(t.VignetteIntensity);
            _vignette.smoothness.Override(t.VignetteSmoothness);
            _vignette.color.Override(t.VignetteColor);
            _grade.highlights.Override(new Vector4(t.HighlightTint.r, t.HighlightTint.g, t.HighlightTint.b, 0f));
            _grade.shadows.Override(new Vector4(t.ShadowTint.r, t.ShadowTint.g, t.ShadowTint.b, 0f));
        }

        private void Refresh(Scene scene)
        {
            _active = _tuning != null && _tuning.Enabled && Listed(scene.name);
            if (_volume != null) _volume.weight = _active ? 1f : 0f;
        }

        private bool Listed(string scene)
        {
            if (_tuning == null || _tuning.Scenes == null) return false;
            return Array.IndexOf(_tuning.Scenes, scene) >= 0;
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            Apply();
            Refresh(SceneManager.GetActiveScene());
#endif
            if (_sun != null) _sun.Render(Camera.main, _active, Time.time, Time.deltaTime);
            if (!_active) return;
            // The Game camera is authored without URP data; turn post-processing on once per camera instance.
            var main = Camera.main;
            if (main == null || main == _camera) return;
            _camera = main;
            var data = main.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }

        private void OnDestroy()
        {
            if (_profile != null) Destroy(_profile);
        }
    }
}
