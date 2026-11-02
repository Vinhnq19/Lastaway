using UnityEngine;

namespace JungleMaze.LevelEditor
{
    /// <summary>
    /// Scene LevelEditor (chạy bằng Play). Ground là Tilemap sinh từ W×H; vật thể đặt tự do theo world unit.
    /// Chuột trái đặt/chọn-kéo, chuột phải xóa, lăn chuột đổi kích thước, Shift lật ngang (scale x = -1), R xoay 90°.
    /// Alt + kéo chuột trái di chuyển camera, Alt + lăn chuột phóng to/thu nhỏ.
    /// File: cùng định dạng map_demo.json nên game/server đọc thẳng được. Ba file partial: điều khiển (file này),
    /// hiển thị (.View), giao diện + file (.Gui).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class LevelEditorController : MonoBehaviour
    {
        private enum Tool { Select, Place }

        private const float WheelScaleStep = 1.1f;
        private const float SnapStep = .5f;
        private const float NudgeStep = .1f;

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private Camera viewCamera;

        private LevelDocument _doc = new LevelDocument();
        private readonly LevelHistory _history = new LevelHistory();
        private Tool _tool = Tool.Select;
        private LevelItemType _brush;
        private float _brushScale = 1f;
        private int _brushRotation;
        private bool _brushFlip;
        private int _selected = -1;
        private int _hover = -1;
        private bool _dragging, _dragRecorded;
        private Vector2 _dragOffset;
        private bool _panning;
        private int _panButton = -1;
        private Vector3 _panOrigin;
        private float _lastWheelRecord;
        private bool _snap;
        private bool _dirty;
        private Vector2 _mouseWorld;

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
            SetStatus("Chọn vật thể ở cột trái rồi click chuột trái để đặt. Xem phím tắt ở cột phải.");
        }

        private void Update()
        {
            if (viewCamera == null) return;
            _mouseWorld = viewCamera.ScreenToWorldPoint(Input.mousePosition);
            HandleCamera();
            if (!IsTyping) HandleShortcuts();
            HandleMouse();
            UpdateGhost();
        }

        // ---------- Chuột ----------

        private void HandleMouse()
        {
            var overUi = IsPointerOverUi();
            if (_dragging) { ContinueDrag(); return; }
            var hover = overUi ? -1 : _doc.FindTopAt(_mouseWorld.x, _mouseWorld.y);
            if (hover != _hover) { _hover = hover; RefreshHighlights(); }
            if (overUi) return;

            var wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f)
            {
                if (Alt) Zoom(wheel);
                else ScaleActive(wheel > 0 ? WheelScaleStep : 1f / WheelScaleStep);
            }
            if (Alt || _panning) return; // Alt + chuột trái là kéo camera, không đặt/chọn

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) GUIUtility.keyboardControl = 0;
            if (Input.GetMouseButtonDown(1))
            {
                if (_hover >= 0) RemoveAt(_hover);
                return;
            }
            if (!Input.GetMouseButtonDown(0)) return;
            if (_tool == Tool.Place && _brush != null) { PlaceBrush(); return; }

            Select(_hover);
            if (_selected < 0) return;
            var p = _doc.Placements[_selected];
            _dragOffset = new Vector2(p.X, p.Y) - _mouseWorld;
            _dragging = true;
            _dragRecorded = false;
        }

        private void ContinueDrag()
        {
            if (!Input.GetMouseButton(0) || _selected < 0) { _dragging = false; return; }
            var target = ClampToMap(SnapPoint(_mouseWorld + _dragOffset));
            var p = _doc.Placements[_selected];
            if (Mathf.Approximately(p.X, target.x) && Mathf.Approximately(p.Y, target.y)) return;
            if (!_dragRecorded) { RecordHistory(); _dragRecorded = true; }
            p.X = target.x;
            p.Y = target.y;
            MarkDirty();
            RefreshView(_selected);
        }

        private void PlaceBrush()
        {
            var position = ClampToMap(SnapPoint(_mouseWorld));
            var placement = CreateFromBrush(position);
            RecordHistory();
            _doc.Placements.Add(placement);
            MarkDirty();
            SyncViews();
            SetStatus($"Đã đặt {_brush.DisplayName} tại ({position.x:0.##}, {position.y:0.##})" +
                      (placement.IsPortal ? $" — cặp portal #{placement.PairId}" : ""));
        }

        private LevelPlacement CreateFromBrush(Vector2 position)
        {
            var spawn = _brush.Group == LevelItemGroup.Spawn;
            var placement = new LevelPlacement
            {
                Type = _brush.Id,
                Group = _brush.Group,
                X = position.x,
                Y = position.y,
                BaseWidth = spawn ? LevelDocument.SpawnSize : _brush.BaseSize.x,
                BaseHeight = spawn ? LevelDocument.SpawnSize : _brush.BaseSize.y,
                Scale = spawn ? 1f : _brushScale,
                Rotation = spawn ? 0 : _brushRotation,
                FlipX = !spawn && _brushFlip,
            };
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
            if (Input.GetKeyDown(KeyCode.R)) RotateActive();
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) { if (_selected >= 0) RemoveAt(_selected); }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q)) SetTool(Tool.Select, null);
            if (Input.GetKeyDown(KeyCode.G)) { _snap = !_snap; SetStatus(_snap ? "Bật snap 0.5" : "Tắt snap"); }
            if (Input.GetKeyDown(KeyCode.H)) { _showOutlines = !_showOutlines; RefreshHighlights(); }
            if (Input.GetKeyDown(KeyCode.F)) FrameMap();
            if (_tool == Tool.Select && _selected >= 0)
            {
                var nudge = Vector2.zero;
                if (Input.GetKeyDown(KeyCode.LeftArrow)) nudge.x -= NudgeStep;
                if (Input.GetKeyDown(KeyCode.RightArrow)) nudge.x += NudgeStep;
                if (Input.GetKeyDown(KeyCode.DownArrow)) nudge.y -= NudgeStep;
                if (Input.GetKeyDown(KeyCode.UpArrow)) nudge.y += NudgeStep;
                if (nudge != Vector2.zero) Nudge(nudge);
            }
        }

        // ---------- Thao tác ----------

        private void SetTool(Tool tool, LevelItemType brush)
        {
            _tool = tool;
            _brush = tool == Tool.Place ? brush : null;
            if (_brush != null) Select(-1);
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

        private void ScaleActive(float factor)
        {
            if (_tool == Tool.Place && _brush != null)
            {
                if (_brush.Group == LevelItemGroup.Spawn) return;
                _brushScale = LevelDocument.ClampScale(_brushScale * factor);
                SetStatus($"Kích thước bút: ×{_brushScale:0.##}");
                return;
            }
            if (_selected < 0) return;
            var p = _doc.Placements[_selected];
            if (p.Group == LevelItemGroup.Spawn) return;
            // Một lượt lăn liên tục chỉ tạo một bước undo.
            if (Time.unscaledTime - _lastWheelRecord > .5f) RecordHistory();
            _lastWheelRecord = Time.unscaledTime;
            p.Scale = LevelDocument.ClampScale(p.Scale * factor);
            MarkDirty();
            RefreshView(_selected);
            SetStatus($"{DisplayName(p.Type)}: ×{p.Scale:0.##} ({p.Width:0.##}×{p.Height:0.##})");
        }

        private void FlipActive()
        {
            if (_tool == Tool.Place && _brush != null)
            {
                _brushFlip = !_brushFlip;
                SetStatus(_brushFlip ? "Bút: lật ngang (sx = -1)" : "Bút: hướng gốc");
                return;
            }
            if (_selected < 0) return;
            RecordHistory();
            var p = _doc.Placements[_selected];
            p.FlipX = !p.FlipX;
            MarkDirty();
            RefreshView(_selected);
        }

        private void RotateActive()
        {
            if (_tool == Tool.Place && _brush != null)
            {
                _brushRotation = (_brushRotation + 1) & 3;
                SetStatus($"Bút: xoay {_brushRotation * 90}°");
                return;
            }
            if (_selected < 0 || _doc.Placements[_selected].Group == LevelItemGroup.Spawn) return;
            RecordHistory();
            var p = _doc.Placements[_selected];
            p.Rotation = (p.Rotation + 1) & 3;
            MarkDirty();
            RefreshView(_selected);
        }

        private void Nudge(Vector2 delta)
        {
            RecordHistory();
            var p = _doc.Placements[_selected];
            var target = ClampToMap(new Vector2(p.X, p.Y) + delta);
            p.X = target.x;
            p.Y = target.y;
            MarkDirty();
            RefreshView(_selected);
        }

        private void DuplicateSelected()
        {
            if (_selected < 0) return;
            var p = _doc.Placements[_selected];
            var type = catalog.Find(p.Type);
            if (type == null) return;
            _brushScale = p.Scale;
            _brushRotation = p.Rotation;
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

        private void Restore(string json)
        {
            var restored = LevelDocument.FromJson(json, catalog.BaseSizeOf);
            var sizeChanged = restored.Width != _doc.Width || restored.Height != _doc.Height || restored.GroundTile != _doc.GroundTile;
            _doc = restored;
            _selected = -1;
            _hover = -1;
            _dragging = false;
            MarkDirty();
            SyncMapFields();
            if (sizeChanged) GenerateGround();
            SyncViews();
        }

        private void ReplaceDocument(LevelDocument document)
        {
            _doc = document;
            _history.Clear();
            _selected = -1;
            _hover = -1;
            _dragging = false;
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

        private Vector2 SnapPoint(Vector2 p) =>
            _snap ? new Vector2(Mathf.Round(p.x / SnapStep) * SnapStep, Mathf.Round(p.y / SnapStep) * SnapStep) : p;

        private Vector2 ClampToMap(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, 0f, _doc.Width), Mathf.Clamp(p.y, 0f, _doc.Height));

        private string DisplayName(string id)
        {
            var type = catalog.Find(id);
            return type != null ? type.DisplayName : id;
        }

        // ---------- Camera ----------

        private void HandleCamera()
        {
            // Kéo camera: Alt + chuột trái (hoặc giữ chuột giữa).
            if (!_panning && !_dragging && !IsPointerOverUi())
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
            viewCamera.transform.position = new Vector3(_doc.Width / 2f, _doc.Height / 2f, -10f);
            var aspect = Mathf.Max(.1f, viewCamera.aspect);
            viewCamera.orthographicSize = Mathf.Max(_doc.Height / 2f, _doc.Width / 2f / aspect) * 1.15f + 1f;
        }
    }
}
