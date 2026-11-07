#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace JungleMaze.App.Vfx.Mocks
{
    /// <summary>
    /// Dev-only fake match for the VfxPreview scene and the F10 panel: a session key ("change room" = new Guid),
    /// InGame and own net id 0 (offline-style: the own body is the Hero anchor). Not compiled into release builds.
    /// </summary>
    public sealed class MockVfxMatch : IVfxMatchFeed
    {
        public MockVfxMatch() => NewSession();

        /// <summary>When true the feed uses this mock instead of the real sessions.</summary>
        public bool Active { get; set; }
        public Guid SessionKey { get; private set; }
        public bool InGame { get; set; } = true;
        public byte OwnNetId { get; set; }
        public bool Online { get; set; }

        /// <summary>"Change room": a new match id; the director releases everything on its next look.</summary>
        public void NewSession() => SessionKey = Guid.NewGuid();
    }
}
#endif
