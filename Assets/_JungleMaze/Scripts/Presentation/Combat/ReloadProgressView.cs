using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.Combat
{
    /// <summary>Own-player reload progress in screen space, following the displayed Hero.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class ReloadProgressView : MonoBehaviour
    {
        private GameObject _canvas;
        private RectTransform _ring;
        private Image _fill;
        private Texture2D _texture;
        private Sprite _sprite;
        private Transform _target;

        public void Show(Transform target, float progress)
        {
            _target = target;
            if (_canvas == null) CreateRing();
            _canvas.SetActive(true);
            _fill.fillAmount = Mathf.Clamp01(progress);
        }

        // Project after HeroCameraFollow (900) and the spectator driver (1000).
        private void LateUpdate()
        {
            if (_canvas == null || !_canvas.activeSelf) return;
            var camera = Camera.main;
            if (_target == null || camera == null) { Hide(); return; }
            var screen = camera.WorldToScreenPoint(_target.position);
            if (screen.z <= 0) { Hide(); return; }
            _ring.position = new Vector3(screen.x, screen.y + 48f, 0);
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.SetActive(false);
        }

        private void CreateRing()
        {
            const int size = 64;
            _texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var radius = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * (size / 2f));
                    var alpha = Mathf.Clamp01(30f - radius) * Mathf.Clamp01(radius - 23f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            _texture.SetPixels(pixels);
            _texture.Apply(false, true);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, size, size), Vector2.one * .5f);
            _canvas = new GameObject("Reload progress", typeof(Canvas));
            _canvas.transform.SetParent(transform, false);
            var canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var background = new GameObject("Ring background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(_canvas.transform, false);
            _ring = background.GetComponent<RectTransform>();
            _ring.sizeDelta = new Vector2(44, 44);
            var image = background.GetComponent<Image>();
            image.sprite = _sprite;
            image.color = new Color(0, 0, 0, .75f);
            image.raycastTarget = false;
            var foreground = new GameObject("White fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            foreground.transform.SetParent(_ring, false);
            var rect = foreground.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _fill = foreground.GetComponent<Image>();
            _fill.sprite = _sprite;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Radial360;
            _fill.fillOrigin = (int)Image.Origin360.Top;
            _fill.fillClockwise = true;
            _fill.color = Color.white;
            _fill.raycastTarget = false;
        }

        private void OnDisable() => Hide();

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas);
            if (_sprite != null) Destroy(_sprite);
            if (_texture != null) Destroy(_texture);
        }
    }
}
