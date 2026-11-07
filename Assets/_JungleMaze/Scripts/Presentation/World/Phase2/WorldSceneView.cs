using System.Collections.Generic;
using JungleBackend.Protocol.Gameplay;
using JungleMaze.App.Flow;
using JungleMaze.App.Match;
using JungleMaze.App.MatchUi;
using JungleMaze.App.Ports;
using JungleMaze.App.World;
using JungleMaze.Core.DI;
using JungleMaze.Presentation.Player;
using JungleMaze.Presentation.UI.Hud;
using JungleMaze.Presentation.World.Loot;
using JungleMaze.World.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JungleMaze.Presentation.World.Phase2
{
    /// <summary>Authoritative world presentation and item intent (online and local offline).</summary>
    public sealed class WorldSceneView : MonoBehaviour
    {
        private WorldClientSession _world;
        private MatchSession _match;
        private IMatchMenuState _menu;
        private ItemCatalog _catalog;
        private float _autoPickupAt;

        private OfflineWorldSession _offlineWorld;
        private OfflineMatchSession _offlineMatch;
        private GameFlow _flow;
        private Transform _hero;
        private IItemIconProvider _icons;

        [SerializeField] private WorldVisualSet visuals;
        /// <summary>Ground loot look; empty loads Resources/GroundLootTuning (built by Jungle Maze → Loot → Build Ground Loot Assets).</summary>
        [SerializeField] private GroundLootTuning lootTuning;
        private GroundLootLayer _loot;
        private bool _lootTuningMissing;
        private Transform _root;
        private Sprite _square;
        private Texture2D _texture;
        private WorldVisualSet _visuals;
        private readonly Dictionary<ushort, WorldObjectView> _views = new Dictionary<ushort, WorldObjectView>();
        private readonly Dictionary<ushort, BoxCollider2D> _colliders = new Dictionary<ushort, BoxCollider2D>();

        [Inject]
        public void Construct(WorldClientSession world, MatchSession match)
        {
            _world = world;
            _match = match;
        }

        [Inject]
        public void Configure(IMatchMenuState menu, ItemCatalog catalog)
        {
            _menu = menu;
            _catalog = catalog;
        }

        /// <summary>Ground item icons; the HUD's normalized catalog, never raw JSurviv art.</summary>
        [Inject]
        public void ConfigureIcons(IItemIconProvider icons) => _icons = icons;

        [Inject]
        public void ConfigureOffline(OfflineWorldSession offlineWorld, OfflineMatchSession offlineMatch, GameFlow flow)
        {
            _offlineWorld = offlineWorld;
            _offlineMatch = offlineMatch;
            _flow = flow;
        }

        private void Start()
        {
            if (_world != null)
            {
                _world.Changed += Refresh;
                _world.Teleported += Teleport;
            }
            if (_offlineWorld != null)
            {
                _offlineWorld.Changed += Refresh;
                _offlineWorld.Teleported += Teleport;
            }
            SceneManager.sceneLoaded += SceneLoaded;
            LevelArtView.Changed += Refresh;
            _visuals = visuals != null ? visuals : Resources.Load<WorldVisualSet>("WorldVisualSet");
            Refresh();
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode) => Refresh();

        private void Teleport(ushort source, uint sequence)
        {
            if (_views.TryGetValue(source, out var entry)) entry.PlayCue(WorldVisualState.Teleported, sequence);
            var isOffline = _flow != null && _flow.IsOffline && _offlineWorld != null;
            var states = isOffline ? _offlineWorld.Objects : (_world != null ? _world.Objects : System.Array.Empty<WorldObjectWire>());
            var destination = FindPortalDestination(states, source);
            if (destination != 0 && _views.TryGetValue(destination, out var pair)) pair.PlayCue(WorldVisualState.Teleported, sequence);
        }

        /// <summary>
        /// Cửa tương tác gần nhất trong tầm tay (F khi không có đồ để nhặt). Cửa tương tác là cửa còn HP; cửa cũ (HP 0,
        /// đóng/mở theo chu kỳ) và cửa đã vỡ bị bỏ qua. 0 nếu không có.
        /// </summary>
        public static ushort NearestDoor(WorldObjectWire[] states, Vector2 hero)
        {
            ushort best = 0; var bestDistance = JungleMaze.World.HazardService.DoorReach;
            foreach (var s in states)
            {
                if (s.Kind != 12 || s.Hp == 0 || (s.Flags & 1) != 0) continue;
                var dx = Mathf.Max(Mathf.Abs(hero.x - s.X) - s.Width / 2f, 0f);
                var dy = Mathf.Max(Mathf.Abs(hero.y - s.Y) - s.Height / 2f, 0f);
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= bestDistance) { bestDistance = d; best = s.Id; }
            }
            return best;
        }

        /// <summary>Portal PairId is a group key, never a world instance ID.</summary>
        public static ushort FindPortalDestination(WorldObjectWire[] states, ushort source)
        {
            ushort pair = 0;
            foreach (var state in states) if (state.Id == source && state.Kind == 11) { pair = state.PairId; break; }
            if (pair == 0) return 0;
            foreach (var state in states) if (state.Kind == 11 && state.Id != source && state.PairId == pair) return state.Id;
            return 0;
        }

        private void Update()
        {
            if (_flow != null && _flow.IsOffline && _offlineWorld != null)
            {
                UpdateOffline();
                return;
            }

            if (_world == null || !_world.IsReady || SceneManager.GetActiveScene().name != "Game" || _match.OwnHp == 0 || (_menu?.BlocksGameplayInput ?? false)) return;
            if (Time.unscaledTime >= _autoPickupAt && _catalog != null)
            {
                _autoPickupAt = Time.unscaledTime + .25f;
                foreach (var o in _world.Objects) if (o.Kind == 100 && Vector2.Distance(new Vector2(_match.Prediction.PredictedX, _match.Prediction.PredictedY), new Vector2(o.X, o.Y)) <= .6f && _catalog.TryGet(o.ItemId, out var item) &&
                    (item.Kind == ItemKind.Ammo || item.Kind == ItemKind.Helmet || item.Kind == ItemKind.Armor)) _world.Send(WorldActionKind.Pickup, o.Id);
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                var camera = Camera.main; if (camera == null) return; var cursor = camera.ScreenToWorldPoint(Input.mousePosition); ushort target = 0; var best = float.MaxValue;
                var own = new Vector2(_match.Prediction.PredictedX, _match.Prediction.PredictedY);
                foreach (var o in _world.Objects) if (o.Kind == 100 && Vector2.Distance(own, new Vector2(o.X, o.Y)) <= 1.2f)
                { var d = ((Vector2)cursor - new Vector2(o.X, o.Y)).sqrMagnitude; if (d < best) { best = d; target = o.Id; } }
                if (target != 0) _world.Send(WorldActionKind.Pickup, target);
                else { var door = NearestDoor(_world.Objects, own); if (door != 0) _world.Send(WorldActionKind.Interact, door); }
            }
            for (var i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) _world.Send(WorldActionKind.Select, 0, (byte)(_world.Inventory != null && _world.Inventory.ActiveWeapon == i ? 255 : i));
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha5 + i))) _world.Send(WorldActionKind.Use, 0, (byte)i);
            }
            if (Input.GetKeyDown(KeyCode.Backspace) && _world.Inventory != null && _world.Inventory.ActiveWeapon >= 0) _world.Send(WorldActionKind.Drop, 0, (byte)_world.Inventory.ActiveWeapon);
            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0 || Input.GetMouseButtonDown(0))
                if (_world.Inventory != null && _world.Inventory.IsUsing) _world.Send(WorldActionKind.CancelUse);
        }

        private void UpdateOffline()
        {
            if (!_offlineWorld.IsReady || SceneManager.GetActiveScene().name != "Game" ||
                (_offlineMatch != null && _offlineMatch.IsDead)) return;

            if (_hero == null)
            {
                var heroObj = GameObject.FindWithTag("Player");
                if (heroObj != null) _hero = heroObj.transform;
                else
                {
                    var heroController = FindAnyObjectByType<HeroController>();
                    if (heroController != null) _hero = heroController.transform;
                }
            }

            if (_hero != null)
            {
                if (_hero.TryGetComponent<HeroController>(out var controller)) controller.MovementMultiplier = _offlineWorld.MovementMultiplier;
                if (_offlineWorld.TryConsumePortalTeleport(out var destination))
                {
                    _hero.position = new Vector3(destination.X, destination.Y, _hero.position.z);
                    if (_hero.TryGetComponent<Rigidbody2D>(out var rb))
                        rb.position = new Vector2(destination.X, destination.Y);
                }
                _offlineWorld.SetPlayerPose(_hero.position.x, _hero.position.y);
            }
            if (_menu?.BlocksGameplayInput ?? false) return;

            if (Time.unscaledTime >= _autoPickupAt && _catalog != null && _hero != null)
            {
                _autoPickupAt = Time.unscaledTime + 0.25f;
                var own = new Vector2(_hero.position.x, _hero.position.y);
                foreach (var o in _offlineWorld.Objects)
                {
                    if (o.Kind == 100 && Vector2.Distance(own, new Vector2(o.X, o.Y)) <= 0.6f &&
                        _catalog.TryGet(o.ItemId, out var item) &&
                        (item.Kind == ItemKind.Ammo || item.Kind == ItemKind.Helmet || item.Kind == ItemKind.Armor))
                    {
                        _offlineWorld.AutoPickup(o.Id);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F) && _hero != null)
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    var cursor = camera.ScreenToWorldPoint(Input.mousePosition);
                    ushort target = 0;
                    var best = float.MaxValue;
                    var own = new Vector2(_hero.position.x, _hero.position.y);
                    foreach (var o in _offlineWorld.Objects)
                    {
                        if (o.Kind == 100 && Vector2.Distance(own, new Vector2(o.X, o.Y)) <= 1.2f)
                        {
                            var d = ((Vector2)cursor - new Vector2(o.X, o.Y)).sqrMagnitude;
                            if (d < best) { best = d; target = o.Id; }
                        }
                    }
                    if (target != 0) _offlineWorld.Pickup(target);
                    else
                    {
                        var door = NearestDoor(_offlineWorld.Objects, own);
                        if (door != 0) _offlineWorld.ToggleDoor(door);
                    }
                }
            }

            for (var i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    var inv = _offlineWorld.Inventory;
                    _offlineWorld.SelectWeapon((byte)(inv != null && inv.ActiveWeapon == i ? 255 : i));
                }
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha5 + i)))
                {
                    _offlineWorld.UseConsumable((byte)i);
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                var inv = _offlineWorld.Inventory;
                if (inv != null && inv.ActiveWeapon >= 0)
                    _offlineWorld.DropWeapon((byte)inv.ActiveWeapon);
            }

            if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0 || Input.GetMouseButtonDown(0))
            {
                var inv = _offlineWorld.Inventory;
                if (inv != null && inv.IsUsing)
                    _offlineWorld.CancelUse();
            }
        }

        private void Refresh()
        {
            if (_root == null)
            {
                _views.Clear();
                _colliders.Clear();
                _loot = null; // its views lived under the destroyed root
                var go = new GameObject("Authoritative World Views");
                _root = go.transform;
                _root.SetParent(transform, false);
            }
            var isOffline = _flow != null && _flow.IsOffline && _offlineWorld != null;
            var ready = isOffline ? _offlineWorld.IsReady : (_world != null && _world.IsReady);
            var show = SceneManager.GetActiveScene().name == "Game" && ready;
            _root.gameObject.SetActive(show);
            if (!show) { _loot?.Clear(); return; }

            if (_square == null)
            {
                _texture = new Texture2D(1, 1);
                _texture.SetPixel(0, 0, Color.white);
                _texture.Apply();
                _square = Sprite.Create(_texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            }
            var sourceObjects = isOffline ? _offlineWorld.Objects : _world.Objects;
            // Ground items (kind 100) float with rarity light in their own pooled layer; WorldObjectView keeps the other kinds.
            var loot = EnsureLootLayer();
            loot?.Apply(sourceObjects, Time.unscaledTimeAsDouble);
            var dynamicStates = new List<WorldObjectWire>();
            // Cover (kind 1–4): offline do scene vẽ; khi có art Level Editor thì LevelArtView đã vẽ hình Cainos.
            var coversDrawn = isOffline || LevelArtView.CoversDrawn;
            foreach (var state in sourceObjects) if ((!coversDrawn || state.Kind > 4) && (loot == null || state.Kind != GroundLootLayer.GroundItemKind)) dynamicStates.Add(state);
            var states = dynamicStates.ToArray();
            // Vật thể có hình Level Editor (rương, bình, bụi, portal, cửa trong JSON) không vẽ thêm art kiểu cũ.
            var drawn = new List<WorldObjectWire>(states.Length);
            foreach (var state in states)
                if (!LevelArtView.TryShowWorldObject(state.Kind, state.X, state.Y, state.Width, state.Height, state.Flags, state.Hp)) drawn.Add(state);
            ApplyViews(drawn.ToArray(), _root, _views, _visuals, _square, _icons);
            ApplyCollision(states);
        }

        private GroundLootLayer EnsureLootLayer()
        {
            if (_loot != null || _lootTuningMissing) return _loot;
            var tuning = lootTuning != null ? lootTuning : Resources.Load<GroundLootTuning>(GroundLootTuning.ResourceName);
            if (tuning == null)
            {
                // Without the tuning asset the old flat icon marker (WorldObjectView) still shows every item.
                _lootTuningMissing = true;
                Debug.LogWarning("[World] GroundLootTuning missing; run Jungle Maze/Loot/Build Ground Loot Assets.");
                return null;
            }
            var go = new GameObject("Ground Loot");
            go.transform.SetParent(_root, false);
            _loot = new GroundLootLayer(go.transform, tuning, _icons, _catalog, _square);
            return _loot;
        }

        private void ApplyCollision(WorldObjectWire[] states)
        {
            var seen = new HashSet<ushort>();
            foreach (var state in states)
            {
                var solid = state.Kind <= 4 || ((state.Kind == 5 || state.Kind == 6 || state.Kind == 13) && (state.Flags & 1) == 0) || (state.Kind == 12 && (state.Flags & 2) != 0);
                if (!solid) continue;
                seen.Add(state.Id);
                if (!_colliders.TryGetValue(state.Id, out var collider))
                {
                    var go = new GameObject("World collision " + state.Id) { layer = LayerMask.NameToLayer("HardCover") };
                    go.transform.SetParent(_root, false);
                    collider = go.AddComponent<BoxCollider2D>();
                    _colliders.Add(state.Id, collider);
                }
                collider.transform.position = new Vector3(state.X, state.Y, 0);
                collider.size = new Vector2(state.Width, state.Height);
            }
            var gone = new List<ushort>();
            foreach (var entry in _colliders) if (!seen.Contains(entry.Key)) gone.Add(entry.Key);
            foreach (var id in gone) { if (_colliders[id] != null) Destroy(_colliders[id].gameObject); _colliders.Remove(id); }
        }

        /// <param name="icons">Optional: ground items (kind 100) show their item icon instead of the blank marker.</param>
        public static void ApplyViews(WorldObjectWire[] states, Transform root, Dictionary<ushort, WorldObjectView> views, WorldVisualSet set, Sprite fallback, IItemIconProvider icons = null)
        {
            var seen = new HashSet<ushort>();
            foreach (var s in states)
            {
                seen.Add(s.Id);
                if (!views.TryGetValue(s.Id, out var view))
                {
                    var go = new GameObject("World " + s.Kind + " #" + s.Id);
                    go.transform.SetParent(root, false);
                    view = go.AddComponent<WorldObjectView>();
                    view.Configure(set);
                    views.Add(s.Id, view);
                }
                var icon = s.Kind == 100 && icons != null && !string.IsNullOrEmpty(s.ItemId) ? icons.GetIcon(s.ItemId) : null;
                if (icon != null) view.Apply(s, icon, true);
                else view.Apply(s, set != null ? set.Find(s.Kind) ?? fallback : fallback);
            }
            var gone = new List<ushort>();
            foreach (var entry in views) if (!seen.Contains(entry.Key)) gone.Add(entry.Key);
            foreach (var id in gone) { if (views[id] != null) Object.Destroy(views[id].gameObject); views.Remove(id); }
        }

        private void OnDestroy()
        {
            if (_world != null) { _world.Changed -= Refresh; _world.Teleported -= Teleport; }
            if (_offlineWorld != null) { _offlineWorld.Changed -= Refresh; _offlineWorld.Teleported -= Teleport; }
            SceneManager.sceneLoaded -= SceneLoaded;
            LevelArtView.Changed -= Refresh;
            if (_root != null) Destroy(_root.gameObject);
            if (_square != null) Destroy(_square);
            if (_texture != null) Destroy(_texture);
        }
    }
}
