using JungleMaze.App.Hud.State;
using JungleMaze.World.Inventory;

namespace JungleMaze.App.World
{
    /// <summary>Projects server-owned item state onto the existing HUD. No inventory decisions live here.</summary>
    public sealed class InventoryHudBinding
    {
        private readonly IInventoryStateWriter _inventory;
        private readonly IHudStateWriter _hud;
        private string _using;
        private readonly bool _publishCombat;
        private InventorySnapshot _last;
        public InventoryHudBinding(IInventoryStateWriter inventory, IHudStateWriter hud, bool publishCombat = true)
        {
            _inventory = inventory; _hud = hud; _publishCombat=publishCombat;
            LocalConsumableHold.Changed += ShowActiveSlot;
        }
        /// <summary>The highlighted quickbar slot: the consumable in hand (4..7), else the selected weapon.</summary>
        private void ShowActiveSlot()
        {
            if (_last == null) return;
            _inventory.SetActiveSlot(LocalConsumableHold.Active ? 4 + LocalConsumableHold.Slot : _last.ActiveWeapon);
        }
        public void Apply(InventorySnapshot state, double now)
        {
            _last = state;
            LocalConsumableHold.Sync(state);
            for (var i = 0; i < 4; i++)
            {
                _inventory.SetWeaponSlot(i, state.Weapons[i].IsEmpty ? null : state.Weapons[i].ItemId);
                _inventory.SetConsumableSlot(i, state.Consumables[i].ItemId, state.Consumables[i].Count);
            }
            ShowActiveSlot();
            if (_publishCombat) _hud.SetHeldWeapon(state.ActiveWeapon < 0 ? "HAND" : state.Weapons[state.ActiveWeapon].ItemId);
            _hud.SetHelmet(state.Helmet.ItemId, state.Helmet.Durability);
            _hud.SetArmor(state.Armor.ItemId, state.Armor.Durability);
            var ammo = state.ActiveWeapon < 0 ? 0 : state.Weapons[state.ActiveWeapon].Ammo;
            var magazine=state.ActiveWeapon<0?0:state.Weapons[state.ActiveWeapon].Magazine;
            if (_publishCombat) _hud.SetAmmo(magazine, ammo);
            if (state.IsUsing && state.UseSlot >= 0 && state.UseSlot < 4)
            {
                _using = state.UseItemId;
                _inventory.BeginUse(state.UseSlot + 4, _using, (float)state.UseDuration,
                    (float)System.Math.Max(0, state.UseEnd - now), true);
            }
            else if (_using != null)
            { _inventory.CancelUse(_using); _using = null; }
        }
        public void Reset()
        {
            _using=null;_last=null;LocalConsumableHold.Release();_inventory.Reset();
            for(var i=0;i<4;i++){_inventory.SetWeaponSlot(i,null);_inventory.SetConsumableSlot(i,null,0);}
            _inventory.SetActiveSlot(-1);_hud.SetHeldWeapon("HAND");_hud.SetHelmet(null,0);_hud.SetArmor(null,0);_hud.SetAmmo(0,0);
        }
    }
}
