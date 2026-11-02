using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JungleMaze.LevelEditor
{
    public sealed partial class LevelEditorController
    {
        private const float TopHeight = 34f, BottomHeight = 26f, LeftWidth = 230f, RightWidth = 270f;

        private static readonly (LevelItemGroup group, string title)[] PaletteGroups =
        {
            (LevelItemGroup.Cover, "CHE CHẮN (Hard cover)"),
            (LevelItemGroup.WorldObject, "VẬT THỂ GAME"),
            (LevelItemGroup.Decoration, "TRANG TRÍ (game chưa hỗ trợ)"),
            (LevelItemGroup.Spawn, "SPAWN"),
        };

        private const string Help =
            "Chuột trái: đặt (chế độ Đặt) / chọn + kéo (chế độ Chọn)\n" +
            "Chuột phải: xóa vật thể dưới con trỏ\n" +
            "Lăn chuột: to/nhỏ bút hoặc vật đang chọn\n" +
            "Shift: lật ngang (sx = -1)   R: xoay 90°\n" +
            "Alt + kéo chuột trái: di chuyển camera (hoặc giữ chuột giữa, WASD)\n" +
            "Alt + lăn chuột, +/-: phóng to / thu nhỏ\n" +
            "Q/Esc: về chế độ Chọn   Del: xóa vật đang chọn\n" +
            "Mũi tên: dịch vật đang chọn 0.1   G: snap 0.5\n" +
            "Ctrl+Z / Ctrl+Y: hoàn tác / làm lại   Ctrl+S: lưu\n" +
            "Ctrl+D: nhân bản vật đang chọn   F: xem toàn map   H: viền";

        private Rect _topRect, _leftRect, _rightRect, _bottomRect;
        private Vector2 _leftScroll, _rightScroll;
        private string _widthText, _heightText, _versionText;
        private string _path = "";
        private string _status = "";
        private string _validation;
        private string _validationHash;
        private string _pendingConfirm;
        private float _pendingConfirmUntil;
        private string[] _files = Array.Empty<string>();
        private Action _guiAction;
        private string _tooltip = "";
        private GUIStyle _wrap, _small, _header, _itemButton, _selectedButton;

        private bool IsTyping => GUIUtility.keyboardControl != 0;

        private static string LevelsDirectory =>
            Application.isEditor ? Path.Combine(Application.dataPath, "_JungleMaze/Levels") : Path.Combine(Application.persistentDataPath, "Levels");

        /// <summary>Map đang dùng trong game (client + GameServer đọc). Chỉ tồn tại khi chạy từ project.</summary>
        private static string GameMapPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Backend/Shared/GameData/map_demo.json"));

        private void InitializeGui()
        {
            Directory.CreateDirectory(LevelsDirectory);
            SyncMapFields();
            RefreshFiles();
        }

        private void SyncMapFields()
        {
            _widthText = _doc.Width.ToString();
            _heightText = _doc.Height.ToString();
            _versionText = _doc.Version;
        }

        private void SetStatus(string message) => _status = message;

        private bool IsPointerOverUi()
        {
            var mouse = Input.mousePosition;
            var gui = new Vector2(mouse.x, Screen.height - mouse.y);
            return _topRect.Contains(gui) || _leftRect.Contains(gui) || _rightRect.Contains(gui) || _bottomRect.Contains(gui) ||
                   GUIUtility.hotControl != 0;
        }

        private void OnGUI()
        {
            if (catalog == null) return;
            EnsureStyles();
            _topRect = new Rect(0, 0, Screen.width, TopHeight);
            _bottomRect = new Rect(0, Screen.height - BottomHeight, Screen.width, BottomHeight);
            _leftRect = new Rect(0, TopHeight, LeftWidth, Screen.height - TopHeight - BottomHeight);
            _rightRect = new Rect(Screen.width - RightWidth, TopHeight, RightWidth, Screen.height - TopHeight - BottomHeight);

            DrawLabels();
            DrawTopBar();
            DrawPalette();
            DrawInspector();
            DrawStatusBar();

            // IMGUI vẽ mỗi event hai lượt (Layout rồi Repaint/Input); đổi trạng thái giữa chừng làm lệch số control.
            // Vì vậy nút chỉ ghi lại việc cần làm, thực hiện sau khi vẽ xong.
            if (Event.current.type == EventType.Repaint) _tooltip = GUI.tooltip;
            var action = _guiAction;
            _guiAction = null;
            action?.Invoke();
        }

        private void Defer(Action action) => _guiAction += action;

        private void EnsureStyles()
        {
            if (_wrap != null) return;
            _wrap = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, richText = true };
            _header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, .85f, .4f) } };
            _itemButton = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, richText = true };
            _selectedButton = new GUIStyle(_itemButton) { fontStyle = FontStyle.Bold, normal = { textColor = Color.yellow } };
        }

        // ---------- Thanh trên ----------

        private void DrawTopBar()
        {
            GUI.Box(_topRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(6, 5, Screen.width - 12, TopHeight - 8));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mới", GUILayout.Width(50))) Defer(() => Confirm("new", "Bỏ thay đổi chưa lưu và tạo map mới?", NewMap));
            if (GUILayout.Button("Mở…", GUILayout.Width(50))) Defer(() => Confirm("open", "Bỏ thay đổi chưa lưu và mở file khác?", OpenWithDialog));
            if (GUILayout.Button("Mở map game", GUILayout.Width(100))) Defer(() => Confirm("game", "Bỏ thay đổi chưa lưu và mở map_demo.json?", () => Load(GameMapPath)));
            if (GUILayout.Button("Lưu", GUILayout.Width(50))) Defer(() => Save(false));
            if (GUILayout.Button("Lưu thành…", GUILayout.Width(85))) Defer(() => Save(true));
            GUILayout.Space(10);
            GUI.enabled = _history.CanUndo;
            if (GUILayout.Button("↶ Hoàn tác", GUILayout.Width(85))) Defer(Undo);
            GUI.enabled = _history.CanRedo;
            if (GUILayout.Button("↷ Làm lại", GUILayout.Width(80))) Defer(Redo);
            GUI.enabled = true;
            GUILayout.Space(10);
            if (GUILayout.Button("Kiểm tra", GUILayout.Width(75))) Defer(RunValidation);
            _snap = GUILayout.Toggle(_snap, " Snap 0.5", GUILayout.Width(80));
            var outlines = GUILayout.Toggle(_showOutlines, " Viền", GUILayout.Width(55));
            if (outlines != _showOutlines) Defer(() => { _showOutlines = outlines; RefreshHighlights(); });
            if (GUILayout.Button("Xem toàn map", GUILayout.Width(100))) Defer(FrameMap);
            GUILayout.FlexibleSpace();
            var name = string.IsNullOrEmpty(_path) ? "(chưa lưu)" : Path.GetFileName(_path);
            GUILayout.Label((_dirty ? "● " : "") + name, _header);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ---------- Bảng chọn vật thể ----------

        private void DrawPalette()
        {
            GUI.Box(_leftRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(_leftRect.x + 6, _leftRect.y + 6, _leftRect.width - 12, _leftRect.height - 12));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tool == Tool.Select, "Chọn (Q)", GUI.skin.button)) { if (_tool != Tool.Select) Defer(() => SetTool(Tool.Select, null)); }
            GUILayout.Label(_tool == Tool.Place ? "Đang: Đặt" : "Đang: Chọn", _small);
            GUILayout.EndHorizontal();
            _leftScroll = GUILayout.BeginScrollView(_leftScroll);
            foreach (var (group, title) in PaletteGroups)
            {
                GUILayout.Space(6);
                GUILayout.Label(title, _header);
                foreach (var item in catalog.Items)
                {
                    if (item == null || item.Group != group) continue;
                    var active = _tool == Tool.Place && _brush == item;
                    var count = _doc.Count(item.Group, item.Group == LevelItemGroup.Spawn ? null : item.Id);
                    var label = $"{item.DisplayName}  <size=10>{item.BaseSize.x:0.#}×{item.BaseSize.y:0.#} · {count}</size>";
                    GUILayout.BeginHorizontal();
                    var iconRect = GUILayoutUtility.GetRect(26, 26, GUILayout.Width(26), GUILayout.Height(26));
                    DrawIcon(iconRect, item);
                    if (GUILayout.Button(new GUIContent(label, item.Note), active ? _selectedButton : _itemButton, GUILayout.Height(26)))
                    {
                        var picked = item;
                        Defer(() =>
                        {
                            if (active) { SetTool(Tool.Select, null); return; }
                            SetTool(Tool.Place, picked);
                            SetStatus($"Đặt {picked.DisplayName}: chuột trái đặt, lăn đổi kích thước, Shift lật, R xoay.");
                        });
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            // Luôn đúng hai nhãn để số control không đổi giữa các lượt vẽ.
            GUILayout.Label(_brush != null ? $"Bút: ×{_brushScale:0.##} · {_brushRotation * 90}°" + (_brushFlip ? " · lật" : "") : "", _header);
            GUILayout.Label(_brush != null ? _brush.Note ?? "" : _tooltip, _small);
            GUILayout.EndArea();
        }

        private void DrawIcon(Rect rect, LevelItemType item)
        {
            if (item.Sprite == null)
            {
                var old = GUI.color;
                GUI.color = item.Tint;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = old;
                return;
            }
            var texture = item.Sprite.texture;
            var r = item.Sprite.textureRect;
            var uv = new Rect(r.x / texture.width, r.y / texture.height, r.width / texture.width, r.height / texture.height);
            var aspect = r.width / Mathf.Max(1f, r.height);
            var fit = aspect >= 1 ? new Rect(rect.x, rect.y + (rect.height - rect.height / aspect) / 2, rect.width, rect.height / aspect)
                                  : new Rect(rect.x + (rect.width - rect.width * aspect) / 2, rect.y, rect.width * aspect, rect.height);
            GUI.DrawTextureWithTexCoords(fit, texture, uv);
        }

        // ---------- Cột phải ----------

        private void DrawInspector()
        {
            GUI.Box(_rightRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(_rightRect.x + 6, _rightRect.y + 6, _rightRect.width - 12, _rightRect.height - 12));
            _rightScroll = GUILayout.BeginScrollView(_rightScroll);

            GUILayout.Label("MAP", _header);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rộng", GUILayout.Width(36)); _widthText = GUILayout.TextField(_widthText, 4, GUILayout.Width(44));
            GUILayout.Label("Cao", GUILayout.Width(28)); _heightText = GUILayout.TextField(_heightText, 4, GUILayout.Width(44));
            if (GUILayout.Button("Tạo ground")) Defer(ApplyMapSize);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Phiên bản", GUILayout.Width(64));
            var version = GUILayout.TextField(_versionText, 32);
            if (version != _versionText) { _versionText = version; Defer(() => { RecordHistory(); _doc.Version = version; MarkDirty(); }); }
            GUILayout.EndHorizontal();
            if (catalog.GroundTiles.Length > 1)
            {
                GUILayout.Label("Ground tile", _small);
                GUILayout.BeginHorizontal();
                for (var i = 0; i < catalog.GroundTiles.Length; i++)
                {
                    var tile = catalog.GroundTiles[i];
                    if (GUILayout.Toggle(_doc.GroundTile == i, tile != null ? tile.name : "?", GUI.skin.button) && _doc.GroundTile != i)
                    {
                        var index = i;
                        Defer(() => { RecordHistory(); _doc.GroundTile = index; MarkDirty(); GenerateGround(); });
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            DrawSelection();

            GUILayout.Space(8);
            GUILayout.Label("KIỂM TRA THEO LUẬT GAME", _header);
            GUILayout.Label($"Spawn {_doc.Count(LevelItemGroup.Spawn)}/10 · Cover {_doc.Count(LevelItemGroup.Cover)} · " +
                            $"Vật thể {_doc.Count(LevelItemGroup.WorldObject)} · Trang trí {_doc.Count(LevelItemGroup.Decoration)}", _small);
            if (_validation == null) GUILayout.Label("Chưa kiểm tra (bấm \"Kiểm tra\").", _small);
            else if (_validation.Length == 0) GUILayout.Label($"<color=#7CFC7C>Hợp lệ — game đọc được.</color>\nhash {_validationHash?.Substring(0, 12)}…", _wrap);
            else GUILayout.Label($"<color=#FF7B6B>{_validation}</color>", _wrap);

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label("FILE TRONG Levels/", _header);
            if (GUILayout.Button("↻", GUILayout.Width(24))) Defer(RefreshFiles);
            GUILayout.EndHorizontal();
            if (_files.Length == 0) GUILayout.Label("(trống)", _small);
            foreach (var file in _files)
                if (GUILayout.Button(Path.GetFileName(file), file == _path ? _selectedButton : GUI.skin.button))
                {
                    var target = file;
                    Defer(() => Confirm("file:" + target, "Bỏ thay đổi chưa lưu và mở file này?", () => Load(target)));
                }

            GUILayout.Space(8);
            GUILayout.Label("PHÍM TẮT", _header);
            GUILayout.Label(Help, _small);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawSelection()
        {
            GUILayout.Label("ĐANG CHỌN", _header);
            if (_selected < 0 || _selected >= _doc.Placements.Count)
            {
                GUILayout.Label("Không có. Ở chế độ Chọn, click vào vật thể để chọn.", _small);
                return;
            }
            var p = _doc.Placements[_selected];
            var type = catalog.Find(p.Type);
            GUILayout.Label($"{(type != null ? type.DisplayName : p.Type)}  <size=10>({p.Type})</size>", _wrap);
            GUILayout.Label($"Tâm ({p.X:0.###}, {p.Y:0.###}) · footprint {p.Width:0.##}×{p.Height:0.##}", _small);
            if (p.Group != LevelItemGroup.Spawn)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Kích thước ×{p.Scale:0.##}", GUILayout.Width(110));
                if (GUILayout.Button("−")) Defer(() => ScaleActive(1f / WheelScaleStep));
                if (GUILayout.Button("+")) Defer(() => ScaleActive(WheelScaleStep));
                if (GUILayout.Button("1:1")) Defer(() => { RecordHistory(); p.Scale = 1f; MarkDirty(); RefreshView(_selected); });
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"Xoay ({p.Rotation * 90}°)")) Defer(RotateActive);
                if (GUILayout.Button(p.FlipX ? "Lật: sx = -1" : "Lật: sx = 1")) Defer(FlipActive);
                GUILayout.EndHorizontal();
            }
            if (p.IsPortal)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Cặp portal #{p.PairId}", GUILayout.Width(110));
                if (GUILayout.Button("−")) Defer(() => SetPortalPair(_selected, -1));
                if (GUILayout.Button("+")) Defer(() => SetPortalPair(_selected, 1));
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Nhân bản (Ctrl+D)")) Defer(DuplicateSelected);
            if (GUILayout.Button("Xóa (Del)")) Defer(() => RemoveAt(_selected));
            GUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            GUI.Box(_bottomRect, GUIContent.none);
            var mode = _tool == Tool.Place && _brush != null ? $"Đặt: {_brush.DisplayName}" : "Chọn";
            var text = $"{mode}   |   ({_mouseWorld.x:0.00}, {_mouseWorld.y:0.00})   |   Map {_doc.Width}×{_doc.Height}" +
                       (_snap ? "   |   Snap 0.5" : "") + "   |   " + _status;
            GUI.Label(new Rect(8, _bottomRect.y + 4, _bottomRect.width - 16, BottomHeight - 6), text);
        }

        /// <summary>Nhãn cặp portal và số thứ tự spawn ngay trên map.</summary>
        private void DrawLabels()
        {
            var spawn = 0;
            for (var i = 0; i < _doc.Placements.Count; i++)
            {
                var p = _doc.Placements[i];
                string text;
                if (p.Group == LevelItemGroup.Spawn) text = "S" + ++spawn;
                else if (p.IsPortal) text = "P" + p.PairId;
                else continue;
                var screen = viewCamera.WorldToScreenPoint(new Vector3(p.X, p.Y + p.Height / 2f + .2f, 0f));
                if (screen.z < 0) continue;
                var rect = new Rect(screen.x - 20, Screen.height - screen.y - 18, 40, 18);
                GUI.Label(rect, text, _header);
            }
        }

        // ---------- File ----------

        private void Confirm(string key, string question, Action action)
        {
            if (!_dirty || (_pendingConfirm == key && Time.unscaledTime < _pendingConfirmUntil))
            {
                _pendingConfirm = null;
                action();
                return;
            }
            _pendingConfirm = key;
            _pendingConfirmUntil = Time.unscaledTime + 4f;
            SetStatus(question + " Bấm lần nữa trong 4 giây để xác nhận.");
        }

        private void NewMap()
        {
            var doc = new LevelDocument();
            if (int.TryParse(_widthText, out var w)) doc.Width = Mathf.Clamp(w, LevelDocument.MinMapSize, LevelDocument.MaxMapSize);
            if (int.TryParse(_heightText, out var h)) doc.Height = Mathf.Clamp(h, LevelDocument.MinMapSize, LevelDocument.MaxMapSize);
            _path = "";
            ReplaceDocument(doc);
            SetStatus($"Map mới {doc.Width}×{doc.Height}.");
        }

        private void ApplyMapSize()
        {
            if (!int.TryParse(_widthText, out var w) || !int.TryParse(_heightText, out var h))
            {
                SetStatus("Rộng/Cao phải là số nguyên.");
                return;
            }
            w = Mathf.Clamp(w, LevelDocument.MinMapSize, LevelDocument.MaxMapSize);
            h = Mathf.Clamp(h, LevelDocument.MinMapSize, LevelDocument.MaxMapSize);
            _widthText = w.ToString();
            _heightText = h.ToString();
            if (w == _doc.Width && h == _doc.Height) { GenerateGround(); return; }
            RecordHistory();
            _doc.Width = w;
            _doc.Height = h;
            MarkDirty();
            GenerateGround();
            FrameMap();
            var outside = 0;
            foreach (var p in _doc.Placements) if (p.X > w || p.Y > h) outside++;
            SetStatus($"Ground {w}×{h}." + (outside > 0 ? $" {outside} vật thể nằm ngoài map mới." : "") + (w != h ? " Lưu ý: game chỉ nhận map vuông." : ""));
        }

        private void OpenWithDialog()
        {
#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanel("Mở level", LevelsDirectory, "json");
            if (!string.IsNullOrEmpty(path)) Load(path);
#else
            SetStatus("Chọn file trong danh sách Levels/ ở cột phải.");
#endif
        }

        private void Load(string path)
        {
            try
            {
                var document = LevelDocument.FromJson(File.ReadAllText(path), catalog.BaseSizeOf);
                var unknown = new HashSet<string>();
                foreach (var p in document.Placements) if (catalog.Find(p.Type) == null) unknown.Add(p.Type);
                _path = Path.GetFullPath(path);
                ReplaceDocument(document);
                SetStatus($"Đã mở {Path.GetFileName(path)}: {document.Placements.Count} vật thể." +
                          (unknown.Count > 0 ? " Loại không có trong catalog: " + string.Join(", ", unknown) : ""));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is FormatException ||
                                      e is Newtonsoft.Json.JsonException || e is InvalidCastException || e is ArgumentException)
            {
                SetStatus($"Không mở được {Path.GetFileName(path)}: {e.Message}");
            }
        }

        private void Save(bool saveAs)
        {
            var path = _path;
            if (saveAs || string.IsNullOrEmpty(path))
            {
#if UNITY_EDITOR
                var name = string.IsNullOrEmpty(_path) ? "level.json" : Path.GetFileName(_path);
                path = UnityEditor.EditorUtility.SaveFilePanel("Lưu level", LevelsDirectory, name, "json");
#else
                path = Path.Combine(LevelsDirectory, $"level_{DateTime.Now:yyyyMMdd_HHmmss}.json");
#endif
                if (string.IsNullOrEmpty(path)) return;
            }
            try
            {
                File.WriteAllText(path, _doc.ToJson());
                _path = Path.GetFullPath(path);
                _dirty = false;
                RefreshFiles();
                RunValidation();
                SetStatus($"Đã lưu {Path.GetFileName(path)}." + (_validation.Length == 0 ? " Map hợp lệ cho game." : " Map CHƯA hợp lệ cho game (xem cột phải)."));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                SetStatus($"Không lưu được: {e.Message}");
            }
        }

        private void RunValidation()
        {
            _validation = _doc.Validate(out _validationHash) ?? "";
            SetStatus(_validation.Length == 0 ? "Kiểm tra: map hợp lệ." : "Kiểm tra: " + _validation);
        }

        private void RefreshFiles()
        {
            try { _files = Directory.Exists(LevelsDirectory) ? Array.ConvertAll(Directory.GetFiles(LevelsDirectory, "*.json"), Path.GetFullPath) : Array.Empty<string>(); }
            catch (IOException) { _files = Array.Empty<string>(); }
            Array.Sort(_files, StringComparer.OrdinalIgnoreCase);
        }
    }
}
