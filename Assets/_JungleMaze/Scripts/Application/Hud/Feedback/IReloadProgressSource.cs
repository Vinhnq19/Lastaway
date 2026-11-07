using System;

namespace JungleMaze.App.Hud.Feedback
{
    /// <summary>One frame of the local player's reload, read from the active weapon controller.</summary>
    public readonly struct ReloadSample
    {
        public ReloadSample(bool active, float remainingSeconds, float totalSeconds)
        {
            Active = active;
            RemainingSeconds = remainingSeconds;
            TotalSeconds = totalSeconds;
        }

        /// <summary>True while the own held weapon is reloading and the player is alive.</summary>
        public bool Active { get; }
        public float RemainingSeconds { get; }
        /// <summary>Reload length of the weapon from balance (reloadSeconds).</summary>
        public float TotalSeconds { get; }
    }

    /// <summary>
    /// Read port for the local player's reload, online and offline. The HUD only displays it; the weapon controller
    /// and the server own the reload itself.
    /// </summary>
    public interface IReloadProgressSource
    {
        /// <summary>Current reload of the local player; inactive when there is no match, controller or reload.</summary>
        ReloadSample Current { get; }

        /// <summary>Raised when the own reload ends: true when it filled the magazine, false when the server rejected it.</summary>
        event Action<bool> Ended;
    }
}
