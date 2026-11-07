using System;

namespace JungleMaze.App.Vfx
{
    /// <summary>Match facts the VFX director needs, the same for online, offline and the dev mock.</summary>
    public interface IVfxMatchFeed
    {
        /// <summary>Changes on every new match/room; <see cref="Guid.Empty"/> when there is none.</summary>
        Guid SessionKey { get; }
        /// <summary>False when disconnected, outside the InGame phase or when offline play is not active.</summary>
        bool InGame { get; }
        /// <summary>255 when unknown.</summary>
        byte OwnNetId { get; }
        /// <summary>True while a networked match drives the session (own body is the own PlayerView, not the Hero).</summary>
        bool Online { get; }
    }
}
