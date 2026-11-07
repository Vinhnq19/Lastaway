using JungleMaze.Domain.Hud;

namespace JungleMaze.Domain.CombatFeedback
{
    /// <summary>
    /// Statuses that can be drawn on a body. Values 1..8 equal <see cref="EffectKind"/> (locked by a test);
    /// <see cref="Invulnerable"/> has no HUD effect, so it lives outside that range.
    /// </summary>
    public enum StatusKind : byte
    {
        None = 0,
        Adrenaline = 1,
        Poison = 2,
        Slow = 3,
        Invisible = 4,
        Stun = 5,
        Root = 6,
        Reverse = 7,
        CcImmune = 8,
        Invulnerable = 100
    }

    /// <summary>Conversions between the HUD effect list and <see cref="StatusKind"/>.</summary>
    public static class StatusKinds
    {
        /// <summary>Same numeric value; unknown effects map to <see cref="StatusKind.None"/>.</summary>
        public static StatusKind FromEffect(EffectKind kind) =>
            (byte)kind >= 1 && (byte)kind <= 8 ? (StatusKind)(byte)kind : StatusKind.None;
    }
}
