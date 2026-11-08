namespace JungleMaze.Domain.CombatFeedback
{
    /// <summary>Confirmed HP loss, independent of weapon/raw damage events.</summary>
    public readonly struct PlayerHealthLost
    {
        public readonly System.Guid MatchId;
        public readonly byte NetId;
        public readonly int Amount;
        public readonly uint Tick;
        public PlayerHealthLost(System.Guid matchId, byte netId, int amount, uint tick)
        { MatchId = matchId; NetId = netId; Amount = amount; Tick = tick; }
    }

    /// <summary>Two steps of a thrown/area weapon: the burning fuse, then the blast.</summary>
    public enum ExplosionPhase : byte
    {
        Fuse = 0,
        Detonate = 1
    }

    /// <summary>
    /// An authoritative hit on a player (VFX-01 contract for WPN-02). Published on the event bus by the packet adapter;
    /// never predicted. <see cref="EventKey"/> must be stable across resends (for example tick, source, target and seq).
    /// </summary>
    public readonly struct HitConfirmedEvent
    {
        public readonly ulong EventKey;
        public readonly byte SourceNetId;
        public readonly byte TargetNetId;
        public readonly int Damage;
        /// <summary>Server-side target position in cells; the effect is drawn on the target's body anchor.</summary>
        public readonly float X, Y;

        public HitConfirmedEvent(ulong eventKey, byte sourceNetId, byte targetNetId, int damage, float x, float y)
        {
            EventKey = eventKey;
            SourceNetId = sourceNetId;
            TargetNetId = targetNetId;
            Damage = damage;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// An authoritative explosion step (VFX-01 contract for WPN-03). Fuse shows the danger area until Detonate (or until
    /// <see cref="SecondsRemaining"/> runs out); Detonate removes the warning and may shake the camera. The blast itself
    /// is drawn by <c>ThrowableView</c>. Use the same id as <c>IHudStateWriter.SetDanger</c> for <see cref="EventKey"/>.
    /// </summary>
    public readonly struct ExplosionEvent
    {
        public readonly ulong EventKey;
        public readonly string WeaponId;
        public readonly ExplosionPhase Phase;
        public readonly float CenterX, CenterY;
        /// <summary>Area in cells; 0 means "use areaWidth/areaHeight of the weapon in balance JSON".</summary>
        public readonly float AreaWidth, AreaHeight;
        /// <summary>Fuse only: seconds until the blast; 0 means "use fuseSeconds of the weapon".</summary>
        public readonly float SecondsRemaining;
        public readonly byte OwnerNetId;

        public ExplosionEvent(ulong eventKey, string weaponId, ExplosionPhase phase, float centerX, float centerY,
            float areaWidth, float areaHeight, float secondsRemaining, byte ownerNetId)
        {
            EventKey = eventKey;
            WeaponId = weaponId;
            Phase = phase;
            CenterX = centerX;
            CenterY = centerY;
            AreaWidth = areaWidth;
            AreaHeight = areaHeight;
            SecondsRemaining = secondsRemaining;
            OwnerNetId = ownerNetId;
        }
    }

    /// <summary>
    /// A status applied to or removed from a player (VFX-01 contract for Game-10). Applying the same
    /// (<see cref="NetId"/>, <see cref="Kind"/>) again only refreshes the timer.
    /// </summary>
    public readonly struct StatusEvent
    {
        /// <summary>0 = no key (no resend filtering, e.g. the local effect store).</summary>
        public readonly ulong EventKey;
        public readonly byte NetId;
        public readonly StatusKind Kind;
        public readonly bool Applied;
        /// <summary>Full duration; 0 = until removed.</summary>
        public readonly float DurationSeconds;
        /// <summary>Seconds left when the event was produced; 0 = the full duration.</summary>
        public readonly float RemainingSeconds;

        public StatusEvent(ulong eventKey, byte netId, StatusKind kind, bool applied, float durationSeconds, float remainingSeconds)
        {
            EventKey = eventKey;
            NetId = netId;
            Kind = kind;
            Applied = applied;
            DurationSeconds = durationSeconds;
            RemainingSeconds = remainingSeconds;
        }
    }
}
