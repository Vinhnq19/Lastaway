using JungleMaze.App.World;
using JungleMaze.App.Combat;
using JungleMaze.App.Match;
using JungleMaze.App.Hud.State;
using JungleMaze.App.Ports;
using JungleMaze.Infrastructure.Ticking;
using JungleMaze.Infrastructure.Config;
using UnityEngine;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using JungleMaze.Presentation.World.Phase2;

namespace JungleMaze.Composition
{
    public sealed partial class Bootstrap
    {
        private void ComposeWorld(TickRunner ticks, MatchSession match)
        {
            var inventoryWriter = _registry.Resolve<IInventoryStateWriter>();
            var hudWriter = _registry.Resolve<IHudStateWriter>();
            var binding = new InventoryHudBinding(inventoryWriter, hudWriter, false);
            _registry.Register(binding);
            WorldConfigData data = null;
            ItemCatalog catalog = null;
            LootTable lootTable = null;
            try
            {
                data = WorldConfigData.Load(Application.streamingAssetsPath);
                _registry.Register(data);
                _registry.Register(data.Map);
                _registry.Register(data.Destructibles);
                _registry.Register(data.Hazards);
                var itemData = WorldConfigData.LoadItems(Application.streamingAssetsPath);
                catalog = itemData.Build();
                _registry.Register(catalog);
                lootTable = new LootTable(catalog, itemData.Loot);
                _registry.Register(lootTable);
            }
            catch (System.Exception e) { _log.Warn("[World] " + e.Message); }
            var config = _registry.Resolve<IConfigProvider>();
            var runtimeData = _registry.Resolve<IClientRuntimeData>();
            var world = new WorldClientSession(match, _registry.Resolve<IPacketSender>(), _registry.Resolve<IPacketReceiver>(), runtimeData, binding, data != null ? data.Map : null, () => config.Current.WorldEnabled, _registry.Resolve<IEffectStateWriter>(), data != null ? data.Layout : null,
                () => ((ClientRuntimeData)runtimeData).SelectedLayout);
            _registry.Register(world);
            ticks.Add(world);
            var combat = new OnlineCombatSession(match, world, _registry.Resolve<IBalanceConfigProvider>(), _registry.Resolve<IPacketSender>(), _registry.Resolve<IPacketReceiver>(), hudWriter);
            _registry.Register(combat);
            ticks.Add(combat);
            var throwables = new OnlineThrowableSession(match, world, _registry.Resolve<IPacketSender>(), _registry.Resolve<IPacketReceiver>());
            _registry.Register(throwables);
            ticks.Add(throwables);

            // In-process offline world session for local solo play (MAP-02 / ITM-01)
            var offlineSession = _registry.Resolve<OfflineMatchSession>();
            var flow = _registry.Resolve<JungleMaze.App.Flow.GameFlow>();
            var effectWriter = _registry.Resolve<IEffectStateWriter>();
            var balance = _registry.Resolve<IBalanceConfigProvider>();
            var offlineWorld = new OfflineWorldSession(offlineSession, data?.Layout, data?.Destructibles, data?.Hazards, catalog, lootTable, balance, hudWriter, inventoryWriter, effectWriter, flow, binding, data?.Airdrops);
            try { offlineWorld.ThrowableRules = ThrowableRulesJson.Load(Application.streamingAssetsPath); }
            catch (System.Exception e) { _log.Warn("[Throwables] " + e.Message); }
            _registry.Register(offlineWorld);
            ticks.Add(offlineWorld);
            // Reload progress on the HUD action bar and the ring above the own hero, online and in offline practice.
            _registry.Register<JungleMaze.App.Hud.Feedback.IReloadProgressSource>(new LocalReloadSource(match, combat, offlineWorld, offlineSession));
            // F pickup list (07_Floor_Loot section 11): WorldSceneView fills it online and offline, the HUD panel draws it.
            _registry.Register(new NearbyLootPicker(catalog));
            if (_registry.TryResolve<JungleMaze.App.Audio.MatchAudioEventSource>(out var audio))
                audio.BindCombat(combat, offlineWorld, world, _registry.Resolve<IHudState>(), throwables);
            // The Game scene owns its world views and colliders; services survive scene changes.
        }
        private void ConfigureAirdropCheck()
        {
            var world=_registry.Resolve<WorldClientSession>();
            var match=_registry.Resolve<MatchSession>();
            var sawThreeDrops=false;
            JungleMaze.App.Testing.OnlineTestExtensions.RegisterCheck("drg01.three-drops",context=>
            {
                if(!sawThreeDrops&&world.IsReady&&world.Airdrops.Count==3)
                {
                    var landed=0;foreach(var state in world.Airdrops.Values)
                        if(state.MatchId==match.MatchId&&(state.Phase==3||state.Phase==4))landed++;
                    sawThreeDrops=landed==3;
                }
                return new JungleMaze.App.Testing.OnlineCheckResult(sawThreeDrops?"PASS":"WAIT","Three authoritative eggs landed in one match; gameplay v4.");
            });
        }
    }
}
