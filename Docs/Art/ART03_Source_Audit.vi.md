# ART-03 source audit

Sprite references and timed sprite keyframes only. No legacy scripts, controllers, float curves or animation events are copied. Damage flashes require replicated HP decrease; broken state requires the server flag. Portal teleport cues require a confirmed source event and are not inferred from proximity.

| Kind | Source | Status |
|---|---|---|
|Chest|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Sprites/01. Rương/chest.aseprite|Static sprite; replicated damage flash; broken artwork absent, hide after confirmed break|
|Pot|Assets/_JungleMaze/Art/LevelEditor/Cainos/Prefab/Props/PF Props - Pot 01.prefab|Static sprite; replicated damage flash; verified broken-pot debris sprite only (no poison script)|
|Bush|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Sprites/19. Bụi cây/bush1.aseprite|Static sprite; replicated damage flash|
|Swamp|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Sprites/Đầm lầy/swamp2.aseprite|Static sprite; replicated damage flash|
|Lava|Assets/Imported/JSurviv/Prefabs/Indestructibles/LAVA.prefab|Animated sprite curve: 9 frames|
|Portal|Assets/Imported/JSurviv/Prefabs/Obstacles/CanGoThrough/PORTAL.prefab|Animated sprite curve: 5 frames|
|Door|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Sprites/Cửa đá/stone_door.aseprite|Server-state open/close sprite curves; legacy callback events discarded|
|Poison|Assets/_JungleMaze/Art/JSurviv/World/Obstacles/Animations/BIG_WILD_ROSE/BIG_WILD_ROSE.idle.anim|Animated sprite curve: 8 frames; thorn nest (GDD Ổ gai) shown as the animated mutated wild rose|
|Supply|Assets/Imported/JSurviv/Prefabs/Destructibles/DragonEgg.prefab|Static sprite; replicated damage flash; DragonEgg is a provisional supply visual, DRG-01 is outside scope|
