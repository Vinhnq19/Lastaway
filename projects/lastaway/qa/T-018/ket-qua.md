# QA T-018 - Minimap zoom 2 mức + waypoint (chặng 1 + 2)

Kết luận: **ĐẠT** (0 lỗi Nặng, 0 lỗi Vừa; 2 lỗi Nhẹ; phím N, M, ESC thật chưa kiểm được). Bậc VERIFIED trong Editor, chưa PLAYTESTED.
QA: qa · Ngày: 2026-11-08 · Working tree chưa commit, Unity 6000.3.15f1, trận Luyện tập offline, Game view 1920x1080 (phóng to, scale 0,82). Click và cuộn là chuột thật; phím không vào được Game view qua công cụ.

## Theo tiêu chí hoàn thành
| Tiêu chí | Kết quả | Bằng chứng |
|---|---|---|
| Minimap mức Toàn 150 cell, mức Gần 30 cell theo Hero | Đạt | `qa/t18_m1.png`, `qa/t18_m2.png`; đọc `Zoom.Cells` = 150 và 30; cửa sổ Gần kẹp trong map (Hero sát mép thì marker lệch góc) |
| Cuộn chuột trên minimap đổi mức | Đạt | cuộn thật lên: mức 2, 30 cell; cuộn xuống: mức 1, 150 cell |
| Cuộn ngoài minimap không đổi mức | Đạt | cuộn thật tại giữa màn: mức vẫn 1 |
| Phím N đổi mức | **Chưa kiểm** | Shortcut/Type không tới Game view (5 lần thử, đã click lấy focus); đã đọc code `MinimapView.Update` dùng `GetKeyDown(N)` gọi `Toggle()`, cùng đường với cuộn |
| M mở bản đồ lớn | **Chưa kiểm bằng phím** | mở bằng `HudBigMapView.Open()` (cùng đường M gọi) |
| Bản đồ lớn chặn tấn công, vẫn đi được | Đạt (đi: kiểm trạng thái) | bộ đếm sự kiện `AttackPredicted` của `OfflineWorldSession`: 5 click thật trên bản đồ lớn = **0** đòn; đóng bản đồ rồi click 1 lần = **1** đòn (đối chứng). `UiLayerStack.BlocksActions` = true, `HeroController.enabled` = true khi bản đồ mở. Chưa nhấn WASD thật |
| Đặt waypoint bằng click trái | Đạt | click thật tại điểm bản đồ lớn cho (49,02; 116,31) đúng bằng `TryMapPoint` của chính con trỏ; `qa/t18_real_bigmap.png` |
| Đặt mới thay cũ (chỉ 1) | Đạt | click (49,02; 116,31) rồi (83,62; 81,92): còn đúng 1, ở điểm mới; `qa/t18_real_replace.png` |
| Gỡ: click trái trong 1,5 cell | Đạt | click cách cờ 0,6 cell: Has = false |
| Gỡ: chuột phải bất kỳ chỗ nào trên map | Đạt | chuột phải ở điểm cách xa cờ: Has = false |
| Click ngoài ô bản đồ (chú giải, thẻ) | Đạt | click ở thẻ chú giải: không đặt cờ |
| Tự xóa khi Hero vào 1,5 cell | Đạt | click thật đặt cờ cách Hero 1,06 cell: cờ biến ngay frame sau (X,Y còn lưu 125,56; 91,36) |
| Xóa khi chết | Đạt (quan sát) | Hero chết do bo giữa lượt, click đặt cờ sau đó: Has = false |
| Mở ESC khi bản đồ mở | Đạt qua API, **chưa kiểm phím** | `MatchOverlayInput.Poll` giả ESC: lần 1 đóng bản đồ, lần 2 mở menu, `Open()` bản đồ khi menu mở bị từ chối, ESC đóng menu. Ghi chú: trận offline của tôi ở trạng thái màn `Results` sau khi Hero chết, phải ép `InMatch` mới mở được menu; chưa thấy ESC mở menu trong trận offline đang sống |
| Mũi tên mép màn có số cell, né HUD | Đạt | cờ về 4 hướng: Đông-Bắc (Minimap), Đông (Match status), Tây-Nam (Vitals), Nam (Quick bar): mũi tên dừng ngoài mép panel, đúng hướng, số 28, 26, 144, 92 đọc rõ; `qa/mui-ten-3-huong.png`, `qa/crop_ne.png` |
| Cờ trên minimap (mức Toàn và Gần) | Đạt | `qa/t18_ne_minimap.png`, `qa/click-that-ban-do-va-minimap-gan.png`: cờ 18 px + đường mảnh; mức Gần cờ nằm sát mép khung theo hướng tới cờ |
| Cổng 9-slice | Đạt | panel, badge số, thẻ không méo ở 1080p |
| Cỡ chữ | Đạt | chữ số khoảng cách: HudTextBold 16 x 1,5 = 24 px, đúng thang; cổng Unity 404 chữ, 1 cảnh báo "gameplay dưới 24" (cùng số lượng với mốc, nhiều khả năng ô số rỗng HudSlot cũ) |
| Tương phản số khoảng cách | Đạt | chữ kem (0,945; 0,906; 0,784) trên badge nâu tối, đọc rõ trên cỏ sáng |

## Test tự động
| Bộ | Kết quả | Mốc |
|---|---|---|
| EditMode | 1159/1171, 12 đỏ cũ (ArtAudioLevelTests x5, LevelDocumentTests x4, Phase2Tests.PackagedMapHashMustMatchServer, OfflineWorldSessionTests.RealOfflineAttack_RebindingAudioDoesNotDuplicate_AndDisposeUnsubscribes, OnlineWorldIntegrationTests.NormalWorldUsesAuthoredTeamMapWithFixedInteractivePlacements) | 1159/1171 |
| PlayMode | 200/201, đỏ cũ DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose (10,10 không vượt 10,2) | 200/201 |

## Bug phát hiện
| # | Lỗi | Mức |
|---|---|---|
| T18-1 | Chú giải bản đồ lớn: 3 icon chuột giống hệt nhau, không phân biệt chuột trái, phải, cuộn | Nhẹ |
| T18-2 | Khi Hero ở sát mép map, mức Gần đặt marker lệch góc khung (đúng luật kẹp, chỉ ghi nhận cho Huy cân nhắc khi chơi) | Nhẹ |
| Ghi nhận | Console có 1000+ dòng `GUI Error: Invalid GUILayout state` + `ArgumentOutOfRangeException` sau khi một lượt Play bị bỏ treo cho tới hết trận (màn Results); không lặp ở lượt Play mới; không có stack, chưa rõ nguồn | Thông tin |

## Chưa kiểm
Phím N, M, ESC thật; đi bằng WASD khi bản đồ mở (kiểm trạng thái); ESC mở menu trong trận offline đang sống; 1280 và 2560; build player; chạy online (server thật); lớp địch (ngoài phạm vi).
Ảnh chứng cứ ở `qa/`. Cách làm click thật: phóng to Game view, ánh xạ tọa độ màn hình sang Input.mousePosition và đối chiếu bằng `TryMapPoint` trước khi click.
