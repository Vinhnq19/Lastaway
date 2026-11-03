using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace JungleMaze.LevelEditor
{
    public sealed partial class LevelEditorController
    {
        private const int GroundOrder = -32000, GridOrder = GroundOrder + 1, BorderOrder = GroundOrder + 2, SprinkleOrder = GroundOrder + 3;
        private const int SpawnOrder = 30000, OverlayOrder = 31000;
        private const int GridMajorEvery = 5;
        private const float ShadowAlpha = .3f;
        private const float GridMinorMinPixels = 6f; // ô nhỏ hơn số pixel này thì ẩn lưới phụ cho đỡ rối
        private static readonly Color GridMinorColor = new Color(1f, 1f, 1f, .1f);
        private static readonly Color GridMajorColor = new Color(1f, 1f, 1f, .28f);
        private static readonly Color SelectedColor = new Color(1f, .85f, .1f);
        private static readonly Color HoverColor = Color.white;
        private static readonly Color BlockedColor = new Color(1f, .25f, .2f);
        private static readonly Color FootColor = new Color(1f, .3f, .3f, .9f);
        private static readonly Color LegacyColor = new Color(1f, 0f, 1f, .45f);

        private sealed class PlacementView
        {
            public GameObject Root;
            public SortingGroup Group;
            public readonly List<SpriteRenderer> Parts = new List<SpriteRenderer>();
            public LineRenderer Outline;
            public readonly List<LineRenderer> Feet = new List<LineRenderer>();
        }

        private readonly List<PlacementView> _views = new List<PlacementView>();
        private Transform _objectsRoot, _sprinkleRoot;
        private readonly List<SpriteRenderer> _sprinkles = new List<SpriteRenderer>();
        private PlacementView _ghost;
        private Tilemap _ground, _border;
        private readonly Dictionary<Sprite, Tile> _tiles = new Dictionary<Sprite, Tile>();
        private Sprite _square, _circle;
        private readonly List<Object> _owned = new List<Object>();
        private bool _showOutlines = true;
        private bool _showGrid = true;
        private MeshRenderer _gridMinor, _gridMajor;

        private void InitializeView()
        {
            _square = MakeSprite(false);
            _circle = MakeSprite(true);

            var grid = new GameObject("Ground Grid").AddComponent<Grid>();
            grid.transform.SetParent(transform, false);
            _ground = CreateTilemap("Ground Tilemap", grid.transform, GroundOrder);
            _border = CreateTilemap("Border Tilemap", grid.transform, BorderOrder);
            _gridMinor = CreateGridLayer("Grid minor", grid.transform);
            _gridMajor = CreateGridLayer("Grid major", grid.transform);

            _sprinkleRoot = new GameObject("Grass Sprinkles").transform;
            _sprinkleRoot.SetParent(transform, false);
            _objectsRoot = new GameObject("Placements").transform;
            _objectsRoot.SetParent(transform, false);
            _ghost = CreateView(transform, "Ghost");
            _ghost.Root.SetActive(false);
        }

        private Tilemap CreateTilemap(string name, Transform parent, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Individual;
            if (catalog.SpriteMaterial != null) renderer.sharedMaterial = catalog.SpriteMaterial;
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

        private static Sprite At(Sprite[] grid, int index) => index >= 0 && index < grid.Length ? grid[index] : null;

        private void RebuildAll()
        {
            GenerateGround();
            SyncViews();
        }

        /// <summary>Phủ lại ground (cỏ theo seed + đường đá), viền/tường mép, lưới và cỏ rải. Gọi khi đổi kích thước/seed/undo.</summary>
        private void GenerateGround()
        {
            _ground.ClearAllTiles();
            var tiles = new TileBase[_doc.Width * _doc.Height];
            for (var y = 0; y < _doc.Height; y++)
            for (var x = 0; x < _doc.Width; x++) tiles[y * _doc.Width + x] = GroundTileAt(x, y);
            _ground.SetTilesBlock(new BoundsInt(0, 0, 0, _doc.Width, _doc.Height, 1), tiles);

            _border.ClearAllTiles();
            foreach (var cell in LevelGround.Border(_doc.Width, _doc.Height, _doc.Seed))
                _border.SetTile(new Vector3Int(cell.X, cell.Y, 0), TileOf(At(catalog.WallGrid, cell.Tile)));

            BuildGrid(_gridMinor, false, GridMinorColor);
            BuildGrid(_gridMajor, true, GridMajorColor);
            RebuildSprinkles();
        }

        private TileBase GroundTileAt(int x, int y)
        {
            var stone = LevelGround.StoneCell(_doc.Seed, x, y, IsPath);
            return TileOf(At(catalog.GrassGrid, stone >= 0 ? stone : LevelGround.GrassCell(_doc.Seed, x, y)));
        }

        private bool IsPath(int x, int y) => _doc.Path.Contains(new Vector2Int(x, y));

        /// <summary>Tô/xoá một ô đường chỉ đổi tile của 3×3 ô quanh nó (viền tự nối theo ô kề).</summary>
        private void RefreshGroundAround(Vector2Int cell)
        {
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                var x = cell.x + dx;
                var y = cell.y + dy;
                if (x < 0 || y < 0 || x >= _doc.Width || y >= _doc.Height) continue;
                _ground.SetTile(new Vector3Int(x, y, 0), GroundTileAt(x, y));
            }
        }

        /// <summary>Cỏ/hoa nhỏ rải theo seed trên ô trống; vật chặn, spawn, decor và đường đá đẩy cỏ đi.</summary>
        private void RebuildSprinkles()
        {
            var occupied = new HashSet<Vector2Int>();
            foreach (var p in _doc.Placements)
            {
                var b = p.Bounds;
                for (var y = Mathf.FloorToInt(b.MinY); y < Mathf.CeilToInt(b.MaxY); y++)
                for (var x = Mathf.FloorToInt(b.MinX); x < Mathf.CeilToInt(b.MaxX); x++) occupied.Add(new Vector2Int(x, y));
            }
            var spots = LevelGround.Sprinkles(_doc.Seed, _doc.Width, _doc.Height,
                (x, y) => occupied.Contains(new Vector2Int(x, y)) || LevelGround.StoneCell(_doc.Seed, x, y, IsPath) >= 0,
                _doc.Decor, catalog.Sprinkles.Length);
            while (_sprinkles.Count < spots.Count)
            {
                var r = new GameObject("Grass").AddComponent<SpriteRenderer>();
                r.transform.SetParent(_sprinkleRoot, false);
                r.sortingOrder = SprinkleOrder;
                if (catalog.SpriteMaterial != null) r.sharedMaterial = catalog.SpriteMaterial;
                _sprinkles.Add(r);
            }
            for (var i = 0; i < _sprinkles.Count; i++)
            {
                var active = i < spots.Count;
                _sprinkles[i].gameObject.SetActive(active);
                if (!active) continue;
                _sprinkles[i].sprite = catalog.Sprinkles[spots[i].sprite];
                _sprinkles[i].transform.position = spots[i].position;
            }
        }

        /// <summary>Đồng bộ số view với số placement rồi vẽ lại tất cả; O(n), gọi sau mỗi thao tác.</summary>
        private void SyncViews()
        {
            while (_views.Count < _doc.Placements.Count) _views.Add(CreateView(_objectsRoot, "Placement"));
            for (var i = 0; i < _views.Count; i++)
            {
                var active = i < _doc.Placements.Count;
                _views[i].Root.SetActive(active);
                if (active) RefreshView(i);
            }
            RebuildSprinkles();
        }

        private void RefreshView(int index)
        {
            var p = _doc.Placements[index];
            var view = _views[index];
            view.Root.name = $"{p.Type} #{index}";
            ApplyPlacement(view, p, catalog.Find(p.Type), 1f);
            ApplyOutline(view, index);
        }

        private void RefreshHighlights()
        {
            for (var i = 0; i < _doc.Placements.Count && i < _views.Count; i++) ApplyOutline(_views[i], i);
        }

        private void ApplyOutline(PlacementView view, int index)
        {
            var p = _doc.Placements[index];
            var type = catalog.Find(p.Type);
            var selected = index == _selected;
            var hovered = index == _hover;
            view.Outline.enabled = _showOutlines || selected || hovered;
            var color = selected ? SelectedColor : hovered ? HoverColor : !p.OnGrid ? LegacyColor : type != null ? type.Tint : Color.magenta;
            color.a = selected || hovered ? 1f : .5f;
            view.Outline.startColor = view.Outline.endColor = color;
            view.Outline.widthMultiplier = selected ? .08f : .04f;
            foreach (var foot in view.Feet) foot.enabled = foot.enabled && (_showOutlines || selected || hovered);
        }

        private void UpdateGhost()
        {
            var show = _tool == Tool.Place && _brush != null && !IsPointerOverUi() && !_panning;
            _ghost.Root.SetActive(show);
            if (!show) return;
            var candidate = CreateFromBrush(BrushOrigin(_mouseCell));
            var blocked = !CanPlace(candidate, -1);
            ApplyPlacement(_ghost, candidate, _brush, .6f);
            _ghost.Group.sortingOrder = OverlayOrder;
            _ghost.Outline.enabled = true;
            _ghost.Outline.startColor = _ghost.Outline.endColor = blocked ? BlockedColor : SelectedColor;
            _ghost.Outline.widthMultiplier = .07f;
        }

        private void ApplyPlacement(PlacementView view, LevelPlacement p, LevelItemType type, float alpha)
        {
            var b = p.Bounds;
            view.Root.transform.position = new Vector3(b.MinX, b.MinY, 0f);
            var width = b.MaxX - b.MinX;
            var height = b.MaxY - b.MinY;

            var parts = PartsFor(p, type);
            var used = 0;
            if (parts != null && parts.Length > 0)
            {
                // Vật lệch ô (map cũ) có thể rộng hơn footprint của catalog: căn hình giữa cạnh đáy.
                var shift = type != null ? (width - type.Footprint.x) / 2f : 0f;
                foreach (var part in parts)
                {
                    var r = PartRenderer(view, used++);
                    r.sprite = part.Sprite;
                    r.sortingOrder = part.Rank;
                    var offset = part.Offset + new Vector2(shift, 0f);
                    if (p.FlipX) offset.x = width - offset.x;
                    r.transform.localPosition = offset;
                    r.transform.localScale = new Vector3(part.Scale.x, part.Scale.y, 1f);
                    r.flipX = part.FlipX != p.FlipX;
                    // Cainos vẽ bóng bằng shader riêng; ở đây dùng Sprite-Unlit nên tự làm bóng đen trong suốt.
                    r.color = part.Shadow ? new Color(0f, 0f, 0f, ShadowAlpha * alpha) : new Color(1f, 1f, 1f, alpha);
                }
            }
            else
            {
                // Spawn hoặc loại không còn trong catalog: khối màu đúng footprint.
                var r = PartRenderer(view, used++);
                var spawn = p.Group == LevelItemGroup.Spawn;
                r.sprite = spawn ? _circle : _square;
                r.sortingOrder = 0;
                var size = spawn ? LevelDocument.SpawnSize : 1f;
                r.transform.localPosition = new Vector3(width / 2f, height / 2f, 0f);
                r.transform.localScale = new Vector3(spawn ? size : width, spawn ? size : height, 1f);
                r.flipX = false;
                var tint = type != null ? type.Tint : LegacyColor;
                tint.a *= alpha;
                r.color = tint;
            }
            for (var i = 0; i < view.Parts.Count; i++) view.Parts[i].gameObject.SetActive(i < used);

            // Vật đứng thấp hơn (y nhỏ) vẽ đè vật phía sau; spawn luôn nổi trên cùng.
            view.Group.sortingOrder = p.Group == LevelItemGroup.Spawn ? SpawnOrder :
                p.Group == LevelItemGroup.Decoration ? SprinkleOrder + 1 : Mathf.Clamp(-Mathf.RoundToInt(b.MinY * 100f), -29000, 29000);
            SetRect(view.Outline, 0, 0, width, height);
            view.Outline.sortingOrder = OverlayOrder;

            var feet = 0;
            if (p.Group == LevelItemGroup.Cover && p.OnGrid && p.Colliders != null)
                foreach (var rect in p.GameRects())
                {
                    var line = FootLine(view, feet++);
                    SetRect(line, rect.MinX - b.MinX, rect.MinY - b.MinY, rect.MaxX - b.MinX, rect.MaxY - b.MinY);
                    line.enabled = true;
                }
            for (var i = feet; i < view.Feet.Count; i++) view.Feet[i].enabled = false;
        }

        private LevelVisualPart[] PartsFor(LevelPlacement p, LevelItemType type)
        {
            if (type == null || type.Variants == null || type.Variants.Length == 0) return null;
            var cellX = p.OnGrid ? p.CellX : Mathf.FloorToInt(p.X);
            var cellY = p.OnGrid ? p.CellY : Mathf.FloorToInt(p.Y);
            var pick = LevelGround.Hash01(_doc.Seed, cellX, cellY, 77);
            return type.Variants[Mathf.Min(type.Variants.Length - 1, (int)(pick * type.Variants.Length))].Parts;
        }

        private SpriteRenderer PartRenderer(PlacementView view, int index)
        {
            while (view.Parts.Count <= index)
            {
                var r = new GameObject("Part").AddComponent<SpriteRenderer>();
                r.transform.SetParent(view.Root.transform, false);
                if (catalog.SpriteMaterial != null) r.sharedMaterial = catalog.SpriteMaterial;
                view.Parts.Add(r);
            }
            return view.Parts[index];
        }

        private LineRenderer FootLine(PlacementView view, int index)
        {
            while (view.Feet.Count <= index)
            {
                var line = CreateLine("Foot collider", view.Root.transform, 4);
                line.startColor = line.endColor = FootColor;
                line.widthMultiplier = .03f;
                line.sortingOrder = OverlayOrder + 1;
                view.Feet.Add(line);
            }
            return view.Feet[index];
        }

        private PlacementView CreateView(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return new PlacementView { Root = root, Group = root.AddComponent<SortingGroup>(), Outline = CreateLine("Footprint", root.transform, 4) };
        }

        private LineRenderer CreateLine(string name, Transform parent, int points)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = points;
            line.numCornerVertices = 0;
            line.widthMultiplier = .05f;
            if (catalog.SpriteMaterial != null) line.sharedMaterial = catalog.SpriteMaterial;
            return line;
        }

        private static void SetRect(LineRenderer line, float minX, float minY, float maxX, float maxY)
        {
            line.SetPosition(0, new Vector3(minX, minY, 0f));
            line.SetPosition(1, new Vector3(maxX, minY, 0f));
            line.SetPosition(2, new Vector3(maxX, maxY, 0f));
            line.SetPosition(3, new Vector3(minX, maxY, 0f));
        }

        // ---------- Lưới ----------

        private MeshRenderer CreateGridLayer(string name, Transform parent)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            var mesh = new Mesh { name = "LE " + name };
            _owned.Add(mesh);
            layer.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = layer.AddComponent<MeshRenderer>();
            if (catalog.SpriteMaterial != null) renderer.sharedMaterial = catalog.SpriteMaterial;
            renderer.sortingOrder = GridOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        /// <summary>
        /// Lưới 1 unit khớp ô Tilemap (topology Lines nên luôn nét 1 pixel dù zoom). Lớp chính vẽ mỗi
        /// <see cref="GridMajorEvery"/> ô, lớp phụ vẽ các ô còn lại; mép map đã có viền đá.
        /// </summary>
        private void BuildGrid(MeshRenderer layer, bool major, Color color)
        {
            var vertices = new List<Vector3>();
            for (var x = 1; x < _doc.Width; x++)
            {
                if ((x % GridMajorEvery == 0) != major) continue;
                vertices.Add(new Vector3(x, 0f, 0f));
                vertices.Add(new Vector3(x, _doc.Height, 0f));
            }
            for (var y = 1; y < _doc.Height; y++)
            {
                if ((y % GridMajorEvery == 0) != major) continue;
                vertices.Add(new Vector3(0f, y, 0f));
                vertices.Add(new Vector3(_doc.Width, y, 0f));
            }
            var indices = new int[vertices.Count];
            var colors = new Color[vertices.Count];
            for (var i = 0; i < indices.Length; i++) { indices[i] = i; colors[i] = color; }

            var mesh = layer.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>Ẩn/hiện lưới theo toggle và mức zoom; gọi mỗi frame (rẻ, chỉ bật/tắt renderer).</summary>
        private void UpdateGridVisibility()
        {
            var pixelsPerUnit = Screen.height / Mathf.Max(.01f, viewCamera.orthographicSize * 2f);
            _gridMajor.enabled = _showGrid;
            _gridMinor.enabled = _showGrid && pixelsPerUnit >= GridMinorMinPixels;
        }

        private Sprite MakeSprite(bool circle)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = circle ? "LE circle" : "LE square" };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x + .5f - size / 2f;
                var dy = y + .5f - size / 2f;
                var inside = !circle || dx * dx + dy * dy <= size * size / 4f;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            _owned.Add(texture);
            _owned.Add(sprite);
            return sprite;
        }

        private void OnDestroy()
        {
            foreach (var o in _owned) if (o != null) Destroy(o);
        }
    }
}
