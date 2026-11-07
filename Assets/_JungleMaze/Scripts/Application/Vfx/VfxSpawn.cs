namespace JungleMaze.App.Vfx
{
    /// <summary>Where and how one VFX instance appears.</summary>
    public readonly struct VfxSpawn
    {
        /// <summary>No anchor: the instance stays at (X, Y).</summary>
        public const byte NoAnchor = 255;

        public readonly float X, Y;
        /// <summary>Area in cells for area effects (fuse frames); 0 = nominal size.</summary>
        public readonly float AreaWidth, AreaHeight;
        /// <summary>Body the instance follows every frame, or <see cref="NoAnchor"/>.</summary>
        public readonly byte AnchorNetId;
        /// <summary>Damage number to show (DamageNumber cue only).</summary>
        public readonly int Number;
        /// <summary>Palette index of the catalog; 0 = the prefab's own colours.</summary>
        public readonly byte Tint;
        /// <summary>Seconds the instance is expected to live (the fuse frame blinks faster towards the end); 0 = unknown.</summary>
        public readonly float Seconds;

        public VfxSpawn(float x, float y, float areaWidth = 0f, float areaHeight = 0f, byte anchorNetId = NoAnchor,
            int number = 0, byte tint = 0, float seconds = 0f)
        {
            X = x;
            Y = y;
            AreaWidth = areaWidth;
            AreaHeight = areaHeight;
            AnchorNetId = anchorNetId;
            Number = number;
            Tint = tint;
            Seconds = seconds;
        }
    }
}
