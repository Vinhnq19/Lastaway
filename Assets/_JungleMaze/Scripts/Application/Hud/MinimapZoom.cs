using System;

namespace JungleMaze.App.Hud
{
    /// <summary>
    /// Minimap zoom numbers (T-018, de-xuat/minimap-chuc-nang.md 4.1; designer values, tuned after Huy plays).
    /// Serialized on the minimap like <see cref="HudTuning"/>, not in balance JSON.
    /// </summary>
    [Serializable]
    public struct MinimapTuning
    {
        /// <summary>Side of the near window in cells (level 2): vision diameter 24 + 3 cells each side.</summary>
        public float NearCells;
        /// <summary>Scale and centre blend when the level changes, unscaled seconds.</summary>
        public float TweenSeconds;

        public static MinimapTuning Default => new MinimapTuning { NearCells = 30f, TweenSeconds = 0.15f };
    }

    /// <summary>
    /// Two minimap levels (T-018): 1 = the whole map, fixed; 2 = a <see cref="MinimapTuning.NearCells"/> square that
    /// follows the hero, clamped inside the map. The window is in world cells (map spans [0, mapSize]); level changes
    /// blend from the window on screen to the new target, so a change during a blend carries on to the new goal.
    /// </summary>
    public sealed class MinimapZoom
    {
        public const int Full = 1, Near = 2;

        private float _t = 1f, _fromX, _fromY, _fromCells;
        private bool _started;

        public int Level { get; private set; } = Full;
        public float MinX { get; private set; }
        public float MinY { get; private set; }
        /// <summary>Side of the visible window in cells; 0 before the first <see cref="Tick"/>.</summary>
        public float Cells { get; private set; }
        public bool Blending => _t < 1f;

        public void Toggle() => SetLevel(Level == Full ? Near : Full);

        public void SetLevel(int level)
        {
            level = level == Near ? Near : Full;
            if (level == Level) return;
            Level = level;
            _fromX = MinX;
            _fromY = MinY;
            _fromCells = Cells;
            _t = _started ? 0f : 1f;
        }

        public void Tick(float deltaSeconds, MinimapTuning tuning, int mapSizeCells, bool hasHero, float heroX, float heroY)
        {
            var map = mapSizeCells > 0 ? mapSizeCells : 1;
            var cells = Level == Near ? tuning.NearCells : map;
            Window(cells, map, hasHero, heroX, heroY, out var x, out var y, out cells);
            if (_t < 1f) _t = tuning.TweenSeconds > 0f ? Math.Min(1f, _t + deltaSeconds / tuning.TweenSeconds) : 1f;
            MinX = Lerp(_fromX, x, _t);
            MinY = Lerp(_fromY, y, _t);
            Cells = Lerp(_fromCells, cells, _t);
            _started = true;
        }

        /// <summary>
        /// Bottom-left corner of a <paramref name="cells"/> window centred on the hero and kept inside the map
        /// (map centre without a hero). A window as large as the map is the map.
        /// </summary>
        public static void Window(float cells, int mapSizeCells, bool hasHero, float heroX, float heroY, out float minX, out float minY, out float side)
        {
            side = cells <= 0f || cells > mapSizeCells ? mapSizeCells : cells;
            var half = side / 2f;
            var cx = hasHero ? Clamp(heroX, half, mapSizeCells - half) : mapSizeCells / 2f;
            var cy = hasHero ? Clamp(heroY, half, mapSizeCells - half) : mapSizeCells / 2f;
            minX = cx - half;
            minY = cy - half;
        }

        /// <summary>A pixel on the whole-map minimap (<see cref="MinimapProjection"/>) to the same point in the current window.</summary>
        public float ToWindow(float fullPixel, float min, float sizePixels, int mapSizeCells)
        {
            if (Cells <= 0f || sizePixels <= 0f) return fullPixel;
            return (fullPixel / sizePixels * mapSizeCells - min) / Cells * sizePixels;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
