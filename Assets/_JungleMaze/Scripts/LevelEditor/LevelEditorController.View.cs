using System.Collections.Generic;
using JungleMaze.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JungleMaze.LevelEditor
{
    public sealed partial class LevelEditorController
    {
        private const int FlatOrder = -30000, SpawnOrder = 30000, GroundOrder = -32000;
        private static readonly Color SelectedColor = new Color(1f, .85f, .1f);
        private static readonly Color HoverColor = Color.white;
        private static readonly Color BlockedColor = new Color(1f, .25f, .2f);

        private sealed class PlacementView
        {
            public GameObject Root;
            public SpriteRenderer Sprite;
            public LineRenderer Outline;
        }

        private readonly List<PlacementView> _views = new List<PlacementView>();
        private Transform _objectsRoot;
        private PlacementView _ghost;
        private Tilemap _tilemap;
        private LineRenderer _border;
        private Tile[] _groundTiles;
        private Sprite _square, _circle;
        private readonly List<Object> _owned = new List<Object>();
        private bool _showOutlines = true;

        private void InitializeView()
        {
            _square = MakeSprite(false);
            _circle = MakeSprite(true);

            var grid = new GameObject("Ground Grid").AddComponent<Grid>();
            grid.transform.SetParent(transform, false);
            var tilemapObject = new GameObject("Ground Tilemap");
            tilemapObject.transform.SetParent(grid.transform, false);
            _tilemap = tilemapObject.AddComponent<Tilemap>();
            var tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingOrder = GroundOrder;
            if (catalog.SpriteMaterial != null) tilemapRenderer.sharedMaterial = catalog.SpriteMaterial;

            _groundTiles = new Tile[Mathf.Max(1, catalog.GroundTiles.Length)];
            for (var i = 0; i < _groundTiles.Length; i++)
            {
                var sprite = i < catalog.GroundTiles.Length && catalog.GroundTiles[i] != null ? catalog.GroundTiles[i] : _square;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                // Ô tilemap = 1 world unit; sprite có PPU khác thì co giãn cho khít ô.
                var size = sprite.bounds.size;
                if (size.x > 0 && size.y > 0 && (!Mathf.Approximately(size.x, 1f) || !Mathf.Approximately(size.y, 1f)))
                {
                    tile.transform = Matrix4x4.Scale(new Vector3(1f / size.x, 1f / size.y, 1f));
                    tile.flags = TileFlags.LockTransform;
                }
                if (sprite == _square) tile.color = new Color(.22f, .36f, .26f);
                _groundTiles[i] = tile;
                _owned.Add(tile);
            }

            _border = CreateLine("Map border", transform, 4);
            _border.widthMultiplier = .15f;
            _border.startColor = _border.endColor = new Color(1f, .8f, .3f, .9f);
            _border.sortingOrder = SpawnOrder + 1;

            _objectsRoot = new GameObject("Placements").transform;
            _objectsRoot.SetParent(transform, false);
            _ghost = CreateView(transform, "Ghost");
            _ghost.Root.SetActive(false);
        }

        private void RebuildAll()
        {
            GenerateGround();
            SyncViews();
        }

        /// <summary>Phủ lại Tilemap W×H bằng ground tile đang chọn. Chỉ gọi khi map đổi kích thước/tile.</summary>
        private void GenerateGround()
        {
            _doc.GroundTile = Mathf.Clamp(_doc.GroundTile, 0, _groundTiles.Length - 1);
            _tilemap.ClearAllTiles();
            var tiles = new TileBase[_doc.Width * _doc.Height];
            for (var i = 0; i < tiles.Length; i++) tiles[i] = _groundTiles[_doc.GroundTile];
            _tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, _doc.Width, _doc.Height, 1), tiles);
            SetRect(_border, 0, 0, _doc.Width, _doc.Height);
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
            var color = selected ? SelectedColor : hovered ? HoverColor : type != null ? type.Tint : Color.magenta;
            color.a = selected || hovered ? 1f : .55f;
            view.Outline.startColor = view.Outline.endColor = color;
            view.Outline.widthMultiplier = selected ? .09f : .045f;
        }

        private void UpdateGhost()
        {
            var show = _tool == Tool.Place && _brush != null && !IsPointerOverUi() && !_panning;
            _ghost.Root.SetActive(show);
            if (!show) return;
            var candidate = CreateFromBrush(ClampToMap(SnapPoint(_mouseWorld)));
            var blocked = IsBlocked(candidate);
            ApplyPlacement(_ghost, candidate, _brush, .6f);
            _ghost.Sprite.sortingOrder = SpawnOrder + 2;
            _ghost.Outline.enabled = true;
            _ghost.Outline.startColor = _ghost.Outline.endColor = blocked ? BlockedColor : SelectedColor;
            _ghost.Outline.widthMultiplier = .08f;
        }

        /// <summary>
        /// Gợi ý nhanh theo luật của game: cover/vật thể game không được chồng lên nhau; spawn không nằm trên vật
        /// chặn đường hoặc hazard. Kết luận cuối cùng vẫn là nút "Kiểm tra".
        /// </summary>
        private bool IsBlocked(LevelPlacement candidate)
        {
            if (candidate.Group == LevelItemGroup.Decoration) return false;
            var bounds = candidate.Bounds;
            foreach (var other in _doc.Placements)
            {
                if (other.Group == LevelItemGroup.Decoration) continue;
                if (candidate.Group == LevelItemGroup.Spawn)
                {
                    if (other.Group == LevelItemGroup.Spawn || other.Type == nameof(WorldObjectKind.Bush)) continue;
                    if (other.Bounds.IntersectsCircle(new WorldPoint(candidate.X, candidate.Y), LevelDocument.SpawnSize / 2f)) return true;
                }
                else if (other.Group != LevelItemGroup.Spawn && bounds.Overlaps(other.Bounds)) return true;
            }
            return false;
        }

        private void ApplyPlacement(PlacementView view, LevelPlacement p, LevelItemType type, float alpha)
        {
            view.Root.transform.position = new Vector3(p.X, p.Y, 0f);
            var spawn = p.Group == LevelItemGroup.Spawn;
            var sprite = type != null && type.Sprite != null ? type.Sprite : spawn ? _circle : _square;
            var tint = type != null && type.Sprite != null ? Color.white : type != null ? type.Tint : Color.magenta;
            tint.a *= alpha;
            view.Sprite.sprite = sprite;
            view.Sprite.color = tint;

            // Sprite giữ tỉ lệ khung footprint gốc, rồi xoay 90° và lật ngang (scale x = -1) quanh tâm footprint.
            var size = sprite.bounds.size;
            var sx = p.BaseWidth * p.Scale / Mathf.Max(.0001f, size.x) * (p.FlipX ? -1f : 1f);
            var sy = p.BaseHeight * p.Scale / Mathf.Max(.0001f, size.y);
            var rotation = Quaternion.Euler(0f, 0f, p.Rotation * 90f);
            var spriteTransform = view.Sprite.transform;
            spriteTransform.localScale = new Vector3(sx, sy, 1f);
            spriteTransform.localRotation = rotation;
            spriteTransform.localPosition = rotation * new Vector3(-sprite.bounds.center.x * sx, -sprite.bounds.center.y * sy, 0f);

            var flat = spawn || (type != null && type.Flat);
            view.Sprite.sortingOrder = spawn ? SpawnOrder : flat ? FlatOrder : Mathf.Clamp(-Mathf.RoundToInt(p.Bounds.MinY * 100f), -29000, 29000);
            SetRect(view.Outline, -p.Width / 2f, -p.Height / 2f, p.Width / 2f, p.Height / 2f);
            view.Outline.sortingOrder = SpawnOrder - 1;
        }

        private PlacementView CreateView(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(root.transform, false);
            var sprite = spriteObject.AddComponent<SpriteRenderer>();
            if (catalog.SpriteMaterial != null) sprite.sharedMaterial = catalog.SpriteMaterial;
            return new PlacementView { Root = root, Sprite = sprite, Outline = CreateLine("Footprint", root.transform, 4) };
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
