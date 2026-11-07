# Baseline DepthSorting (T-013)

Lệnh: git diff --stat HEAD -- <file test> + các file T-013 đổi; git log file test

## File test và code nó dùng có đổi trong T-013 không
```
(trống = không đổi)
```
## Mọi file T-013 đổi so với HEAD
```
 M Assets/_JungleMaze/Prefabs/UI/Hud.prefab
 M Assets/_JungleMaze/Scripts/Presentation/UI/Hud/HudGaugeRowView.cs
 M Assets/_JungleMaze/Scripts/Presentation/UI/Hud/QuickbarView.cs
 M Assets/_JungleMaze/Tests/PlayMode/HudAcceptanceTests.cs
 M Assets/_JungleMaze/Tests/PlayMode/HudFeedbackAcceptanceTests.cs
 M Assets/_JungleMaze/Tests/PlayMode/HudFigmaV2Feedback.cs
 M Assets/_JungleMaze/Tests/PlayMode/UiArtIntegrationTests.cs
?? Assets/_JungleMaze/Art/Hud/StatusIcons.meta
?? Assets/_JungleMaze/Art/Hud/StatusIcons/
?? Assets/_JungleMaze/Scripts/Presentation/UI/Hud/MinimapMapCapture.cs
?? Assets/_JungleMaze/Scripts/Presentation/UI/Hud/MinimapMapCapture.cs.meta
?? Assets/_JungleMaze/Tests/PlayMode/HudLayoutB.cs
?? Assets/_JungleMaze/Tests/PlayMode/HudLayoutB.cs.meta
?? Assets/_JungleMaze/Tools/Editor/Hud.meta
?? Assets/_JungleMaze/Tools/Editor/Hud/
```
## Lịch sử file test
```
77607e85 Vũ Nguyễn Hoàng 2026-11-03 feat(client): add dirty-queue depth sorting for top-down entities
```
    1:using System.Collections;
    2:using System.Collections.Generic;
    3:using System.Reflection;
    4:using JungleMaze.Presentation.Player;
    5:using JungleMaze.Presentation.Rendering;
    6:using NUnit.Framework;
    7:using UnityEngine;
    8:using UnityEngine.Rendering;
    9:using UnityEngine.TestTools;
    10:using UnityEngine.TestTools.Constraints;
    11:using Is =UnityEngine.TestTools.Constraints.Is;
    12:using Object = UnityEngine.Object;
    21:    public sealed class DepthSortingPlayTests
    208:            var body = go.GetComponent<Rigidbody2D>();

## Lần chạy trước T-013 (cùng phiên, cùng Editor)
- Job PlayMode `f425c028...`: bắt đầu 10:53:18, xong 10:54:18 ngày 06/11, 133 ca, đỏ đúng 1 ca `DepthSortingPlayTests.HeroWithInterpolatedRigidbodySortsByFinalFramePose` ("Hero đã thực sự di chuyển", 10,107 < 10,2). File: `test-PlayMode-truoc-T013.json`, mục `progress.failures_so_far`.
- File T-013 sửa sớm nhất là `HudGaugeRowView.cs` lúc 10:56:22, tức sau lần chạy này; prefab sửa lúc 11:14.
- Không dùng `git stash`, vì luật phiên cấm stash trên checkout dùng chung. Thay vào đó: lần chạy trên là trước khi sửa, và `git diff` cho thấy file test cùng `Presentation/Player`, `Presentation/Rendering` không đổi.
