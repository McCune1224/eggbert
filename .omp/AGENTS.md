# OMP agent guide — Eggbert

Eggbert is a Godot 4.7, statically typed GDScript RPG: 640×360, pixel-art, top-down, and zero gravity. Read `../DESIGN.md`, `../ROADMAP.md`, `../LOGGING.md`, and `../docs/godot-editor-guide.md` before changing a scene or resource.

## Headless commands

Run from the repository root:

```bash
godot --headless --path . --editor --quit
godot --headless --path . --script res://tests/verify_migration_integrity.gd
```

Targeted scripts are listed in `../docs/verification.md`; use `FACTORY_LAYOUT_SCENE=AssemblyLine` or `ControlRoom` for the two factory layouts. The port has no separate compilation step; Godot import and the targeted verifiers are the verification contract.

## Runtime contracts

Autoload names are used directly: `WorldFlags`, `QuestManager`, `GameController`, `DialogManager`, `AudioManager`, `Player`, `FadeTransition`, `CutsceneController`, `DebugOverlay`, `SaveManager`, `Inventory`, `Equipment`, `CombatController`, `KeybindManager`, and `FactoryOpeningFlow`.

Level loading is deliberately non-overloaded:

```gdscript
GameController.load_level_at_position(scene_path, player_position)
GameController.load_level_at_transition(scene_path, target_transition_name)
```

Persistent nodes in `persist` provide `get_save_key`, `serialize`, `deserialize`, and `get_load_priority`. Save keys are `player`, `inventory`, `equipment`, and `world_flags`; storage is `user://savegame.tres`.

## Authoring conventions

Use snake_case files, functions, fields, and signals; PascalCase `class_name` declarations and node names; CONSTANT_CASE constants; typed collections and returns; tabs; `@export` and `@onready`; native `signal.connect`, `signal.emit`, and `await`; and `PackedScene.instantiate()`. Use the editor for nested `.tres` resources, tilemap data, and UIDs.

Physics layers: 1 Player, 2 Walls, 3 NPCs, 4 Bullets, 5 Interactables, 6 Enemies, 7 TriggerAreas, 8 PlayerHitbox, 9 EnemyHitbox, 10 Items. Inputs include WASD, E, Esc, Space, Shift, J, F, Tab, and Backtick.

Supported addons are AsepriteWizard, `nklbdev.aseprite_importers`, `level_assembly`, and `cutscene_inspector`. Do not configure an unestablished MCP server.

## Logging and workflow

`GameLogger` writes game-originated tagged lines to `user://logs/eggbert_YYYY-MM-DD.log`; use `EGGBERT_LOG_LEVEL=debug` for tracing and `EGGBERT_LOG_ECHO=0` for file-only output. Engine errors remain in Godot stdout/stderr. Read the newest log before repeating a debugging loop.

Work on `port/gdscript-migration` or another feature branch. Keep commits focused and submit review before integration; `main` is integration-only. Preserve unrelated working-tree changes and report exact paths and verifier commands.

## Design questions

Ask before deciding unresolved narrative, consumable, equipment, or difficulty details. `FEATURE_IDEAS.md` is not a priority queue.
