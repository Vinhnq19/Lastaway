using System;
using JungleMaze.App.Hud.Feedback;
using JungleMaze.App.Match;
using JungleMaze.App.World;

namespace JungleMaze.App.Combat
{
    /// <summary>
    /// Reload read port for the HUD: picks the active weapon controller (offline practice first, then the online
    /// match, the same rule as <c>CombatWeaponPresenter</c>) and forwards the end of the own reload. Owns no timer.
    /// </summary>
    public sealed class LocalReloadSource : IReloadProgressSource, IDisposable
    {
        private readonly MatchSession _match;
        private readonly OnlineCombatSession _online;
        private readonly OfflineWorldSession _offline;
        private readonly OfflineMatchSession _offlineMatch;

        public LocalReloadSource(MatchSession match, OnlineCombatSession online, OfflineWorldSession offline, OfflineMatchSession offlineMatch)
        {
            _match = match;
            _online = online;
            _offline = offline;
            _offlineMatch = offlineMatch;
            if (_online != null) _online.Changed += OnOnline;
            if (_offline != null) _offline.CombatChanged += OnOffline;
        }

        public event Action<bool> Ended;

        public ReloadSample Current
        {
            get
            {
                if (_offline != null && _offline.IsActive)
                    return Sample(_offline.Combat?.Controller, _offlineMatch == null || !_offlineMatch.IsDead);
                if (_match != null && _match.Connected && _online != null)
                    return Sample(_online.Controller, _match.OwnHp > 0);
                return default;
            }
        }

        /// <summary>
        /// Reload snapshot of one controller. Inactive when there is no controller, the player is dead or the held
        /// weapon is no longer the one being reloaded. The total is the weapon's balance reloadSeconds.
        /// </summary>
        public static ReloadSample Sample(WeaponController controller, bool alive)
        {
            if (controller == null || !alive || !controller.IsReloading || controller.Selected != controller.ReloadWeapon)
                return default;
            var total = controller.Slot(controller.ReloadWeapon).ReloadSeconds;
            return new ReloadSample(true, controller.ReloadRemainingSeconds, total);
        }

        public void Dispose()
        {
            if (_online != null) _online.Changed -= OnOnline;
            if (_offline != null) _offline.CombatChanged -= OnOffline;
        }

        private void OnOnline(CombatEvent value) { if (_offline == null || !_offline.IsActive) Forward(value); }
        private void OnOffline(CombatEvent value) { if (_offline != null && _offline.IsActive) Forward(value); }

        private void Forward(CombatEvent value)
        {
            if (value.Kind == CombatEventKind.ReloadFinished) Ended?.Invoke(true);
            else if (value.Kind == CombatEventKind.ReloadRejected) Ended?.Invoke(false);
        }
    }
}
