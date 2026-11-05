using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JungleMaze.LevelEditor;
using UnityEditor;
using UnityEngine;

namespace JungleMaze.EditorTools
{
    /// <summary>
    /// Sinh 2 map mẫu 150×150 kiểu battle royale nhiều khu (tháp lớn, làng nhà tường gạch + cửa gỗ, rừng, mỏ đá, nghĩa địa,
    /// đền, trại) bằng đúng model <see cref="LevelDocument"/> và catalog của Level Editor: mọi vật đặt theo ô qua
    /// <see cref="LevelDocument.IsFree"/>, map chỉ được lưu khi <see cref="LevelDocument.Validate"/> hợp lệ. Mở file trong Level
    /// Editor để sửa tiếp. Tất định theo seed: chạy lại cho cùng kết quả.
    /// </summary>
    public static class LevelSampleMaps
    {
        public const int Size = 150;
        public static readonly string[] Files = { "map1_thap_co", "map2_thi_tran" };
        private const string CatalogPath = "Assets/_JungleMaze/Art/LevelEditor/LevelEditorCatalog.asset";

        [MenuItem("Jungle Maze/Level Editor/Generate Sample Maps 150")]
        public static void GenerateMenu() => Debug.Log("[LevelSampleMaps]\n" + GenerateAll());

        public static string GenerateAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null || catalog.Find(LevelWalls.Id) == null)
                throw new InvalidOperationException("Catalog thiếu Tường gạch: chạy Jungle Maze/Level Editor/Build Level Editor Scene trước.");
            var report = new StringBuilder();
            for (var variant = 1; variant <= 2; variant++)
            {
                var doc = new Builder(catalog, variant).Build(out var summary);
                var error = doc.Validate(out var hash);
                report.AppendLine($"{Files[variant - 1]}: {summary} → {(error ?? "hợp lệ, hash " + hash)}");
                if (error != null) continue;
                var path = Path.Combine(Application.dataPath, "_JungleMaze/Levels", Files[variant - 1] + ".json");
                File.WriteAllText(path, doc.ToJson().Replace("\r\n", "\n"), new UTF8Encoding(false));
            }
            AssetDatabase.Refresh();
            return report.ToString();
        }

        private sealed class Builder
        {
            private readonly LevelCatalog _catalog;
            private readonly int _variant;
            private readonly LevelDocument _doc;
            private readonly System.Random _rng;
            private int _skipped;

            public Builder(LevelCatalog catalog, int variant)
            {
                _catalog = catalog;
                _variant = variant;
                _doc = new LevelDocument { Width = Size, Height = Size, Seed = variant == 1 ? 3101 : 4202, Decor = .05f, Version = "1.1.0" };
                _rng = new System.Random(variant * 7919);
            }

            public LevelDocument Build(out string summary)
            {
                if (_variant == 1) MapTower(); else MapTown();
                var spawns = PlaceSpawns();
                // Spawn đứng đầu danh sách (game BFS từ spawn đầu tiên).
                var ordered = new List<LevelPlacement>(_doc.Placements.Count);
                foreach (var p in _doc.Placements) if (p.Group == LevelItemGroup.Spawn) ordered.Add(p);
                foreach (var p in _doc.Placements) if (p.Group != LevelItemGroup.Spawn) ordered.Add(p);
                _doc.Placements.Clear();
                _doc.Placements.AddRange(ordered);

                var counts = new SortedDictionary<string, int>();
                foreach (var p in _doc.Placements) counts[p.Type] = counts.TryGetValue(p.Type, out var n) ? n + 1 : 1;
                var text = new StringBuilder($"{_doc.Placements.Count} vật, {spawns} spawn, bỏ {_skipped} ô trùng |");
                foreach (var kv in counts) text.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
                summary = text.ToString();
                return _doc;
            }

            // ---------- Bố cục ----------

            /// <summary>Map 1 "Tháp cổ": tháp lớn giữa map, đường chữ thập ra 4 phía, mỗi góc một khu.</summary>
            private void MapTower()
            {
                Portals(new Vector2Int(4, 143), new Vector2Int(143, 4), new Vector2Int(143, 143), new Vector2Int(4, 4));
                Road(73, 2, 76, 58); Road(73, 92, 76, 147); Road(2, 73, 58, 76); Road(92, 73, 147, 76);
                Tower(63, 63);
                Village(14, 98, 3, 2, 10, 8, 5);                 // làng tây bắc
                Camp(120, 120); Forest(96, 96, 146, 146, .62f);    // rừng đông bắc + trại thợ săn
                Quarry(8, 8, 56, 56); Camp(30, 30);                // mỏ đá tây nam
                Graveyard(102, 14, 32, 30);                        // nghĩa địa đông nam
                Ruins(118, 62);                                    // đền đổ nát phía đông
                House(18, 60, 12, 9, new[] { new Vector2Int(24, 60), new Vector2Int(29, 64) }, 6); // trang trại phía tây
                House(36, 80, 9, 7, new[] { new Vector2Int(40, 80) }, 4);
                House(80, 22, 10, 8, new[] { new Vector2Int(84, 22) }, 5); Camp(66, 30); // nhà/trại đặt trước rừng để cây không chiếm ô tường
                Forest(60, 100, 90, 146, .35f);
                Forest(60, 4, 90, 50, .3f);
                Forest(4, 58, 14, 92, .5f); Forest(136, 58, 146, 92, .5f);
            }

            /// <summary>Map 2 "Thị trấn": thị trấn lưới có quảng trường giữa, tháp đông bắc, rừng tây nam, mỏ tây bắc, nghĩa địa đông nam.</summary>
            private void MapTown()
            {
                Portals(new Vector2Int(5, 49), new Vector2Int(141, 96), new Vector2Int(48, 140), new Vector2Int(140, 4));
                Road(2, 52, 147, 53); Road(52, 2, 53, 147); Road(98, 2, 99, 104); Road(2, 98, 104, 99);
                Village(58, 58, 3, 3, 9, 7, 5);                    // thị trấn trung tâm, ô giữa là quảng trường
                Tower(110, 110);                                   // tháp đông bắc
                Camp(26, 26); Forest(4, 4, 48, 48, .66f); Forest(4, 56, 22, 94, .5f); // rừng tây nam
                Quarry(8, 104, 46, 144); Camp(28, 124);            // mỏ đá tây bắc
                Graveyard(108, 12, 34, 30);                        // nghĩa địa đông nam
                Ruins(76, 26);                                     // đền phía nam
                House(116, 62, 12, 9, new[] { new Vector2Int(122, 62), new Vector2Int(116, 66) }, 6); // trang trại đông
                House(132, 80, 10, 8, new[] { new Vector2Int(136, 80) }, 5);
                House(60, 120, 11, 8, new[] { new Vector2Int(65, 120) }, 5); House(78, 132, 9, 7, new[] { new Vector2Int(82, 132) }, 4);
                Forest(56, 104, 104, 146, .25f); Forest(102, 50, 146, 58, .4f);
            }

            // ---------- Khu vực ----------

            /// <summary>Nhà: viền tường kín, cửa gỗ (tương tác) ở các ô cho trước, sàn đá, nội thất ngẫu nhiên.</summary>
            private void House(int x, int y, int w, int h, Vector2Int[] doors, int loot)
            {
                WallLine(x, y, x + w - 1, y, doors); WallLine(x, y + h - 1, x + w - 1, y + h - 1, doors);
                WallLine(x, y + 1, x, y + h - 2, doors); WallLine(x + w - 1, y + 1, x + w - 1, y + h - 2, doors);
                foreach (var d in doors) Put("Door", d.x, d.y);
                Road(x + 1, y + 1, x + w - 2, y + h - 2);
                var spots = new List<Vector2Int>();
                for (var i = x + 1; i < x + w - 1; i++) spots.Add(new Vector2Int(i, y + h - 2));
                for (var j = y + 2; j < y + h - 2; j++) { spots.Add(new Vector2Int(x + 1, j)); spots.Add(new Vector2Int(x + w - 2, j)); }
                for (var k = 0; k < loot && spots.Count > 0; k++)
                {
                    var pick = _rng.Next(spots.Count);
                    var s = spots[pick];
                    spots.RemoveAt(pick);
                    switch (_rng.Next(5))
                    {
                        case 0: Put("Chest", s.x, s.y); break;
                        case 1: Put("Pot", s.x, s.y); break;
                        case 2: Breakable("Barrel", s.x, s.y, 15, "Pot"); break;
                        case 3: Breakable("CrateSmall", s.x, s.y, 25, "Chest"); break;
                        default: Put("StoneLantern", s.x, s.y); break;
                    }
                }
            }

            /// <summary>Tháp lớn 24×24: ụ góc 3×3, 4 cửa giữa cạnh, vách chữ thập chia 4 phòng có cửa, sảnh giữa có thùng tiếp tế.</summary>
            private void Tower(int x, int y)
            {
                const int s = 24;
                int m = x + s / 2, n = y + s / 2;
                for (var cx = 0; cx < 2; cx++)
                for (var cy = 0; cy < 2; cy++)
                for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++) Put(LevelWalls.Id, x - 1 + cx * (s - 1) + i, y - 1 + cy * (s - 1) + j);
                House(x, y, s, s, new[] { new Vector2Int(m, y), new Vector2Int(m, y + s - 1), new Vector2Int(x, n), new Vector2Int(x + s - 1, n) }, 0);

                var inner = new[]
                {
                    new Vector2Int(m - 4, y + 5), new Vector2Int(m + 4, y + 5), new Vector2Int(m - 4, y + s - 6), new Vector2Int(m + 4, y + s - 6),
                    new Vector2Int(x + 5, n - 4), new Vector2Int(x + s - 6, n - 4), new Vector2Int(x + 5, n + 4), new Vector2Int(x + s - 6, n + 4),
                };
                WallLine(m - 4, y + 1, m - 4, n - 4, inner); WallLine(m + 4, y + 1, m + 4, n - 4, inner);
                WallLine(m - 4, n + 4, m - 4, y + s - 2, inner); WallLine(m + 4, n + 4, m + 4, y + s - 2, inner);
                WallLine(x + 1, n - 4, m - 5, n - 4, inner); WallLine(m + 5, n - 4, x + s - 2, n - 4, inner);
                WallLine(x + 1, n + 4, m - 5, n + 4, inner); WallLine(m + 5, n + 4, x + s - 2, n + 4, inner);
                foreach (var d in inner) Put("Door", d.x, d.y);

                Put("Supply", m - 2, n + 1); Put("Supply", m + 1, n - 2);
                Put("Statue01", m - 3, n - 3); Put("Statue01", m + 2, n - 3, true); Put("Statue01", m - 3, n + 2); Put("Statue01", m + 2, n + 2, true);
                foreach (var r in new[] { new Vector2Int(x + 2, y + 2), new Vector2Int(x + s - 8, y + 2), new Vector2Int(x + 2, y + s - 8), new Vector2Int(x + s - 8, y + s - 8) })
                {
                    Put("Chest", r.x + 2, r.y + 2); Breakable("CrateSmall", r.x + 3, r.y + 2, 25, "Chest"); Breakable("Barrel", r.x + 2, r.y + 3, 15, "Pot");
                    Put("Pot", r.x + 4, r.y + 4); Put("RunePillarX3", r.x + 1, r.y + 4);
                }
                // Sân quanh tháp: đường đá vòng, cột rune và đèn đá.
                Road(x - 5, y - 5, x + s + 4, y - 4); Road(x - 5, y + s + 3, x + s + 4, y + s + 4);
                Road(x - 5, y - 5, x - 4, y + s + 4); Road(x + s + 3, y - 5, x + s + 4, y + s + 4);
                for (var k = 0; k <= s; k += 6)
                {
                    Put("RunePillarX2", x - 3 + k, y - 3); Put("RunePillarX2", x - 3 + k, y + s + 2);
                    Put("StoneLantern", x - 3, y - 3 + k); Put("StoneLantern", x + s + 2, y - 3 + k);
                }
            }

            /// <summary>Làng/thị trấn: lưới nhà và phố đá; lưới 3×3 lấy ô giữa làm quảng trường (tâm map luôn thoáng).</summary>
            private void Village(int x0, int y0, int cols, int rows, int hw, int hh, int street)
            {
                for (var c = 0; c < cols; c++)
                for (var r = 0; r < rows; r++)
                {
                    var x = x0 + c * (hw + street);
                    var y = y0 + r * (hh + street);
                    if (cols == 3 && rows == 3 && c == 1 && r == 1)
                    {
                        Road(x - street + 1, y - street + 1, x + hw + street - 2, y + hh + street - 2);
                        Put("Well", x + hw / 2 + 1, y + 2);
                        Put("Statue01", x, y); Put("Statue01", x + hw - 1, y, true); Put("Statue01", x, y + hh - 1); Put("Statue01", x + hw - 1, y + hh - 1, true);
                        Put("StoneLantern", x + 2, y + hh - 2); Put("StoneLantern", x + hw - 3, y + hh - 2);
                        continue;
                    }
                    var side = (c + r) % 2 == 0 ? new Vector2Int(x + hw - 1, y + hh / 2) : new Vector2Int(x, y + hh / 2);
                    House(x, y, hw, hh, new[] { new Vector2Int(x + hw / 2, y), side }, 4);
                    Road(x - street + 1, y - street + 1, x + hw + street - 2, y - 1);
                    Road(x - street + 1, y, x - 1, y + hh - 1);
                }
                var east = x0 + cols * (hw + street);
                var north = y0 + rows * (hh + street);
                Road(east - street + 1, y0 - street + 1, east - 1, north - street);
                Road(x0 - street + 1, north - street, east - 1, north - 1);
            }

            private void Forest(int x0, int y0, int x1, int y1, float density)
            {
                for (var x = x0; x <= x1; x += 2)
                for (var y = y0; y <= y1; y += 2)
                {
                    if (_rng.NextDouble() > density) continue;
                    var tx = x + _rng.Next(2);
                    var ty = y + _rng.Next(2);
                    var roll = _rng.NextDouble();
                    if (roll < .78) Put("Tree0" + (1 + _rng.Next(3)), tx, ty, _rng.Next(2) == 0, true);
                    else if (roll < .93) Put("Bush", tx, ty, false, true);
                    else Put(_rng.Next(2) == 0 ? "Rock05" : "Rock06", tx, ty, false, true);
                }
            }

            private void Quarry(int x0, int y0, int x1, int y1)
            {
                for (var x = x0; x <= x1; x += 3)
                for (var y = y0; y <= y1; y += 3)
                {
                    var roll = _rng.NextDouble();
                    var tx = x + _rng.Next(2);
                    var ty = y + _rng.Next(2);
                    if (roll < .25) Put("Rock07", tx, ty, _rng.Next(2) == 0, true);
                    else if (roll < .45) Put(_rng.Next(2) == 0 ? "Rock05" : "Rock06", tx, ty, false, true);
                    else if (roll < .55) Put("StoneCube", tx, ty, false, true);
                    else if (roll < .6) Breakable("CrateSmall", tx, ty, 25, "Chest");
                    else if (roll < .64) Put("Pot", tx, ty, false, true);
                }
            }

            private void Graveyard(int x0, int y0, int w, int h)
            {
                WallLine(x0, y0, x0 + w - 1, y0, new Vector2Int(x0 + w / 2, y0), new Vector2Int(x0 + w / 2 + 1, y0));
                Road(x0 + w / 2, y0, x0 + w / 2 + 1, y0 + h - 1);
                for (var x = x0 + 2; x < x0 + w - 2; x += 3)
                for (var y = y0 + 3; y < y0 + h - 2; y += 3)
                {
                    if (Math.Abs(x - (x0 + w / 2)) < 2) continue;
                    var roll = _rng.Next(6);
                    Put(roll < 3 ? "Gravestone0" + (1 + roll) : roll == 3 ? "CoffinH" : roll == 4 ? "StoneLantern" : "Pot", x, y, false, true);
                }
                Put("Statue01", x0 + w / 2, y0 + h - 1); Put("Chest", x0 + w / 2 + 1, y0 + h - 1);
            }

            private void Ruins(int cx, int cy)
            {
                Put("Well", cx - 1, cy); Put("Gate02", cx - 1, cy + 4); Put("Gate01", cx - 1, cy - 5);
                Put("Pillar01", cx - 5, cy - 3); Put("Pillar02", cx + 4, cy - 3); Put("Pillar01", cx - 5, cy + 3); Put("Pillar02", cx + 4, cy + 3);
                Put("Statue01", cx - 3, cy); Put("Statue01", cx + 2, cy, true); Put("BenchS", cx - 1, cy - 2); Put("Chest", cx, cy + 2);
                Put("RunePillarBroken", cx - 7, cy); Put("RunePillarX3", cx + 6, cy);
                Road(cx - 6, cy - 6, cx + 5, cy + 7);
            }

            private void Camp(int cx, int cy)
            {
                Breakable("Barrel", cx, cy, 15, "Pot"); Breakable("Barrel", cx + 1, cy, 15, "Pot"); Breakable("CrateSmall", cx, cy + 1, 25, "Chest");
                Put("BenchE", cx - 2, cy); Put("Chest", cx + 2, cy + 2); Put("Pot", cx - 1, cy + 3); Put("StoneLantern", cx + 3, cy - 1); Put("SignE", cx - 3, cy + 3);
            }

            private void Portals(params Vector2Int[] ends)
            {
                for (var i = 0; i < ends.Length; i++)
                {
                    var p = Put("Portal", ends[i].x, ends[i].y);
                    if (p != null) p.PairId = (ushort)(i / 2 + 1);
                }
            }

            /// <summary>10 spawn quanh vòng r=52 tính từ tâm, dời tới ô gần nhất có 3×3 thoáng và cách nhau ≥ 8.</summary>
            private int PlaceSpawns()
            {
                var spawns = new List<Vector2Int>();
                for (var i = 0; i < 10; i++)
                {
                    var a = (i * 36 + 18) * Mathf.Deg2Rad;
                    var tx = Mathf.RoundToInt(Size / 2f + 52 * Mathf.Cos(a));
                    var ty = Mathf.RoundToInt(Size / 2f + 52 * Mathf.Sin(a));
                    var done = false;
                    for (var r = 0; r < 12 && !done; r++)
                    for (var dx = -r; dx <= r && !done; dx++)
                    for (var dy = -r; dy <= r && !done; dy++)
                    {
                        var x = tx + dx;
                        var y = ty + dy;
                        if (!_doc.IsFree(x - 1, y - 1, 3, 3, false)) continue;
                        var far = true;
                        foreach (var s in spawns) if ((s - new Vector2Int(x, y)).sqrMagnitude < 64) far = false;
                        if (!far) continue;
                        Put("Spawn", x, y);
                        spawns.Add(new Vector2Int(x, y));
                        done = true;
                    }
                }
                return spawns.Count;
            }

            // ---------- Đặt vật ----------

            private bool OnPath(int x, int y, int w, int h)
            {
                for (var i = x; i < x + w; i++)
                for (var j = y; j < y + h; j++)
                    if (_doc.Path.Contains(new Vector2Int(i, j))) return true;
                return false;
            }

            private LevelPlacement Put(string type, int x, int y, bool flip = false, bool avoidPath = false)
            {
                var item = _catalog.Find(type);
                if (item == null) throw new InvalidOperationException("Catalog thiếu " + type);
                var decor = item.Group == LevelItemGroup.Decoration;
                if (!_doc.IsFree(x, y, item.Footprint.x, item.Footprint.y, decor)) { _skipped++; return null; }
                if (avoidPath && OnPath(x, y, item.Footprint.x, item.Footprint.y)) return null;
                var p = LevelPlacement.OnCell(type, item.Group, x, y, item.Shape);
                p.FlipX = flip;
                _doc.Placements.Add(p);
                return p;
            }

            private void Breakable(string type, int x, int y, ushort hp, string loot)
            {
                var p = Put(type, x, y);
                if (p == null) return;
                p.Destructible = true;
                p.Hp = hp;
                p.Loot = loot;
            }

            private void Road(int x0, int y0, int x1, int y1)
            {
                for (var x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
                for (var y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
                    if (x >= 0 && y >= 0 && x < Size && y < Size) _doc.Path.Add(new Vector2Int(x, y));
            }

            private void WallLine(int x0, int y0, int x1, int y1, params Vector2Int[] gaps)
            {
                for (var x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
                for (var y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
                    if (Array.IndexOf(gaps, new Vector2Int(x, y)) < 0) Put(LevelWalls.Id, x, y);
            }
        }
    }
}
