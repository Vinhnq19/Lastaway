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
            var offlineWorld = new OfflineWorldSession(offlineSession, data?.Layout, data?.Destructibles, data?.Hazards, catalog, lootTable, balance, hudWriter, inventoryWriter, effectWriter, flow, binding);
            try { offlineWorld.ThrowableRules = ThrowableRulesJson.Load(Application.streamingAssetsPath); }
            catch (System.Exception e) { _log.Warn("[Throwables] " + e.Message); }
            _registry.Register(offlineWorld);
            ticks.Add(offlineWorld);
            // F pickup list (07_Floor_Loot section 11): WorldSceneView fills it online and offline, the HUD panel draws it.
            _registry.Register(new NearbyLootPicker(catalog));
            if (_registry.TryResolve<JungleMaze.App.Audio.MatchAudioEventSource>(out var audio))
                audio.BindCombat(combat, offlineWorld, world, _registry.Resolve<IHudState>());
            // The Game scene owns its world views and colliders; services survive scene changes.
        }
    }
}
