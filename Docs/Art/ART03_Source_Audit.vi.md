# ART-03 source audit

Sprite references and timed sprite keyframes only. No legacy scripts, controllers, float curves or animation events are copied. Damage flashes require replicated HP decrease; broken state requires the server flag. Portal teleport cues require a confirmed source event and are not inferred from proximity.

| Kind | Source | Status |
|---|---|---|
|Chest|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Sprites/01. Rương/chest.aseprite|Static sprite; replicated damage flash; broken artwork absent, hide after confirmed break|
|Pot|Assets/Imported/JSurviv/Prefabs/Destructibles/Bình Hoa.prefab|Static sprite; replicated damage flash; verified broken-pot debris sprite only (no poison script)|
|Bush|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Sprites/19. Bụi cây/bush1.aseprite|Static sprite; replicated damage flash|
|Swamp|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Sprites/Đầm lầy/swamp2.aseprite|Static sprite; replicated damage flash|
|Lava|Assets/Imported/JSurviv/Prefabs/Indestructibles/LAVA.prefab|Animated sprite curve: 9 frames|
|Portal|Assets/Imported/JSurviv/Prefabs/Obstacles/CanGoThrough/PORTAL.prefab|Animated sprite curve: 5 frames|
|Door|Assets/_JungleMaze/Art/JSurviv/Legacy2024/Obstacles/Tàn tích bỏ hoang/Abandoned_door_idle/open/close.anim|Server-state open/close sprite curves; legacy callback events discarded|
|Poison|Assets/Imported/JSurviv/Prefabs/Obstacles/CanGoThrough/HUNT_TRAP.prefab|Static sprite; replicated damage flash; hunt-trap artwork is a provisional marker, poison-specific art unverified|
|Supply|Assets/Imported/JSurviv/Prefabs/Destructibles/DragonEgg.prefab|Static sprite; replicated damage flash; DragonEgg is a provisional supply visual, DRG-01 is outside scope|
