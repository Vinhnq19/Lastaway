using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using JungleMaze.App.Flow;
using JungleMaze.App.Match;
using JungleMaze.Core.DI;
using JungleMaze.LevelEditor;
using JungleMaze.Presentation.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace JungleMaze.Presentation.World
{
    /// <summary>
    /// Vẽ map như Level Editor: ground cỏ theo seed, đường đá, viền/tường mép, cỏ rải và hình Cainos của cover/decor,
    /// đọc từ khối <c>editor</c> của map JSON (game logic bỏ qua khối này). Chỉ là hình: collider/cover vẫn do dữ liệu
    /// map và scene quyết định. Map không có khối <c>editor</c> hoặc phòng mạng đang chọn map khác thì giữ cách vẽ cũ.
    /// Vật thể game có trong JSON (rương, bình, bụi, portal, cửa, cover "Phá được") cũng dùng hình Cainos; trạng thái
    /// (HP, vỡ, cửa mở) lấy từ snapshot world qua <see cref="TryShowWorldObject"/>, do <see cref="Phase2.WorldSceneView"/>
    /// gọi. Vật phá được nháy sáng bằng shader <c>JungleMaze/Sprite Hit Flash</c> mỗi lần HP giảm, ẩn khi vỡ.
    /// </summary>
    public sealed class LevelArtView : MonoBehaviour
    {
        private const int GroundOrder = -90, BorderOrder = -89, DecorOrder = -80;
        private const float ShadowAlpha = .3f, OpenDoorAlpha = .35f, FlashSeconds = .14f;
        private const byte DoorKind = 12;
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        [SerializeField] private LevelCatalog catalog;
        [Tooltip("Ẩn nền/viền cũ của MapGridView khi map có art Level Editor (collider biên vẫn giữ).")]
        [SerializeField] private MapGridView grid;
        [Tooltip("Material dùng shader JungleMaze/Sprite Hit Flash cho vật phá được.")]
        [SerializeField] private Material hitFlashMaterial;

        /// <summary>True khi art Level Editor đang hiện: cover đã có hình Cainos, không vẽ khối cover kiểu cũ.</summary>
        public static bool CoversDrawn { get; private set; }

        /// <summary>Art bật/tắt: view world cần vẽ lại để bỏ (hoặc lấy lại) hình kiểu cũ.</summary>
        public static event System.Action Changed;

        /// <summary>Hình Cainos của một vật thể game và trạng thái nháy sáng của nó.</summary>
        private sealed class ObjectArt
        {
            public GameObject Root;
            public readonly List<SpriteRenderer> Body = new List<SpriteRenderer>();
            public bool Breakable;
            public int LastHp = -1;
            public float FlashLeft;
        }

        private static LevelArtView _active;
        /// <summary>Hình Cainos của vật thể game, theo góc dưới-trái bounds (đơn vị 1/100 ô).</summary>
        private readonly Dictionary<(int, int), ObjectArt> _worldObjects = new Dictionary<(int, int), ObjectArt>();
        private readonly List<ObjectArt> _flashing = new List<ObjectArt>();
        private MaterialPropertyBlock _block;
        private HashSet<Vector2Int> _wallCells = new HashSet<Vector2Int>();

        /// <summary>
        /// Vật thể game này có hình Level Editor thì cập nhật hình theo trạng thái (HP giảm → nháy sáng, vỡ → ẩn, cửa mở
        /// → mờ) và trả true; người gọi không tạo hình kiểu cũ nữa. Trả false khi art không hiện hoặc vật không có trong JSON.
        /// </summary>
        public static bool TryShowWorldObject(byte kind, float x, float y, float width, float height, byte flags, ushort hp)
        {
            if (!CoversDrawn || _active == null) return false;
            if (!_active._worldObjects.TryGetValue(Key(x - width / 2f, y - height / 2f), out var art) || art.Root == null) return false;
            var broken = (flags & 1) != 0;
            art.Root.SetActive(!broken);
            if (art.Breakable)
            {
                if (!broken && art.LastHp >= 0 && hp < art.LastHp) _active.Flash(art);
                art.LastHp = hp;
            }
            if (kind == DoorKind)
            {
                var alpha = (flags & 2) != 0 ? 1f : OpenDoorAlpha;
                foreach (var r in art.Body) r.color = new Color(1f, 1f, 1f, alpha);
            }
            return true;
        }

        private static (int, int) Key(float minX, float minY) => (Mathf.RoundToInt(minX * 100f), Mathf.RoundToInt(minY * 100f));

        private void Flash(ObjectArt art)
        {
            if (art.FlashLeft <= 0f) _flashing.Add(art);
            art.FlashLeft = FlashSeconds;
            SetFlash(art, 1f);
        }

        private void SetFlash(ObjectArt art, float amount)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            foreach (var r in art.Body)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetFloat(FlashAmountId, amount);
                r.SetPropertyBlock(_block);
            }
        }

        private void LateUpdate()
        {
            for (var i = _flashing.Count - 1; i >= 0; i--)
            {
                var art = _flashing[i];
                art.FlashLeft -= Time.deltaTime;
                if (art.FlashLeft > 0f) { SetFlash(art, art.FlashLeft / FlashSeconds); continue; }
                art.FlashLeft = 0f;
                SetFlash(art, 0f);
                _flashing.RemoveAt(i);
            }
        }

        private GameFlow _flow;
        private RoomMapSession _roomMaps;
        private string _localHash;
        private bool _built, _applied;
        private Transform _root;
        private readonly List<Object> _owned = new List<Object>();
        private readonly Dictionary<Sprite, Tile> _tiles = new Dictionary<Sprite, Tile>();

        [Inject] public void Construct(GameFlow flow) => _flow = flow;
        [Inject] public void BindRoomMaps(RoomMapSession roomMaps) => _roomMaps = roomMaps;

        private void Start()
        {
            // Cùng map với world offline: map offline riêng nếu có, không thì map_demo của server.
            var runtime = Path.Combine(Application.streamingAssetsPath, "JungleMaze", "runtime");
            var path = Path.Combine(runtime, JungleMaze.App.Ports.RuntimeDataFiles.OfflineMap);
            if (!File.Exists(path)) path = Path.Combine(runtime, JungleMaze.App.Ports.RuntimeDataFiles.ServerMap);
            if (catalog == null || !File.Exists(path)) return;
            var json = File.ReadAllText(path);
            _localHash = Hash(json);
            LevelDocument doc;
            try
            {
                if (!json.Contains("\"editor\"")) return;
                doc = LevelDocument.FromJson(json, catalog.BaseSizeOf, catalog.ShapeOf);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[LevelArt] Không đọc được art của map: " + e.Message);
                return;
            }
            Build(doc);
            Apply();
        }

        private void Update() => Apply();

        /// <summary>Hiện khi chơi offline hoặc phòng mạng đang chọn đúng map đóng gói.</summary>
        private void Apply()
        {
            if (!_built) return;
            var offline = _flow == null || _flow.IsOffline;
            var sameMap = _roomMaps == null || !_roomMaps.HasState || _roomMaps.State.MapHash == _localHash;
            var show = offline || sameMap;
            // MapGridView dựng nền/tường trong Start của nó, có thể sau Start của view này: đợi nó dựng xong rồi mới ẩn.
            var gridReady = grid == null || grid.MapSizeCells > 0;
            if (_applied && _root.gameObject.activeSelf == show) return;
            _root.gameObject.SetActive(show);
            var changed = CoversDrawn != show;
            CoversDrawn = show;
            if (changed) Changed?.Invoke();
            if (!gridReady) return;
            _applied = true;
            if (grid != null)
                foreach (var r in grid.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = !show;
        }

        private void Build(LevelDocument doc)
        {
            _root = new GameObject("Level art").transform;
            _root.SetParent(transform, false);

            var tileGrid = new GameObject("Ground").AddComponent<Grid>();
            tileGrid.transform.SetParent(_root, false);
            var ground = CreateTilemap("Ground tiles", tileGrid.transform, GroundOrder);
            var border = CreateTilemap("Border tiles", tileGrid.transform, BorderOrder);
            bool IsPath(int x, int y) => doc.Path.Contains(new Vector2Int(x, y));

            var tiles = new TileBase[doc.Width * doc.Height];
            for (var y = 0; y < doc.Height; y++)
            for (var x = 0; x < doc.Width; x++)
            {
                var stone = LevelGround.StoneCell(doc.Seed, x, y, IsPath);
                tiles[y * doc.Width + x] = TileOf(At(catalog.GrassGrid, stone >= 0 ? stone : LevelGround.GrassCell(doc.Seed, x, y)));
            }
            ground.SetTilesBlock(new BoundsInt(0, 0, 0, doc.Width, doc.Height, 1), tiles);
            foreach (var cell in LevelGround.Border(doc.Width, doc.Height, doc.Seed))
                border.SetTile(new Vector3Int(cell.X, cell.Y, 0), TileOf(At(catalog.WallGrid, cell.Tile)));

            var occupied = new HashSet<Vector2Int>();
            foreach (var p in doc.Placements)
            {
                var b = p.Bounds;
                for (var y = Mathf.FloorToInt(b.MinY); y < Mathf.CeilToInt(b.MaxY); y++)
                for (var x = Mathf.FloorToInt(b.MinX); x < Mathf.CeilToInt(b.MaxX); x++) occupied.Add(new Vector2Int(x, y));
            }
            var sprinkleRoot = new GameObject("Grass sprinkles").transform;
            sprinkleRoot.SetParent(_root, false);
            foreach (var spot in LevelGround.Sprinkles(doc.Seed, doc.Width, doc.Height,
                         (x, y) => occupied.Contains(new Vector2Int(x, y)) || LevelGround.StoneCell(doc.Seed, x, y, IsPath) >= 0,
                         doc.Decor, catalog.Sprinkles.Length))
            {
                var r = new GameObject("Grass").AddComponent<SpriteRenderer>();
                r.transform.SetParent(sprinkleRoot, false);
                r.transform.position = spot.position;
                r.sprite = catalog.Sprinkles[spot.sprite];
                r.sortingLayerID = SortingLayers.IdOf(SortingLayers.Ground);
                r.sortingOrder = DecorOrder;
                UseEditorMaterial(r);
            }

            var objects = new GameObject("Placements").transform;
            objects.SetParent(_root, false);
            _wallCells = LevelWalls.Cells(doc.Placements);
            foreach (var p in doc.Placements)
            {
                if (p.Group == LevelItemGroup.Spawn) continue;
                var art = BuildPlacement(objects, doc, p);
                // Vật thể game (kể cả cover "Phá được") ghép với snapshot theo footprint.
                if (art != null && (p.Group == LevelItemGroup.WorldObject || p.BreaksInGame))
                    _worldObjects[Key(p.Bounds.MinX, p.Bounds.MinY)] = art;
            }
            _active = this;
            _built = true;
        }

        private ObjectArt BuildPlacement(Transform parent, LevelDocument doc, LevelPlacement p)
        {
            var type = catalog.Find(p.Type);
            var parts = PartsFor(doc, p, type);
            if (parts == null || parts.Length == 0) return null; // loại ngoài catalog: giữ hình kiểu cũ của game
            var b = p.Bounds;
            var width = b.MaxX - b.MinX;
            var art = new ObjectArt { Breakable = p.BreaksInGame };
            var root = new GameObject(p.Type);
            art.Root = root;
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(b.MinX, b.MinY, 0f);
            var group = root.AddComponent<SortingGroup>();
            if (p.Group == LevelItemGroup.Decoration)
            {
                group.sortingLayerID = SortingLayers.IdOf(SortingLayers.Ground);
                group.sortingOrder = DecorOrder + 1;
            }
            else
            {
                // Cùng công thức Y-sort với hero, tính tại chân (collider thấp nhất) để đi sau thân cây/tượng.
                var foot = b.MinY;
                if (p.OnGrid && p.Colliders != null && p.Colliders.Length > 0)
                {
                    foot = float.MaxValue;
                    foreach (var rect in p.GameRects()) foot = Mathf.Min(foot, rect.MinY);
                }
                group.sortingLayerID = SortingLayers.IdOf(SortingLayers.WorldDynamic);
                group.sortingOrder = DepthSortingSystem.ComputeOrder(foot, DepthSortingSystem.DefaultPrecision);
            }
            var shift = type != null ? (width - type.Footprint.x) / 2f : 0f;
            foreach (var part in parts)
            {
                var r = new GameObject("Part").AddComponent<SpriteRenderer>();
                r.transform.SetParent(root.transform, false);
                r.sprite = part.Sprite;
                r.sortingOrder = part.Rank;
                var offset = part.Offset + new Vector2(shift, 0f);
                if (p.FlipX) offset.x = width - offset.x;
                r.transform.localPosition = offset;
                r.transform.localScale = new Vector3(part.Scale.x, part.Scale.y, 1f);
                r.flipX = part.FlipX != p.FlipX;
                r.color = part.Shadow ? new Color(0f, 0f, 0f, ShadowAlpha) : Color.white;
                UseEditorMaterial(r);
                if (part.Shadow) continue;
                art.Body.Add(r);
                // Vật phá được: thân dùng shader nháy sáng (bóng giữ material thường để không lóe trắng).
                if (art.Breakable && hitFlashMaterial != null) r.sharedMaterial = hitFlashMaterial;
            }
            return art;
        }

        /// <summary>Cùng material Unlit với Level Editor để màu trong trận giống hệt lúc xếp map (không bị ánh sáng 2D nhuộm).</summary>
        private void UseEditorMaterial(Renderer renderer)
        {
            if (catalog.SpriteMaterial != null) renderer.sharedMaterial = catalog.SpriteMaterial;
        }

        /// <summary>Cùng cách chọn mẫu hình theo seed và ô như Level Editor, để game và editor giống hệt nhau.</summary>
        private LevelVisualPart[] PartsFor(LevelDocument doc, LevelPlacement p, LevelItemType type)
        {
            if (type == null || type.Variants == null || type.Variants.Length == 0) return null;
            if (p.Type == LevelWalls.Id && p.OnGrid) return LevelWalls.Parts(catalog.WallGrid, _wallCells, p.CellX, p.CellY);
            var cellX = p.OnGrid ? p.CellX : Mathf.FloorToInt(p.X);
            var cellY = p.OnGrid ? p.CellY : Mathf.FloorToInt(p.Y);
            var pick = LevelGround.Hash01(doc.Seed, cellX, cellY, 77);
            return type.Variants[Mathf.Min(type.Variants.Length - 1, (int)(pick * type.Variants.Length))].Parts;
        }

        private Tilemap CreateTilemap(string name, Transform parent, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerID = SortingLayers.IdOf(SortingLayers.Ground);
            renderer.sortingOrder = order;
            UseEditorMaterial(renderer);
            return tilemap;
        }

        private Tile TileOf(Sprite sprite)
        {
            if (sprite == null) return null;
            if (_tiles.TryGetValue(sprite, out var tile)) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            _tiles[sprite] = tile;
            _owned.Add(tile);
            return tile;
        }

        private static Sprite At(Sprite[] sprites, int index) => index >= 0 && index < sprites.Length ? sprites[index] : null;

        /// <summary>Cùng dạng chuẩn hoá với hash map phòng mạng: bỏ BOM, CRLF → LF, SHA-256 hex.</summary>
        private static string Hash(string json)
        {
            var text = json.TrimStart('﻿').Replace("\r\n", "\n");
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var v in bytes) sb.Append(v.ToString("x2"));
                return sb.ToString();
            }
        }

        private void OnDestroy()
        {
            if (_active == this) { _active = null; CoversDrawn = false; }
            foreach (var o in _owned) if (o != null) Destroy(o);
        }
    }
}
