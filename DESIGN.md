# DESIGN.md — Eggbert

Settled game-design decisions. Feature status is tracked in `ROADMAP.md`; authoring details are in `docs/godot-editor-guide.md`.

## Core loop

```
Overworld (NPCs, quests, puzzles) ──→ Combat Arena ──→ Overworld
       ↑                              (dodge + counter)      │
       └──────────────────────────────────────────────────────┘
```

The overworld is a story-driven, top-down, zero-gravity space for talking to NPCs, pursuing quests, solving environmental puzzles, and exploring. Combat is a dedicated bullet-hell arena: Eggbert has no direct attack, and a proximity parry (J) within a brief timing window deals damage. Equipment can extend parry radius and damage.

## Overworld systems

- **Dialog:** `WorldFlags` select NPC lines. Optional choice menus offer 2–4 responses; arrow keys plus E select a response and the cutscene caller may set a flag from the selected index.
- **Pause menu (Esc):** EarthBound-style Items, Status, Map, Save, and Settings panels. Inventory and Equipment tabs provide Use/Equip controls.
- **Fast travel:** placed `WarpPoint` nodes unlock destinations. The pause menu lists unlocked warps and uses a fade before arrival.
- **World map:** a stylized region panel with no real-time player marker.
- **Audio:** each `BaseLevel` owns ambient loops; `AudioManager` plays music, ambience, and UI sounds.
- **Traversal:** push blocks, floor switches, switch-gated doors, conveyors, teleport pads, and related puzzles.

## Core systems

### World flags

`WorldFlags` is an autoload storing typed `Variant` values for dialog branching, quest progression, warp unlocks, and map reveals. It is persisted under the `world_flags` save key.

### Save system

There is one slot, `user://savegame.tres`. It stores Player position/health/level, WorldFlags, warp unlocks, inventory, equipment, and quest state. The menu Save action and level transitions trigger saves. `SaveManager` writes a Godot `Resource` and validates persistent nodes through the typed duck-typed contract (`get_save_key`, `serialize`, `deserialize`, `get_load_priority`). Invalid old resources are deleted and treated as a fresh run.

### Settings

Music and SFX volume, text speed (Instant/Fast/Normal), fullscreen, and 1×–4× window scale are supported. Key rebinding remains excluded.

### Quests and inventory

Quests are editor-authored ordered objectives backed by WorldFlags. Multiple linear quests may be active; one current objective can be pinned to the overworld HUD. There are no map paths, distance indicators, timers, or branching quest graphs. Inventory categories are Key Items, Consumables, and Equipment; item use is overworld-only.

### Cutscenes and dialog branches

Cutscenes are `.tres` `CutsceneResource` files containing typed `CutsceneStep` resources and optional `CutsceneCondition` branches (`FlagSet`, `FlagNotSet`, `ChoiceEquals`). `CutsceneController` runs them; no separate cutscene scene files are required. The action enum retains its serialized order and includes `LockPlayer`, `UnlockPlayer`, `MoveNpc`, `MovePlayer`, `FaceDirection`, `PlayAnimation`, `CameraMove`, `SayDialog`, `Wait`, `SetFlag`, `Fade`, `PromptChoice`, `Stop`, and `DialogBranch`.

`CutsceneTrigger` is an Area2D with exported `cutscene`, optional `dialog_lines`, `trigger_mode`, `once`, and `cutscene_id`. One-shot triggers use `cutscene_<id>` in WorldFlags and call `CutsceneController` directly. `DialogBranch` resources contain ordered `DialogNode` and `DialogResponse` data.

### Main menu and game over

The menu provides New Game, Continue, Settings, and Quit. Continue loads the single slot. At zero HP, the game fades to black, shows “You collapsed…”, and restores the last save location (or level entrance) with partial HP and no item loss.

## Combat

Player movement uses WASD, dash (Space), and sprint (Shift). Bullets collide with PlayerHitbox layer 8. Enemies use idle → telegraph → attack → cooldown, with spreads, aimed shots, zone denial, and laser patterns. The HUD is a minimal CanvasLayer containing an HP bar and enemy name.

`CombatController` saves the overworld scene and position, instantiates an arena, and returns through `GameController.load_level_at_position(scene_path, player_position)`. A re-entry guard rejects calls while already fighting; there is no pre-combat save. Arena victory returns to the saved overworld position, while defeat reloads the last save.

Health damage is `max(1, raw_damage - defense)`. Healing is clamped to MaxHP; zero HP emits `died`; revive restores at least one HP.

## Open design questions

- Concrete consumable names, effects, and heal values.
- Whether future equipment should use currently unused Attack and Speed fields.
- Difficulty and HP scaling.
- Full story and narrative conflict.

## Excluded unless future need

Party companions, key rebinding, multiple save slots, and combat item usage.

## Technical constraints

- Godot 4.7 and statically typed GDScript.
- 640×360 viewport with `canvas_items` stretch.
- Top-down, zero gravity.
- Resource save format via `ResourceSaver`.
- Headless verification scripts under `tests/`; use the editor for nested resource serialization.
