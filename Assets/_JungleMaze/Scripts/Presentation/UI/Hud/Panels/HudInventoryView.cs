using System;
using JungleMaze.App.Hud;
using JungleMaze.App.Hud.State;
using JungleMaze.Core.DI;
using JungleMaze.Presentation.UI.Kit;
using JungleMaze.Domain.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Hud
{
    /// <summary>
    /// B4 bag (T-022, mock T-021/b/b4-tui-do*.png): read-only view of the 4 weapon + 4 consumable slots, worn helmet and
    /// armour, adrenaline seconds and ammo, with an item tooltip on hover. Nothing here sends a command (no drop).
    /// Nested in Hud.prefab (no GraphicRaycaster there): hover is a rect test, not a raycast. No key opens it any more
    /// (Tab/I removed 08/11: the quickbar already shows every slot); <see cref="Open"/> stays for tools and tests.
    /// The "nearby / on the ground" column has no data source yet and always shows its empty state.
    /// </summary>
    public sealed class HudInventoryView : MonoBehaviour
    {
        [Serializable]
        public struct Cell
        {
            public RectTransform Root;
            public Image Frame;
            public Image Icon;
            public TMP_Text Count;
        }

        public const string WeaponKind = "Vũ khí";
        public const string ConsumableKind = "Dược phẩm";

        [SerializeField] private GameObject content;
        [SerializeField] private Cell[] cells = new Cell[InventorySlots.TotalSlotCount];
        [SerializeField] private Sprite slotNormal, slotSelected, slotEmpty;
        [SerializeField] private TMP_Text helmetValue, armorValue, adrenalineValue, ammoValue;
        [SerializeField] private RectTransform tooltip;
        [SerializeField] private RectTransform tooltipBackground;
        [SerializeField] private Image tooltipIcon;
        [SerializeField] private TMP_Text tooltipName, tooltipKind;
        [SerializeField] private float tooltipGap = 8f;

        private IHudState _hud;
        private IInventoryState _inventory;
        private IEffectState _effects;
        private IItemIconProvider _icons;
        private Camera _camera;
        private readonly char[] _text = new char[24];
        private readonly Vector3[] _corners = new Vector3[4];

        public bool IsOpen => content != null && content.activeSelf;
        public Cell[] Cells => cells;
        public RectTransform Tooltip => tooltip;
        public TMP_Text TooltipName => tooltipName;
        public TMP_Text TooltipKind => tooltipKind;
        public TMP_Text HelmetValue => helmetValue;
        public TMP_Text ArmorValue => armorValue;
        public TMP_Text AdrenalineValue => adrenalineValue;
        public TMP_Text AmmoValue => ammoValue;

        [Inject]
        public void Construct(IHudState hud, IInventoryState inventory, IEffectState effects, IItemIconProvider icons)
        {
            _hud = hud;
            _inventory = inventory;
            _effects = effects;
            _icons = icons;
        }

        public void Open()
        {
            if (content == null) return;
            content.SetActive(true);
            UiLayerStack.Push(this, Close, blocksActions: true); // ESC closes the top panel before the match menu (CPO 07/11)
            var canvas = GetComponentInParent<Canvas>();
            _camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Refresh();
        }

        public void Close()
        {
            UiLayerStack.Remove(this);
            if (content != null) content.SetActive(false);
            ShowTooltip(-1);
        }

        /// <summary>A disabled HUD (scene unload, spectator teardown) never leaves a stale ESC layer behind.</summary>
        private void OnDisable() => UiLayerStack.Remove(this);

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Update()
        {
            if (!IsOpen) return;
            Refresh();
            Hover(Input.mousePosition);
        }

        /// <summary>Pushes the current state into every cell and row (cheap; runs each frame only while open).</summary>
        public void Refresh()
        {
            for (var i = 0; i < cells.Length; i++)
            {
                var slot = Slot(i);
                var c = cells[i];
                var look = HudPanelMath.Look(slot.IsEmpty, _inventory != null && _inventory.ActiveSlot == i);
                var frame = look == HudPanelMath.SlotLook.Empty ? slotEmpty : look == HudPanelMath.SlotLook.Selected ? slotSelected : slotNormal;
                if (c.Frame != null && c.Frame.sprite != frame) c.Frame.sprite = frame;
                var icon = slot.IsEmpty || _icons == null ? null : _icons.GetIcon(slot.ItemId);
                if (c.Icon != null)
                {
                    if (c.Icon.sprite != icon) c.Icon.sprite = icon;
                    c.Icon.enabled = icon != null;
                }
                if (c.Count != null)
                {
                    var show = !slot.IsEmpty && i >= InventorySlots.WeaponSlotCount; // counts only on consumables (mock)
                    c.Count.enabled = show;
                    if (show) c.Count.SetCharArray(_text, 0, HudFormat.Count(_text, slot.Count));
                }
            }
            Number(helmetValue, Durability(_hud != null ? _hud.Helmet : default));
            Number(armorValue, Durability(_hud != null ? _hud.Armor : default));
            var adrenaline = _effects != null && _effects.TryGet(EffectKind.Adrenaline, out var effect) && effect.IsTimed
                ? HudFormat.CeilToInt(effect.RemainingSeconds) : 0;
            Number(adrenalineValue, adrenaline);
            if (ammoValue != null)
            {
                var ammo = _hud != null ? _hud.Ammo : default;
                ammoValue.SetCharArray(_text, 0, HudFormat.Fraction(_text, ammo.InMagazine, ammo.Reserve));
            }
        }

        /// <summary>Shows the tooltip of cell <paramref name="index"/> (-1 or an empty slot hides it).</summary>
        public void ShowTooltip(int index)
        {
            if (tooltip == null) return;
            var slot = index >= 0 && index < cells.Length ? Slot(index) : default;
            if (slot.IsEmpty)
            {
                if (tooltip.gameObject.activeSelf) tooltip.gameObject.SetActive(false);
                return;
            }
            tooltip.gameObject.SetActive(true);
            if (tooltipIcon != null) tooltipIcon.sprite = cells[index].Icon != null ? cells[index].Icon.sprite : null;
            var name = HudItemNames.Get(slot.ItemId, slot.DisplayName ?? slot.ItemId); // GDD ITEM LIST name
            if (tooltipName.text != name) tooltipName.text = name;
            var kind = index < InventorySlots.WeaponSlotCount ? WeaponKind : ConsumableKind;
            if (tooltipKind.text != kind) tooltipKind.text = kind;

            var parent = (RectTransform)tooltip.parent;
            cells[index].Root.GetWorldCorners(_corners);
            var min = parent.InverseTransformPoint(_corners[0]);
            var max = parent.InverseTransformPoint(_corners[2]);
            var flip = HudPanelMath.TooltipFlipsLeft(max.x, tooltipGap, tooltip.rect.width, parent.rect.xMax);
            tooltip.pivot = new Vector2(flip ? 1f : 0f, 1f);
            tooltip.localPosition = new Vector3(flip ? min.x - tooltipGap : max.x + tooltipGap, max.y, 0f);
            if (tooltipBackground != null) tooltipBackground.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f); // arrow faces the cell
        }

        private void Hover(Vector2 screen)
        {
            var hovered = -1;
            for (var i = 0; i < cells.Length && hovered < 0; i++)
                if (cells[i].Root != null && RectTransformUtility.RectangleContainsScreenPoint(cells[i].Root, screen, _camera)) hovered = i;
            ShowTooltip(hovered);
        }

        private InventorySlot Slot(int index) =>
            _inventory != null && index < InventorySlots.TotalSlotCount ? _inventory.GetSlot(index) : default;

        private static int Durability(GearStatus gear) => gear.IsWorn ? HudFormat.CeilToInt(Mathf.Max(0f, gear.Durability)) : 0;

        private void Number(TMP_Text text, int value)
        {
            if (text != null) text.SetCharArray(_text, 0, HudFormat.Integer(_text, value));
        }
    }
}
