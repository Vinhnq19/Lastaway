# Level Editor Cainos — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Level Editor dùng art Cainos, đặt mọi thứ theo ô, collider chân, decor cỏ theo seed, cọ đường đá tự nối, tường dày mép nam.

**Architecture:** Logic thuần (chọn tile theo seed, auto-tile đường đá, chọn tile tường, chiếm ô) nằm trong `LevelGround.cs` / `LevelDocument.cs`, không phụ thuộc scene, có test EditMode. Catalog (`LevelCatalog`) được menu build sinh từ prefab Cainos: lấy các sprite con (để vẽ), collider chân (để xuất JSON) và footprint ô. Controller chỉ vẽ và nhận input.

**Tech Stack:** Unity 6000.3.15f1, URP 2D, Tilemap, IMGUI, Newtonsoft.Json, NUnit EditMode.

**Spec:** `Docs/superpowers/specs/2026-11-03-level-editor-cainos-design.md`

## Global Constraints

- 1 ô = 1 world unit = 32 px Cainos (PPU 32).
- Xuất JSON phải qua `AuthoredMapDocument.Build`; decor/đường đá không bao giờ vào `obstacles`/`worldObjects`.
- Cùng (seed, map) ⇒ cùng decor. Kết quả của một ô chỉ phụ thuộc (seed, x, y, và các ô đường đá lân cận của nó).
- `map_demo.json` mở/lưu giữ nguyên hash game (test hiện có phải qua).
- Không sửa `Backend/`, không sửa `map_demo.json`.
- Không còn sprite cũ trong catalog. Swamp/Lava/Poison bị bỏ khỏi catalog.

## Review Focus

1. Mở `map_demo.json` (vật lệch ô, kind không còn trong catalog) → vẫn hiện, giữ bounds, lưu ra cùng hash. *(Task 2 test)*
2. Cây/vật nhiều collider được lật ngang → collider lật theo, vẫn nằm trong footprint. *(Task 2 test)*
3. Tô đường đá ở mép map/góc map → không truy cập ngoài lưới, viền đúng. *(Task 3 test)*
4. Đổi kích thước map nhỏ đi khi đã có đường đá/vật → ô ngoài map bị bỏ khi lưu, không crash. *(Task 2 test)*
5. Seed âm / seed rất lớn → vẫn tất định. *(Task 3 test)*

---

### Task 1: Import Cainos + catalog sinh từ prefab

**Files:**
- Import: `C:\Users\HVU\Downloads\Pixel Art Top Down - Basic v1.2.3\Pixel Art Top Down - Basic v1.2.3.unitypackage`, rồi `Assets/Cainos/Pixel Art Top Down - Basic/Patch - URP 2D Lit - Unity 6.3.unitypackage` (qua `AssetDatabase.ImportPackage(path, false)`).
- Modify: `Assets/_JungleMaze/Scripts/LevelEditor/LevelCatalog.cs`
- Modify: `Assets/_JungleMaze/Tools/Editor/LevelEditor/LevelEditorBuild.cs`
- Modify: `Docs/Licenses/` (ghi nguồn Cainos)

**Interfaces — Produces:**
- `LevelVisualPart { Sprite Sprite; Vector2 Offset; Vector2 Scale; bool FlipX; int Rank; bool Shadow; }` — offset tính từ **góc dưới-trái footprint**.
- `LevelItemType` thêm: `Vector2Int Footprint`, `Rect[] Colliders` (theo góc dưới-trái footprint, chưa lật), `LevelVisualPart[] Parts`. Bỏ dùng `BaseSize` cho item mới (giữ field để đọc file cũ: `BaseSize = Footprint`).
- `LevelCatalog` thêm: `Sprite[] GrassGrid` (64, index = row*8+col, row đếm từ trên), `Sprite[] WallGrid` (256, 16×16), `Sprite[] GrassSprinkles` (sprite cỏ/hoa nhỏ của prefab Grass 01–15, Flower 01). Bỏ `GroundTiles`.

- [ ] Import 2 package; console không lỗi compile.
- [ ] Build catalog theo bảng mục 7 của spec. Với mỗi prefab: các `SpriteRenderer` con → `Parts` (Rank = thứ tự sorting layer Cainos × 1000 + sortingOrder; renderer bóng → `Shadow = true`, Rank thấp nhất). Các `Collider2D` (không phải trigger) → AABB, làm tròn 1/32. Footprint = hình chữ nhật ô nguyên nhỏ nhất chứa mọi collider khi đặt chân prefab ở tâm đáy footprint; mục nào spec chốt kích thước ô (rương/bình 1×1, bụi theo cỡ art, cửa theo hướng) thì dùng giá trị đó. Collider của world object = cả footprint.
- [ ] `GrassGrid`/`WallGrid` xếp theo vị trí `sprite.rect` trong texture, không theo tên.
- [ ] Chạy menu `Jungle Maze/Level Editor/Build Level Editor Scene`; log liệt kê mục không có collider (phải nhập tay) bằng 0.
- [ ] Commit `feat(level-editor): import Cainos and build catalog from prefabs`.

### Task 2: Mô hình tài liệu theo ô

**Files:**
- Modify: `Assets/_JungleMaze/Scripts/LevelEditor/LevelDocument.cs`
- Test: `Assets/_JungleMaze/Tests/EditMode/LevelEditor/LevelDocumentTests.cs`

**Interfaces:**
- Consumes: `LevelItemType.Footprint`, `.Colliders` qua resolver.
- Produces:
  - `LevelItemShape { int Width, Height; Rect[] Colliders; }` và `FromJson(string json, Func<string, LevelItemShape?> shape = null)`.
  - `LevelPlacement` thêm `bool OnGrid; int CellX, CellY; Rect[] Colliders`. Với `OnGrid`: `X = CellX + Width/2f`, `Y = CellY + Height/2f`, `Scale = 1`, `Rotation = 0`.
  - `LevelPlacement GameRects()` → `IEnumerable<WorldBounds>`: cover ⇒ từng collider (lật theo `FlipX` quanh trục giữa footprint) dịch tới ô; legacy ⇒ `Bounds`.
  - `LevelDocument`: `int Seed`, `HashSet<Vector2Int> Path`, `bool IsCellFree(int x, int y, LevelItemGroup group, int ignore = -1)`.
  - JSON: `editor = { tool, seed, path: [[x,y],…], placements: [...] }`. Có `editor.placements` thì đọc từ đó (mục theo ô: `type, group, cellX, cellY, flipX, pairId`; mục legacy: thêm `bounds, scale, rotation`). Không có thì đọc như cũ (mọi vật `OnGrid = false`).

- [ ] Test mới (viết trước, phải fail):
  - `CoverExportsOneObstaclePerFootCollider` — cây footprint 1×1 collider (0.375,0.1,0.25,0.2) tại ô (10,12) ⇒ `obstacles` có đúng 1 phần tử minX 10.375, minY 12.1, maxX 10.625, maxY 12.3.
  - `FlipMirrorsCollidersInsideFootprint` — footprint 3×1, collider (0,0,1,1) + flip ⇒ rect minX = cellX+2.
  - `GridPlacementsRoundTripThroughEditorBlock` — seed 1234, path {(1,1),(2,1)}, 1 cover + 1 portal cặp + 1 spawn ⇒ đọc lại đúng ô/flip/pair/seed/path; `Validate` cùng hash trước/sau.
  - `CellOccupancyBlocksSecondBlockingItemButNotDecor`.
  - `ShrinkingMapDropsOutsidePathCellsOnSave` — path (40,40), map 30×30 ⇒ JSON không còn ô đó.
  - Test cũ `OpeningAndSavingGameMapKeepsTheSameGameHash` vẫn qua (legacy).
- [ ] Cài đặt cho tới khi tất cả test LevelEditor qua (chạy bằng `mcp__unity__run_tests` EditMode, filter `JungleMaze.Tests.LevelEditor`).
- [ ] Commit `feat(level-editor): tile-based placements with foot colliders`.

### Task 3: Ground theo seed, đường đá, tường mép

**Files:**
- Create: `Assets/_JungleMaze/Scripts/LevelEditor/LevelGround.cs`
- Test: `Assets/_JungleMaze/Tests/EditMode/LevelEditor/LevelGroundTests.cs`

**Interfaces — Produces (static, thuần):**
- `float Hash01(int seed, int x, int y, int salt)` — hash số nguyên tất định (không dùng `System.Random`, không dùng `GetHashCode` của string).
- `int GrassCell(int seed, int x, int y)` → index trong `GrassGrid` (vùng hàng 0–3). Hoa/sỏi (cột 4–7) mọc theo cụm bằng value-noise bước ~6 ô.
- `int StoneCell(int seed, int x, int y, Func<int,int,bool> isPath)` → index tile đá trong `GrassGrid` (hàng 4–7), hoặc -1 nếu ô này không có đá. Ô path ⇒ tile 4 viên (mask 1111, chọn biến thể theo hash). Ô cỏ kề path ⇒ mask các viên quay về phía path, mỗi viên giữ lại với xác suất 0.6 theo hash; mask 0 ⇒ -1.
- `static readonly Dictionary<int, int[]> StoneTilesByMask` — mask 4 bit (TL=8, TR=4, BL=2, BR=1) → các index tile. **Lấy bằng đo pixel**: viết tạm script Python đếm pixel xám trong từng góc 16×16 của các tile hàng 4–7 của `TX Tileset Grass.png`; mask nào không có tile thì rút về mask con gần nhất có tile.
- `int[,] BorderTiles(int width, int height, int seed)` hoặc tương đương: tile rim cho ô mép trong map và 2 hàng tường dưới y=0 (index trong `WallGrid`), cửa sổ (tile cột 6, hàng 6–7) xen theo hash với xác suất ~0.12 và không sát nhau.
- `IEnumerable<(Vector2 pos, int sprinkle)> Sprinkles(int seed, int width, int height, Func<int,int,bool> blocked, float density)`.

- [ ] Test (viết trước):
  - `SameSeedSameGround` / `DifferentSeedDifferentGround` (map 50×50, so mảng index; khác nhau ≥ 30% ô).
  - `GroundCellIndependentOfOtherCells` — `GrassCell(s,x,y)` không đổi khi thêm path ở ô cách ≥ 2.
  - `NegativeAndHugeSeedsAreDeterministic` (seed -1, `int.MaxValue`).
  - `PathCellAlwaysFullStone`, `NorthNeighbourOfPathOnlyUsesBottomStones` (mask & 0b1100 == 0), `DiagonalNeighbourUsesSingleCornerStone`, `PathAtMapCornerDoesNotThrow`.
  - `BorderHasTwoWallRowsBelowSouthEdge` (map 10×10 ⇒ hàng -1 và -2 đủ 10 ô có tile).
- [ ] Cài đặt cho tới khi qua.
- [ ] Commit `feat(level-editor): seeded ground decor, stone path autotile, border wall`.

### Task 4: Controller / View / GUI

**Files:**
- Modify: `LevelEditorController.cs`, `LevelEditorController.View.cs`, `LevelEditorController.Gui.cs`
- Modify: `Docs/hvu/06_Level_Editor.vi.md`

**Interfaces — Consumes:** mọi thứ của Task 1–3.

- [ ] Input: con trỏ → ô `floor(mouse)`. Đặt: ô dưới-trái footprint = ô con trỏ trừ `(Footprint.x-1)/2` theo x (căn giữa), y = ô con trỏ. Bóng xem trước đỏ khi `!IsCellFree` hoặc vi phạm luật spawn hiện có. Kéo vật = theo ô. Mũi tên dịch 1 ô. Bỏ: lăn chuột scale, R xoay, G/Snap, "Kích thước ×", "Xoay". Shift lật giữ lại.
- [ ] Công cụ **Đường đá** (nút ở bảng trái + phím P): giữ chuột trái tô, chuột phải xoá, một lượt kéo = một bước undo; mỗi lần đổi chỉ vẽ lại 3×3 ô quanh ô đổi.
- [ ] View: ground Tilemap lấy `GrassCell`/`StoneCell`; Tilemap viền + tường từ `BorderTiles`; lớp sprinkle (SpriteRenderer, tạo lại khi đổi seed/vật/path); vật vẽ từ `Parts` (Rank → sortingOrder trong `SortingGroup` của vật, `SortingGroup.sortingOrder` theo đáy footprint); `Shadow` part dùng material Cainos shadow nếu render đúng, không thì Sprite-Unlit với alpha giữ nguyên. Viền footprint (H) vẽ cả hình chữ nhật collider màu khác.
- [ ] GUI cột phải: thay "Ground tile" bằng `Seed [____] [Ngẫu nhiên]` (đổi seed = một bước undo, vẽ lại ground ngay) và thanh mật độ cỏ.
- [ ] Hướng dẫn `06_Level_Editor.vi.md`: phím tắt và quy tắc mới.
- [ ] Kiểm bằng mắt trong Play: map 50×50 seed 1 có cây, tượng, đường đá chữ L, 10 spawn; chụp màn hình, so với ảnh overview; "Kiểm tra" báo hợp lệ.
- [ ] Commit `feat(level-editor): tile placement UI, path brush, seed input with Cainos art`.
