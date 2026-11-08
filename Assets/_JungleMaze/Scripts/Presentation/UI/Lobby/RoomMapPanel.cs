using System;
using System.Collections.Generic;
using System.Threading;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JungleMaze.Presentation.UI.Lobby
{
    /// <summary>
    /// Map cards and bot controls of the lobby. Skin A (T-024): the controls come pre-built in LobbyUi.prefab
    /// (<see cref="LobbyView.MapCards"/>, card template, bot buttons, <see cref="LobbySlotView.RemoveBot"/>); this only
    /// shows them and wires the same actions. A prefab without them falls back to the old flat controls built here.
    /// </summary>
    public sealed class RoomMapPanel : MonoBehaviour
    {
        private const float Body = 24f / 1.5f, Caption = 20f / 1.5f; // text scale T-027 in canvas units (1080p px / 1.5)
        private const float CardGap = 12f / 1.5f;
        private RoomMapSession _maps;
        private MatchSession _match;
        private LobbyView _view;
        private RectTransform _content;
        private Button _template;
        private Button _retry;
        private Button _addBot, _fillBots;
        private TMP_Text _fillText;
        private readonly List<(LobbySlotView slot, Button button)> _removeBots = new List<(LobbySlotView, Button)>();
        private PublishedMaps _catalog;
        private CancellationTokenSource _images;
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly List<(PublishedMap map, Button button, TMP_Text title, GameObject selected)> _cards =
            new List<(PublishedMap, Button, TMP_Text, GameObject)>();

        public void Bind(RoomMapSession maps, MatchSession match, LobbyView view)
        {
            _maps = maps; _match = match; _view = view;
            if (view.MapCards != null && view.AddBot != null && view.FillBots != null && view.RetryMap != null) UsePrefab(view);
            else BuildFallback(view);
            _retry.onClick.AddListener(_maps.Retry);
            _addBot.onClick.AddListener(() => _maps.ManageBots(RoomBotAction.Add));
            _fillText = _fillBots.GetComponentInChildren<TMP_Text>();
            _fillBots.onClick.AddListener(() => _maps.ManageBots(RoomBotAction.FillMinimum));
            foreach (var slot in view.Slots)
            {
                var button = slot.RemoveBot != null ? slot.RemoveBot : FallbackRemoveButton(slot);
                button.onClick.AddListener(() =>
                {
                    foreach (var participant in _match.Roster)
                        if (participant.NetId == slot.NetId && participant.IsBot)
                        { _maps.ManageBots(RoomBotAction.Remove, participant.BotId); break; }
                });
                _removeBots.Add((slot, button));
            }
        }

        /// <summary>Skin A: the prefab already placed Start, Leave and every control on the mock; only show them.</summary>
        private void UsePrefab(LobbyView view)
        {
            view.MapCards.gameObject.SetActive(true);
            _content = view.MapCards.content;
            _template = view.MapCardTemplate;
            _retry = view.RetryMap;
            _addBot = view.AddBot;
            _fillBots = view.FillBots;
            _addBot.gameObject.SetActive(true);
            _fillBots.gameObject.SetActive(true);
        }

        /// <summary>Old flat controls for a prefab built before skin A (Figma 1280×720 units).</summary>
        private void BuildFallback(LobbyView view)
        {
            var info = view.Start.transform.parent as RectTransform;
            if (info != null) info.sizeDelta = new Vector2(info.sizeDelta.x, 560);
            Place(view.Start.GetComponent<RectTransform>(), 300, 500, 276, 44);
            Place(view.Leave.GetComponent<RectTransform>(), 24, 500, 260, 44);
            var viewport = Rect("Map cards", view.transform, 104, 467, 552, 150);
            var background = viewport.gameObject.AddComponent<Image>(); background.color = new Color(.06f, .1f, .1f, .98f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = Rect("Content", viewport, 0, 0, 552, 150);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = _content; scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
            _retry = Rect("Retry map", view.transform, 408, 427, 220, 32).gameObject.AddComponent<Button>();
            _retry.gameObject.AddComponent<Image>().color = new Color(.18f, .26f, .23f);
            Label(_retry.transform, "Thử tải map lại", 0, 0, 220, 32, Caption);
            _addBot = BotButton(view.transform, "+ Bot", 104, 427, 124, 32);
            _fillBots = BotButton(view.transform, "Điền đủ 6", 238, 427, 160, 32);
        }

        private Button FallbackRemoveButton(LobbySlotView slot)
        {
            var button = BotButton(slot.transform, "Xóa", 0, 0, 54, 28);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, .5f);
            rect.anchoredPosition = new Vector2(-4, 0);
            return button;
        }

        private void Update()
        {
            if (_maps == null) return;
            if (!ReferenceEquals(_catalog, _maps.Catalog)) Rebuild();
            _retry.gameObject.SetActive(_maps.Error != null);
            _retry.interactable = !_maps.Busy;
            _addBot.interactable = _maps.CanManageBots && _maps.State.PlayerCount < _maps.State.MaxPlayers;
            _fillBots.interactable = _maps.CanManageBots && _maps.State.PlayerCount < _maps.State.MinPlayers && _maps.State.MaxPlayers >= _maps.State.MinPlayers;
            _fillText.text = "Điền đủ " + (_maps.HasState ? _maps.State.MinPlayers : 6);
            foreach (var row in _removeBots)
            {
                var isBot = false;
                foreach (var participant in _match.Roster)
                    if (participant.NetId == row.slot.NetId && participant.IsBot) { isBot = true; break; }
                row.button.gameObject.SetActive(isBot);
                row.button.interactable = _maps.CanManageBots;
            }
            foreach (var card in _cards)
            {
                var selected = _maps.HasState && card.map.Id == _maps.State.MapId && card.map.MapHash == _maps.State.MapHash;
                card.button.interactable = _maps.IsHost && _match.Phase == MatchPhase.Waiting;
                if (card.selected != null) card.selected.SetActive(selected); // skin A: kit_card_room_selected overlay
                else card.button.GetComponent<Image>().color = selected ? new Color(.15f, .38f, .27f) : new Color(.12f, .18f, .2f);
                card.title.text = card.map.Name + " · v" + card.map.Version;
            }
        }

        private void Rebuild()
        {
            _images?.Cancel(); _images?.Dispose(); _images = new CancellationTokenSource();
            foreach (Transform child in _content) Destroy(child.gameObject);
            foreach (var image in _textures) Destroy(image); _textures.Clear(); _cards.Clear();
            _catalog = _maps.Catalog;
            var maps = _catalog?.Maps ?? Array.Empty<PublishedMap>();
            var pitch = _template != null ? ((RectTransform)_template.transform).sizeDelta.x + CardGap : 276f;
            var viewport = (RectTransform)_content.parent;
            _content.sizeDelta = new Vector2(Mathf.Max(viewport.sizeDelta.x, maps.Length * pitch), _content.sizeDelta.y);
            for (var i = 0; i < maps.Length; i++)
            {
                var map = maps[i];
                Button button;
                RawImage image;
                TMP_Text title;
                GameObject selected = null;
                if (_template != null)
                {
                    var go = Instantiate(_template.gameObject, _content, false);
                    go.name = map.Id;
                    go.SetActive(true);
                    var rect = (RectTransform)go.transform;
                    rect.anchoredPosition = new Vector2(i * pitch, rect.anchoredPosition.y);
                    button = go.GetComponent<Button>();
                    image = go.transform.Find("Preview").GetComponent<RawImage>();
                    title = go.transform.Find("Title").GetComponent<TMP_Text>();
                    go.transform.Find("Description").GetComponent<TMP_Text>().text = map.Description;
                    selected = go.transform.Find("Selected").gameObject;
                }
                else
                {
                    var card = Rect(map.Id, _content, i * 276 + 4, 4, 268, 150);
                    card.gameObject.AddComponent<Image>(); button = card.gameObject.AddComponent<Button>();
                    image = Rect("Preview", card, 8, 8, 112, 112).gameObject.AddComponent<RawImage>(); image.raycastTarget = false;
                    title = Label(card, map.Name, 128, 8, 132, 55, Body);
                    Label(card, map.Description, 128, 65, 132, 78, Caption);
                }
                button.onClick.AddListener(() => _maps.Select(map));
                _cards.Add((map, button, title, selected));
                LoadPreview(map, image, _images.Token);
            }
        }
        private async void LoadPreview(PublishedMap map, RawImage image, CancellationToken token)
        {
            try
            {
                var bytes = await _maps.Preview(map, token);
                if (token.IsCancellationRequested || image == null) return;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes)) { Destroy(texture); return; }
                _textures.Add(texture); image.texture = texture;
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (token.IsCancellationRequested || image == null) return;
                var size = image.rectTransform.sizeDelta;
                Label(image.transform, "Không tải được ảnh", 0, 0, size.x, size.y, Caption);
            }
        }
        private TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, float size)
        {
            var label = Rect("Label", parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _view.Rule.font; label.fontSize = size; label.text = text; // Rule: body face (Status is the title face in skin A)
            label.color = Color.white; label.raycastTarget = false; label.enableWordWrapping = true;
            return label;
        }
        private Button BotButton(Transform parent, string text, float x, float y, float width, float height)
        {
            var rect = Rect(text, parent, x, y, width, height);
            rect.gameObject.AddComponent<Image>().color = new Color(.14f, .25f, .22f);
            var button = rect.gameObject.AddComponent<Button>();
            Label(rect, text, 4, 3, width - 8, height - 6, Caption);
            return button;
        }
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); Place(rect, x, y, width, height); return rect;
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
        private void OnDestroy()
        { _images?.Cancel(); _images?.Dispose(); foreach (var texture in _textures) if (texture != null) Destroy(texture); }
    }
}
