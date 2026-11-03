using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>
    /// Scene LevelEditor (chạy bằng Play). Ground là Tilemap cỏ sinh theo seed; mọi vật đặt theo ô với footprint
    /// nguyên, vùng chặn trong game là collider chân lấy từ prefab Cainos. Chuột trái đặt/chọn-kéo, chuột phải xóa,
    /// Shift lật ngang, P cọ đường đá. Lăn chuột phóng to/thu nhỏ, Alt + kéo chuột trái (hoặc chuột giữa) di chuyển camera.
    /// File: cùng định dạng map_demo.json nên game/server đọc thẳng được. Ba file partial: điều khiển (file này),
    /// hiển thị (.View), giao diện + file (.Gui).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class LevelEditorController : MonoBehaviour
    {
        private enum Tool { Select, Place, Path }

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private Camera viewCamera;

        private LevelDocument _doc = new LevelDocument();
        private readonly LevelHistory _history = new LevelHistory();
        private Tool _tool = Tool.Select;
        private LevelItemType _brush;
        private bool _brushFlip;
        private int _selected = -1;
        private int _hover = -1;
        private bool _dragging, _dragRecorded;
        private Vector2Int _dragOffset;
        private bool _painting, _paintRecorded, _paintErase;
        private bool _panning;
        private int _panButton = -1;
        private Vector3 _panOrigin;
        private bool _dirty;
        private Vector2 _mouseWorld;
        private Vector2Int _mouseCell;

        public LevelDocument Document => _doc;

        private void Start()
        {
            if (catalog == null)
            {
                Debug.LogError("[LevelEditor] Thiếu LevelCatalog; chạy menu Jungle Maze/Level Editor/Build Level Editor Scene.");
                enabled = false;
                return;
            }
            if (viewCamera == null) viewCamera = Camera.main;
            InitializeView();
            InitializeGui();
            RebuildAll();
            FrameMap();
            SetStatus("Chọn vật thể ở cột trái rồi click vào ô để đặt. P: cọ đường đá. Xem phím tắt ở cột phải.");
        }

        private void Update()
        {
            if (viewCamera == null) return;
            _mouseWorld = viewCamera.ScreenToWorldPoint(Input.mousePosition);
            _mouseCell = new Vector2Int(Mathf.FloorToInt(_mouseWorld.x), Mathf.FloorToInt(_mouseWorld.y));
            HandleCamera();
            if (!IsTyping) HandleShortcuts();
            HandleMouse();
            UpdateGhost();
            UpdateGridVisibility();
        }

        // ---------- Chuột ----------

        private void HandleMouse()
        {
            var overUi = IsPointerOverUi();
            if (_dragging) { ContinueDrag(); return; }
            if (_painting) { ContinuePaint(); return; }
            var hover = overUi || _tool == Tool.Path ? -1 : _doc.FindTopAt(_mouseWorld.x, _mouseWorld.y);
            if (hover != _hover) { _hover = hover; RefreshHighlights(); }
            if (overUi) return;

            var wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f) Zoom(wheel);
            if (Alt || _panning) return; // Alt + chuột trái là kéo camera, không đặt/chọn

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) GUIUtility.keyboardControl = 0;
            if (_tool == Tool.Path)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    _painting = true;
                    _paintRecorded = false;
                    _paintErase = Input.GetMouseButtonDown(1);
                    ContinuePaint();
                }
                return;
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (_hover >= 0) RemoveAt(_hover);
                return;
            }
            if (!Input.GetMouseButtonDown(0)) return;
            if (_tool == Tool.Place && _brush != null) { PlaceBrush(); return; }

            Select(_hover);
            if (_selected < 0) return;
            _dragOffset = CellOf(_doc.Placements[_selected]) - _mouseCell;
            _dragStartCell = _mouseCell;
            _dragging = true;
            _dragRecorded = false;
        }

        /// <summary>Kéo theo ô. Vật lệch ô (map cũ) chỉ được đưa vào ô khi con trỏ thực sự rời ô ban đầu.</summary>
        private void ContinueDrag()
        {
            if (!Input.GetMouseButton(0) || _selected < 0) { _dragging = false; return; }
            if (_mouseCell == _dragStartCell) return;
            var p = _doc.Placements[_selected];
            var target = _mouseCell + _dragOffset;
            if (p.OnGrid && p.CellX == target.x && p.CellY == target.y) return;
            var moved = p.Clone();
            moved.MoveToCell(target.x, target.y);
            if (!CanPlace(moved, _selected)) return;
            if (!_dragRecorded) { RecordHistory(); _dragRecorded = true; }
            _doc.Placements[_selected] = moved;
            MarkDirty();
            RefreshView(_selected);
            RebuildSprinkles();
        }

        private Vector2Int _dragStartCell;

        private static Vector2Int CellOf(LevelPlacement p) =>
            p.OnGrid ? new Vector2Int(p.CellX, p.CellY) : new Vector2Int(Mathf.RoundToInt(p.Bounds.MinX), Mathf.RoundToInt(p.Bounds.MinY));

        private void ContinuePaint()
        {
            var held = _paintErase ? Input.GetMouseButton(1) : Input.GetMouseButton(0);
            if (!held) { _painting = false; return; }
            var cell = _mouseCell;
            if (cell.x < 0 || cell.y < 0 || cell.x >= _doc.Width || cell.y >= _doc.Height) return;
            if (_doc.Path.Contains(cell) != _paintErase) return;
            if (!_paintRecorded) { RecordHistory(); _paintRecorded = true; } // một lượt kéo = một bước undo
            if (_paintErase) _doc.Path.Remove(cell);
            else _doc.Path.Add(cell);
            MarkDirty();
            RefreshGroundAround(cell);
            RebuildSprinkles();
        }

        /// <summary>Ô dưới-trái của footprint khi con trỏ ở <paramref name="cell"/>: con trỏ nằm giữa cạnh đáy.</summary>
        private Vector2Int BrushOrigin(Vector2Int cell) => new Vector2Int(cell.x - (_brush.Footprint.x - 1) / 2, cell.y);

        private bool CanPlace(LevelPlacement p, int ignore) =>
            _doc.IsFree(Mathf.RoundToInt(p.Bounds.MinX), Mathf.RoundToInt(p.Bounds.MinY), Mathf.RoundToInt(p.Width), Mathf.RoundToInt(p.Height),
                p.Group == LevelItemGroup.Decoration, ignore);

        private void PlaceBrush()
        {
            var placement = CreateFromBrush(BrushOrigin(_mouseCell));
            if (!CanPlace(placement, -1))
            {
                SetStatus("Ô này đã có vật hoặc nằm ngoài map.");
                return;
            }
            RecordHistory();
            _doc.Placements.Add(placement);
            MarkDirty();
            SyncViews();
            SetStatus($"Đã đặt {_brush.DisplayName} tại ô ({placement.CellX}, {placement.CellY})" +
                      (placement.IsPortal ? $" — cặp portal #{placement.PairId}" : ""));
        }

        private LevelPlacement CreateFromBrush(Vector2Int origin)
        {
            var placement = LevelPlacement.OnCell(_brush.Id, _brush.Group, origin.x, origin.y, _brush.Shape);
            placement.FlipX = _brush.Group != LevelItemGroup.Spawn && _brushFlip;
            if (placement.IsPortal) placement.PairId = _doc.NextPortalPair();
            return placement;
        }

        // ---------- Phím ----------

        private static bool Ctrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                                    Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        private static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);

        private void HandleShortcuts()
        {
            var ctrl = Ctrl;
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (ctrl)
            {
                if (Input.GetKeyDown(KeyCode.Z)) { if (shift) Redo(); else Undo(); }
                else if (Input.GetKeyDown(KeyCode.Y)) Redo();
                else if (Input.GetKeyDown(KeyCode.S)) Save(false);
                else if (Input.GetKeyDown(KeyCode.D)) DuplicateSelected();
                return;
            }
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) FlipActive();
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) { if (_selected >= 0) RemoveAt(_selected); }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q)) SetTool(Tool.Select, null);
            if (Input.GetKeyDown(KeyCode.P)) SetTool(_tool == Tool.Path ? Tool.Select : Tool.Path, null);
            if (Input.GetKeyDown(KeyCode.H)) { _showOutlines = !_showOutlines; RefreshHighlights(); }
            if (Input.GetKeyDown(KeyCode.L)) _showGrid = !_showGrid;
            if (Input.GetKeyDown(KeyCode.F)) FrameMap();
            if (_tool == Tool.Select && _selected >= 0)
            {
                var nudge = Vector2Int.zero;
                if (Input.GetKeyDown(KeyCode.LeftArrow)) nudge.x -= 1;
                if (Input.GetKeyDown(KeyCode.RightArrow)) nudge.x += 1;
                if (Input.GetKeyDown(KeyCode.DownArrow)) nudge.y -= 1;
                if (Input.GetKeyDown(KeyCode.UpArrow)) nudge.y += 1;
                if (nudge != Vector2Int.zero) Nudge(nudge);
            }
        }

        // ---------- Thao tác ----------

        private void SetTool(Tool tool, LevelItemType brush)
        {
            _tool = tool;
            _brush = tool == Tool.Place ? brush : null;
            if (_brush != null || tool == Tool.Path) Select(-1);
            if (tool == Tool.Path) SetStatus("Đường đá: giữ chuột trái để tô, chuột phải để xoá. P hoặc Q để thoát.");
            UpdateGhost();
        }

        private void Select(int index)
        {
            _selected = index >= 0 && index < _doc.Placements.Count ? index : -1;
            RefreshHighlights();
        }

        private void RemoveAt(int index)
        {
            if (index < 0 || index >= _doc.Placements.Count) return;
            var removed = _doc.Placements[index];
            RecordHistory();
            _doc.Placements.RemoveAt(index);
            if (_selected == index) _selected = -1;
            else if (_selected > index) _selected--;
            _hover = -1;
            _dragging = false;
            MarkDirty();
            SyncViews();
            SetStatus($"Đã xóa {DisplayName(removed.Type)}" + (removed.IsPortal ? $" (cặp #{removed.PairId} còn lẻ)" : ""));
        }

        private void FlipActive()
        {
            if (_tool == Tool.Place && _brush != null)
            {
                _brushFlip = !_brushFlip;
                SetStatus(_brushFlip ? "Bút: lật ngang" : "Bút: hướng gốc");
                return;
            }
            if (_selected < 0) return;
            RecordHistory();
            var p = _doc.Placements[_selected];
            p.FlipX = !p.FlipX;
            MarkDirty();
            RefreshView(_selected);
        }

        private void Nudge(Vector2Int delta)
        {
            var p = _doc.Placements[_selected];
            var target = CellOf(p) + delta;
            var moved = p.Clone();
            moved.MoveToCell(target.x, target.y);
            if (!CanPlace(moved, _selected)) { SetStatus("Không dịch được: ô bận hoặc ra ngoài map."); return; }
            RecordHistory();
            _doc.Placements[_selected] = moved;
            MarkDirty();
            RefreshView(_selected);
            RebuildSprinkles();
        }

        private void DuplicateSelected()
        {
            if (_selected < 0) return;
            var p = _doc.Placements[_selected];
            var type = catalog.Find(p.Type);
            if (type == null) return;
            _brushFlip = p.FlipX;
            SetTool(Tool.Place, type);
            SetStatus($"Nhân bản {type.DisplayName}: click để đặt bản sao.");
        }

        private void SetPortalPair(int index, int delta)
        {
            var p = _doc.Placements[index];
            var next = Mathf.Clamp(p.PairId + delta, 1, 99);
            if (next == p.PairId) return;
            RecordHistory();
            p.PairId = (ushort)next;
            MarkDirty();
        }

        private void SetSeed(int seed)
        {
            if (seed == _doc.Seed) return;
            RecordHistory();
            _doc.Seed = seed;
            MarkDirty();
            GenerateGround();
            SyncViews(); // mẫu hình bụi/bình chọn theo seed
            SetStatus($"Seed {seed}: cỏ, hoa, viền đường và tường mép sinh lại.");
        }

        private void SetDecor(float density)
        {
            density = Mathf.Clamp01(density);
            if (Mathf.Abs(density - _doc.Decor) < .005f) return;
            if (!_decorRecorded) { RecordHistory(); _decorRecorded = true; }
            _doc.Decor = density;
            MarkDirty();
            RebuildSprinkles();
        }

        private bool _decorRecorded;

        // ---------- Undo / tài liệu ----------

        private void RecordHistory() => _history.Record(_doc.ToJson());

        private void Undo()
        {
            if (!_history.TryUndo(_doc.ToJson(), out var previous)) { SetStatus("Không còn gì để hoàn tác."); return; }
            Restore(previous);
            SetStatus("Hoàn tác.");
        }

        private void Redo()
        {
            if (!_history.TryRedo(_doc.ToJson(), out var next)) { SetStatus("Không còn gì để làm lại."); return; }
            Restore(next);
            SetStatus("Làm lại.");
        }

        private LevelDocument ParseDocument(string json) => LevelDocument.FromJson(json, catalog.BaseSizeOf, catalog.ShapeOf);

        private void Restore(string json)
        {
            _doc = ParseDocument(json);
            _selected = -1;
            _hover = -1;
            _dragging = false;
            _painting = false;
            MarkDirty();
            SyncMapFields();
            GenerateGround();
            SyncViews();
        }

        private void ReplaceDocument(LevelDocument document)
        {
            _doc = document;
            _history.Clear();
            _selected = -1;
            _hover = -1;
            _dragging = false;
            _painting = false;
            _dirty = false;
            _validation = null;
            SyncMapFields();
            RebuildAll();
            FrameMap();
        }

        private void MarkDirty()
        {
            _dirty = true;
            _validation = null; // kết quả kiểm tra cũ không còn đúng
        }

        private string DisplayName(string id)
        {
            var type = catalog.Find(id);
            return type != null ? type.DisplayName : id;
        }

        // ---------- Camera ----------

        private void HandleCamera()
        {
            // Kéo camera: Alt + chuột trái (hoặc giữ chuột giữa).
            if (!_panning && !_dragging && !_painting && !IsPointerOverUi())
            {
                if (Alt && Input.GetMouseButtonDown(0)) _panButton = 0;
                else if (Input.GetMouseButtonDown(2)) _panButton = 2;
                else _panButton = -1;
                if (_panButton >= 0)
                {
                    _panning = true;
                    _panOrigin = viewCamera.ScreenToWorldPoint(Input.mousePosition);
                }
            }
            if (_panning && !Input.GetMouseButton(_panButton)) _panning = false;
            if (_panning)
            {
                var now = viewCamera.ScreenToWorldPoint(Input.mousePosition);
                viewCamera.transform.position += _panOrigin - now;
            }
            if (IsTyping || Ctrl) return;
            var move = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) move.y += 1;
            if (Input.GetKey(KeyCode.S)) move.y -= 1;
            if (Input.GetKey(KeyCode.A)) move.x -= 1;
            if (Input.GetKey(KeyCode.D)) move.x += 1;
            if (_tool != Tool.Select || _selected < 0)
            {
                if (Input.GetKey(KeyCode.UpArrow)) move.y += 1;
                if (Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
                if (Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
                if (Input.GetKey(KeyCode.RightArrow)) move.x += 1;
            }
            if (move != Vector2.zero)
                viewCamera.transform.position += (Vector3)(move.normalized * viewCamera.orthographicSize * 1.2f * Time.unscaledDeltaTime);
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) Zoom(1);
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) Zoom(-1);
        }

        private void Zoom(float direction)
        {
            var before = (Vector2)viewCamera.ScreenToWorldPoint(Input.mousePosition);
            viewCamera.orthographicSize = Mathf.Clamp(viewCamera.orthographicSize * (direction > 0 ? 1f / 1.15f : 1.15f), 2f, 200f);
            var after = (Vector2)viewCamera.ScreenToWorldPoint(Input.mousePosition);
            viewCamera.transform.position += (Vector3)(before - after); // zoom quanh con trỏ
        }

        private void FrameMap()
        {
            viewCamera.orthographic = true;
            viewCamera.transform.position = new Vector3(_doc.Width / 2f, _doc.Height / 2f - 1f, -10f);
            var aspect = Mathf.Max(.1f, viewCamera.aspect);
            viewCamera.orthographicSize = Mathf.Max(_doc.Height / 2f + 1f, _doc.Width / 2f / aspect) * 1.15f + 1f;
        }
    }
}
