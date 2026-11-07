#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JungleMaze.App.Ports;

namespace JungleMaze.App.Visibility.Mocks
{
    /// <summary>
    /// Dev-only fake fog of war wrapping the real policy: hides chosen net ids and/or one rectangle (cells). Used by the
    /// F10 panel, the VfxPreview scene and tests. Not compiled into release builds.
    /// </summary>
    public sealed class ToggleVisibilityPolicy : IVisibilityPolicy
    {
        private readonly IVisibilityPolicy _inner;
        private readonly bool[] _hidden = new bool[256];
        private bool _hasRect;
        private float _minX, _minY, _maxX, _maxY;

        public ToggleVisibilityPolicy(IVisibilityPolicy inner = null) => _inner = inner;

        public bool IsHidden(byte netId) => _hidden[netId];

        public void SetHidden(byte netId, bool hidden)
        {
            if (netId != VisibilityQuery.PositionOnly) _hidden[netId] = hidden;
        }

        /// <summary>Hides every query whose point lies inside the rectangle.</summary>
        public void HideRect(float minX, float minY, float maxX, float maxY)
        {
            _hasRect = true;
            _minX = minX; _minY = minY; _maxX = maxX; _maxY = maxY;
        }

        public void ClearRect() => _hasRect = false;

        public void ShowAll()
        {
            for (var i = 0; i < _hidden.Length; i++) _hidden[i] = false;
            _hasRect = false;
        }

        public bool IsVisible(float x, float y, byte netId)
        {
            if (netId != VisibilityQuery.PositionOnly && _hidden[netId]) return false;
            if (_hasRect && x >= _minX && x <= _maxX && y >= _minY && y <= _maxY) return false;
            return _inner == null || _inner.IsVisible(x, y, netId);
        }
    }
}
#endif
