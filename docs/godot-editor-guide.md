# Godot Editor Guide — Eggbert

A developer's reference for using the Godot 4.7 editor with this project: plugins, resource authoring, conventions, and every component system.

---

## 1. Editor Setup & First Launch

### Plugins

The following editor plugins are enabled in `project.godot` > `editor_plugins`:

| Plugin | Status | Config key |
|--------|--------|------------|
| AsepriteWizard | Enabled | `res://addons/AsepriteWizard/plugin.cfg` |
| Cutscene Inspector | Enabled | `res://addons/cutscene_inspector/plugin.cfg` |
| Level Assembly | *Disabled* (opt-in) | `res://addons/level_assembly/plugin.cfg` |

To enable Level Assembly: `Project > Project Settings > Plugins > Level Assembly > Enable`.

### Project settings

- **Resolution**: 640x360, `canvas_items` stretch mode
- **Main scene**: `boot/GameInit.tscn`
- **Theme**: `res://assets/themes/eggbert_theme.tres` (`gui > theme > custom`)
- **Physics layers**: 10 named layers (see [Physics Layers Cheatsheet](#14-physics-layers-cheatsheet))
- **Input map**: WASD + E/interact + Esc/menu + Space/dash + Shift/sprint + F/check + J/parry + Tab/dialog log + Backtick/debug overlay

### MCP server (AI assistant tooling)

`godot-mcp` runs via npx and provides editor commands from AI assistants. Configuration is in `.omp/mcp.json`. Available commands are listed in [MCP Tool Reference](#13-mcp-tool-reference).

### Debug skip

Set `EGGBERT_SKIP_MENU=1` to bypass the main menu and load the last save directly. Configured in `.omp/mcp.json`.

Set `EGGBERT_LOAD_STATE=<slot>` to boot straight into a named dev save state (issue #168) — works even without a `savegame.tres`; falls back to committed fixtures in `tests/savestates/`. In-game: `Ctrl+S` capture quick slot, `Ctrl+L` load it, `Ctrl+M` open the save-state menu.

---

## 2. Custom Editor Plugins

### AsepriteWizard (`addons/AsepriteWizard/`)

Pipeline for importing Aseprite sprites into Godot as spritesheet frames, tileset textures, or static textures.

**UI location**: `Project > Tools > Aseprite Wizard` submenu:
- *Spritesheet Wizard Dock* — opens a bottom-panel dock for configuring frame imports
- *Imports Manager* — manages imported aseprite assets; can be docked to bottom panel as "Aseprite Imports Manager"
- *Config* — project-wide import settings

**Import plugins** (auto-enabled, run on file import):
- Noop import (passthrough)
- Sprite frames import → `SpriteFrames` resources for `AnimatedSprite2D`
- Tileset texture import → tileset textures for `TileSet`
- Static texture import → plain `Texture2D`

**Inspector docks**: Custom inspectors for `AnimatedSprite2D` and `Sprite` nodes let you swap frames directly from the inspector.

### Cutscene Inspector (`addons/cutscene_inspector/`)

Custom inspector that activates when selecting a `CutsceneResource` or `DialogBranch` resource.

**For CutsceneResource**: Shows a step-card UI instead of the raw `Steps` array. Each card represents one `CutsceneStep`. Buttons: Move up/down, Remove, Add (opens a type menu), Edit (opens the step in its own inspector for field editing). All operations go through UndoRedo.

**For DialogBranch**: Shows a node-card UI for the `Nodes` array. Each card is a `DialogNode`. Buttons: Add Node (auto-assigns `node_N` id), Move, Remove, Edit. UndoRedo-backed.

**Cross-reference panel (PuzzleCrossRefInspector)**: Activates on `CutsceneTrigger`, `DialogBranchTrigger`, `FloorSwitch`, `KeyDoor`, `Door` nodes. Shows a read-only panel listing linked resources and resolved scene tree references. Clickable buttons jump to the linked resource or select the node in the scene tree.

### Level Assembly (`addons/level_assembly/`)

Right-upper dock panel labeled "Level Assembly". Categorized buttons that instantiate preconfigured scene instances at `(0,0)` with UndoRedo support.

**Categories**: Transitions, Progress, Puzzles, Traversal, Hazards, Items, Story.

**Buttons**: Level Transition, Save Point, Door, Key Door, Timed Door, Floor Switch, Push Block, Teleport Pad, Conveyor, Moving Platform, Timed Spikes, Spike Tile, Weighted Plate, Pickup Item, Conditional Item, Cutscene Trigger, Dialog Branch Trigger.

Search/filter field filters buttons by name. Not enabled by default — enable in Project Settings > Plugins.

### `nklbdev.aseprite_importers` (`addons/nklbdev.aseprite_importers/`)

Low-level Aseprite file format support: image format loader + import plugins. Present in the project but not currently enabled in `editor_plugins` — enable it from Project Settings > Plugins if you need bare Aseprite file handling separate from the AsepriteWizard pipeline.

---

## 3. Working with Godot Resources

### Hand-authoring rules

- **Never hand-author `.tres` with nested sub-resources.** Godot's deserialization of inline sub-resources is unreliable across editor versions and can lose data.
- **Hand-author flat `.tres` only**: scalar fields, `ext_resource` references to other `.tres` files.
- **Use the Inspector** to create Resources, configure them, then **Save As** `.tres`. This guarantees correct serialization.
- **Array[ExtResource("N")] is invalid.** Use `Array[Resource]` with `script = ExtResource("N")` instead.
- **UIDs are hash-generated by the editor.** Never hand-write `uid://` — copy from the editor's filesystem dock.
- **`load_steps` in .tscn files** must count correctly (each `ext_resource` reference counts as a step). The editor manages this; hand-editing risks broken loading.

### When to use `[Export]` vs. dedicated `.tres`

| Use case | Approach |
|----------|----------|
| One-off values on a scene instance | `[Export]` fields on the script |
| Reusable data shared across scenes | Dedicated `.tres` resource (created via Inspector) |
| Complex nested data (cutscene steps, dialog trees) | `.tres` + custom inspector plugin |
| Item definitions | Static `ItemDatabase` registry in code (no `.tres`) |

---

## 4. Scene Hierarchy Conventions

Every level scene follows this structure:

```
Node2D (BaseLevel.cs)
├── CoreTilemapLayer (LevelTileMapLayer.cs)
│   └── [tilemap data — do not hand-edit]
├── [component nodes at root level]
│   ├── LevelTransition (Area2D)
│   ├── SavePoint (Area2D)
│   ├── WarpPoint (Area2D)
│   ├── NPCs (Area2D + CutsceneTrigger / DialogBranchTrigger)
│   ├── Doors, Switches, Puzzles
│   └── PickupItem / ConditionalItem
└── [optional child nodes]
```

Rules:
- **Root**: `Node2D` with `BaseLevel.cs` script — exports `LevelName`, `LevelMusic`, `LevelAmbience`
- **Tilemap**: Direct child of root, position `(0,0)`, assigned a `TileSet`. Minimum world-pixel bounds typically `1536×1024`. Never hand-edit `tile_map_data` or atlas sub-resources.
- **Components**: Placed as children of root, *not* nested under the tilemap. Stable node names are API surface (e.g. `ArrestCutscene`, `EggsileTransition`, `HubArrival`).
- **Interactions**: NPCs use `Area2D` with `CollisionShape2D` + `CutsceneTrigger` or `DialogBranchTrigger`.
- **Naming**: PascalCase. Node names referenced by other systems (transitions, triggers) must be stable.

---

## 5. Puzzle & Component Reference

| Component | Scene | C# Class | Key Exports | Usage Notes | Editor Plugin? |
|-----------|-------|----------|-------------|-------------|----------------|
| Level Transition | `LevelTransition.tscn` | `LevelTransition` | `Level` (scene), `TargetTransitionName`, `Side`, `Size`, `RequiredFlag` | Size/Scale controls collision area. Side determines player exit offset. RequiredFlag gates post-ending exits. | Yes |
| Save Point | `SavePoint.tscn` | `SavePoint` | `LocationName` | Interact to heal + save. | Yes |
| Door | `Door.tscn` | `Door` | `StartOpen` | Toggle open/closed. Emits `Opened`/`Closed` signals. | Yes |
| Key Door | `KeyDoor.tscn` | `KeyDoor` | `RequiredFlag`, `LockedMessage` | Gated by a WorldFlag. Shows LockedMessage when missing flag. | Yes |
| Timed Door | `TimedDoor.tscn` | `TimedDoor` | `OpenDuration` | Auto-closes after `OpenDuration` seconds. | Yes |
| Floor Switch | `FloorSwitch.tscn` | `FloorSwitch` | `TargetDoorPath` | Set TargetDoorPath **after** placing both switch and door in the scene. | Yes |
| Push Block | `PushBlock.tscn` | `PushBlock` | `DirectionalMode` | Needs full tile of clearance. DirectionalMode snaps to dominant axis. | Yes |
| Teleport Pad | `TeleportPad.tscn` | `TeleportPad` | `TargetPadPath` | Paired: place two, set each other's TargetPadPath. Cooldown prevents re-trigger. | Yes |
| Conveyor Tile | `ConveyorTile.tscn` | `ConveyorTile` | `Direction`, `Speed` | Pushes player. Sprint to move against it. Reserve escape route. | Yes |
| Moving Platform | `MovingPlatform.tscn` | `MovingPlatform` | (AnimationPlayer path) | Set endpoints via AnimationPlayer. Player rides via AnimatableBody2D. | Yes |
| Timed Spikes | `TimedSpikes.tscn` | `TimedSpikes` | `TelegraphDuration`, `ActiveDuration` | Cycles: Inactive→Telegraphing→Active. Place on clearly telegraphed routes. | Yes |
| Spike Tile | `SpikeTile.tscn` | `SpikeTile` | `Damage` | One-shot (deactivates after first hit). Don't place on arrival points. | Yes |
| Weighted Plate | `WeightedPressurePlate.tscn` | `WeightedPressurePlate` | — | Stays pressed while a body is on it (player or push block). Emits signals. | Yes |
| MultiSwitch Gate | (in code) | `MultiSwitchGate` | `Mode` (AND/OR), `RequiredCount` | All switches must be active (AND) or any one (OR). Optionally latches. | No |
| Sequence Puzzle | (in code) | `SequencePuzzle`, `SequencePressurePlate` | `ExpectedOrder` | Plates must be stepped on in correct order. Wrong order resets. | No |
| Light Mirror | (in code) | `LightMirror` | — | Pushable mirror at 45° angles. `Rotate()` cycles 4 orientations. | No |
| Light Sensor | (in code) | `LightSensor` | — | Emits signal when light beam hits it. | No |
| Fake Wall | (in code) | `FakeWall` | — | Walk-through wall, toggles collision on proximity/interaction. | No |

---

## 6. Dialog & Cutscene Authoring

### Simple dialog (inline, no resource)

Use `CutsceneTrigger` with `DialogLines` (string array). Good for one-off NPC lines:

- Set `Mode` = `OnInteract` (press E) or `OnEnter` (automatic)
- Set `Once` = `true` with a `CutsceneId` for one-time triggers (auto-sets `cutscene_<id>` flag)
- `ChoiceOptions` + `ChoiceResponses` for flavor choices (optional)

When the `Cutscene` resource path is missing/null, the trigger falls back to `DialogLines` if set; otherwise logs an error.

### DialogBranch resources (multi-choice NPCs)

Create a `DialogBranch` resource via the Inspector:

1. `Resource > New > DialogBranch`
2. Save as `.tres`
3. The Cutscene Inspector shows a node-card UI for editing nodes.
4. Each `DialogNode` has: `Id`, `SpeakerName`, `Lines`, `Responses`, `Condition`, `SetFlagsOnEnter`
5. Each `DialogResponse` has: `Text`, `NextNodeId` (empty = end dialog), `SetFlagOnSelect`, `Condition`

Use `DialogBranchTrigger` (instead of `CutsceneTrigger`) for multi-choice NPC interactions.

### CutsceneResource (cutscene sequences)

Create a `CutsceneResource` via the Inspector, then add steps using the Cutscene Inspector step-card UI.

**Step types** (StepType enum):

| Step | Purpose | Key fields |
|------|---------|------------|
| `SayDialog` | NPC speaks dialog lines | `Lines`, `SpeakerName`, `Voice` |
| `MoveNpc` | Move an NPC to a target position | `TargetNode`, `TargetPosition`, `Duration` |
| `MovePlayer` | Move the player character | `TargetPosition`, `Duration` |
| `FaceDirection` | Make a character face a direction | `TargetNode`, `Direction` |
| `PlayAnimation` | Play an animation on a node | `TargetNode`, `AnimationName` |
| `CameraMove` | Pan/shake the camera | `TargetPosition`, `Duration`, `ShakeIntensity` |
| `Wait` | Pause execution | `Duration` |
| `SetFlag` | Set a WorldFlag | `FlagName` (set to true) |
| `Fade` | Screen fade in/out | `FadeType`, `Duration` |
| `PromptChoice` | Show a choice menu during cutscene | `Choices`, `ChoiceFlags` |
| `LockPlayer` | Disable player input | — |
| `UnlockPlayer` | Re-enable player input | — |
| `Stop` | Abort the current cutscene | — |
| `DialogBranch` | Play an authored dialog tree in cutscene | `DialogBranch` resource reference |

**Conditions**: Every step (and dialog node) can have a `CutsceneCondition`:

| Condition | Effect |
|-----------|--------|
| `Always` | Step always runs (default) |
| `FlagSet` | Step runs only if WorldFlag key is true |
| `FlagNotSet` | Step runs only if WorldFlag key is false |
| `ChoiceEquals` | Step runs only if last choice index matches |

### Voice system

Each NPC can have a `DialogVoiceResource` (exported on dialog steps):
- **Procedural fallback**: 80ms sine blip at 440Hz (`BlipDuration = 0.08f`). Configure pitch, volume, variance.
- **.ogg clips**: Override with actual voice clips per phonetic hit.
- Max 16 concurrent one-shot `AudioStreamPlayer` instances.

---

## 7. Combat Authoring

### Arena structure

```
Node2D (CombatArena.cs)
├── Camera2D (makes current on _Ready)
├── CombatHUD (HP bar + enemy name)
└── [enemy nodes]
```

Each arena:
1. Sets `PlayerSpawnPosition` (exported)
2. Sets `EnemiesRemaining` to match enemy count
3. Enemies call `arena.OnEnemyDefeated()` on death; when counter hits zero, `BattleWon` fires
4. Player death fires `BattleLost` → "You collapsed..." → reload save

### Creating a combat encounter

```csharp
CombatController.Instance.EnterCombat("res://path/to/arena.tscn", spawnPosition);
```

- Call from a trigger (Area2D) or scripted event
- Re-entry guard: calls while already in combat are discarded (preserves return position)
- **No pre-combat save**: death reloads from last save point (Undertale-style)

### Enemy state machine

Every enemy follows: `Idle → Telegraph → Attack → Cooldown → Idle`

- **Idle**: waiting, basic movement
- **Telegraph**: visual warning (flash, scale, color change)
- **Attack**: bullet pattern / contact damage
- **Cooldown**: recovery period after attack

### Bullets

- `RedBullet.tscn`, collision on Bullet layer (layer 4)
- PlayerHitbox layer (8) detects bullet collisions → damage

### Parry (J key)

- `ParryComponent` on enemies: proximity-based parry detection
- `ParryRadius` and `ParryDamage` scale from equipped items via `Equipment` stat bonuses
- Success: reflects bullets + deals damage
- Miss: cooldown (ring flashes red)
- `UpdateStats(radiusBoost, damageBoost)` called on equipment change

### HealthComponent

```
TakeDamage(rawDamage):
    netDamage = Max(1, rawDamage - Defense)
    CurrentHP -= netDamage
    if CurrentHP <= 0 → emit Died

Heal(amount):
    CurrentHP = Min(MaxHP, CurrentHP + amount)

SetMaxHP(newMax, refill = false):
    MaxHP = newMax
    if refill: CurrentHP = MaxHP
    else: CurrentHP = Min(CurrentHP, MaxHP)

Revive(hpPercent = 50):
    CurrentHP = Max(1, MaxHP * hpPercent / 100)
```

### Battle flow

1. `EnterCombat(arenaPath, spawn)` — saves overworld position, loads arena
2. Arena `_Ready` — spawns player at `PlayerSpawnPosition`, hooks `Player.HealthComponent.Died`
3. Enemies defeated → `OnEnemyDefeated` decrements counter → counter=0 → `BattleWon`
4. `CombatController.OnBattleWon` — unhooks arena, returns to overworld via `LoadLevel(returnPath, returnPosition)`
5. `CombatController.OnBattleLost` — shows "You collapsed..." dialog, calls `SaveManager.LoadGame()`

---

## 8. Quest Authoring

### QuestDefinition resource

Create via Inspector: `Resource > New > QuestDefinition`

| Field | Purpose |
|-------|---------|
| `Id` | Unique quest identifier |
| `Title` | Display name |
| `Description` | Quest log text |
| `StartFlag` | WorldFlag that gates activation. Quest is active while StartFlag is set. |
| `Objectives` | Ordered array of `QuestObjective` |

### QuestObjective resource

| Field | Purpose |
|-------|---------|
| `Id` | Unique objective identifier (within quest) |
| `Description` | Objective text shown in quest log |
| `CompletionFlag` | WorldFlag set when objective is complete |

### QuestManager

Autoload singleton. WorldFlags-driven:

- `GetQuest(questId)` — returns QuestDefinition by Id
- `GetStatus(quest)` — returns Locked/Active/Completed based on StartFlag and objective flags
- `GetCurrentObjective(quest)` — first incomplete objective (by order)
- `PinQuest(questId)` / `UnpinQuest()` — pin one quest for HUD display
- Pinned quest stored in `WorldFlags["quest_pinned_id"]`; unpinned = `"__unpinned__"` sentinel

---

## 9. Item & Equipment System

### ItemDatabase

Static registry in `ItemDatabase.cs`. All items defined in code — no `.tres` files for items.

```csharp
ItemDatabase.All — Dictionary<string, Item>
ItemDatabase.Get(id) — lookup by id
```

To add a new item: add an entry to the `All` dictionary with a unique `Id`.

### Item resource fields

| Field | Purpose |
|-------|---------|
| `Id` | Unique identifier |
| `DisplayName` | Shown in UI |
| `Description` | Tooltip/description text |
| `Category` | Key / Consumable / Equipment |
| `Icon` | Inventory icon texture |
| `Icon` | Inventory icon texture |
| (Equipment) | `MaxHP`, `Defense`, `Attack`, `Speed`, `ParryRadius`, `ParryDamage` |

### PickupItem

Walking-over pickup: `body_entered` (Player layer) → add to Inventory, optionally show dialog, set flags, queue free.

### ConditionalItem

Only visible/collidable when a `RequiredFlag` WorldFlag is set.

### Equipment autoload

- 3 slots: Weapon, Armor, Accessory
- `Equip(item)` / `Unequip(slot)` — applies/reverses stat changes
- **Wired stats**: MaxHP, Defense, ParryRadius, ParryDamage
- **Unused (per DESIGN.md)**: Attack, Speed — computed but not wired to player
- `GetEquipped(slot)` → Item or null
- `PreviewDeltas(item)` → stat change preview string

### Inventory

- Dictionary of `id → count` (key items and equipment always count=1; consumables stack)
- `Add(id, count)` / `Remove(id, count)` / `Has(id)` / `GetCount(id)` / `GetByCategory(category)`

---

## 10. Save System Architecture

### ISavable interface

```csharp
public interface ISavable
{
    string SaveKey { get; }          // Unique key ("player", "inventory", etc.)
    Dictionary<string, Variant> Serialize();
    void Deserialize(Dictionary<string, Variant> data);
    int GetLoadPriority();           // Higher = loads first. Default: 0
}
```

### Load priorities

| System | Priority | Load order |
|--------|----------|------------|
| Player | 10 | First — triggers level scene switch |
| Equipment | 5 | After player, before inventory |
| Inventory | 0 | After systems that depend on items |
| WorldFlags | 0 | Same as inventory |

### Save flow

1. `SavePoint` interact → `SaveManager.SaveGame()`
2. Iterates nodes in "persist" group that implement ISavable, collects `SaveKey → serialized Dictionary` in `SaveFile.ComponentData`
3. `ResourceSaver.Save(saveFile, "user://savegame.tres")`
4. **Death**: `SaveManager.LoadGame()` — reloads from last save (no pre-combat save)

### Load flow

1. `LoadGame()` → returns false if no save exists (MainMenu Continue checks `HasSave()`)
2. Loads `SaveFile` resource from disk; if it's from an old/corrupt format, deletes it and returns false
3. Player (priority 10) loads first, triggers level scene switch
4. Equipment (5), Inventory (0), WorldFlags (0) load after in priority order
5. Each ISavable deserializes from `saveFile.ComponentData[saveKey]` — skipped silently if key is missing

---

## 11. Autoload Singleton Reference

| Singleton | Class | Node Type | Role | ISavable |
|-----------|-------|-----------|------|----------|
| `GameController` | `Node` | Scene | Level loading/unloading, tilemap bounds → camera | No |
| `WorldFlags` | `Node` | Scene | Dictionary-based world state, dialog branching, quest progression | Yes |
| `DialogManager` | `Node2D` | Scene | NPC dialog lines + DialogBubble + choice prompts | No |
| `AudioManager` | `Node` | Scene | Music cross-fade (2-player pool), SFX, ambience | No |
| `Player` | `CharacterBody2D` | Scene | WASD movement, dash, save/load, combat | Yes |
| `FadeTransition` | `CanvasLayer` | Scene | Screen fade in/out between levels | No |
| `CutsceneController` | `Node` | Script | Resource-driven cutscene player | No |
| `DebugOverlay` | `Node` | Script | Backtick-toggle debug overlay | No |
| `SaveManager` | `Node` | Scene | Save/Load via SaveFile + ResourceSaver → `user://savegame.tres` | No |
| `Inventory` | `Node` | Script | Item stacks by id | Yes |
| `Equipment` | `Node` | Script | Equip/unequip Weapon/Armor/Accessory, stat application | Yes |
| `CombatController` | `Node` | Script | EnterCombat scene swap, win/lose flow, re-entry guard | No |
| `KeybindManager` | `Node` | Script | Rebind/reset/save/load input bindings | No |
| `QuestManager` | `Node` | Scene | WorldFlags-driven quest tracking, pinning | No |
| `FactoryOpeningFlow` | `Node` | Script | Tutorial flow coordinator (Factory→Eggs Isle) | No |

---

## 12. Logging & Debugging

See `LOGGING.md` for full reference. Quick facts:

- **Log file**: `user://logs/eggbert_YYYY-MM-DD.log` (auto-rotates, keeps 5)
- **Env**: `EGGBERT_LOG_LEVEL=debug` for verbose traces
- **Engine errors**: Captured via `GameLogBridge` → same log file
- **Init**: `GameLogger.InitializeFromEnv()` in `boot/GameInit.cs`

### Debug keys

| Key | Action |
|-----|--------|
| Backtick (`) | Toggle DebugOverlay (WorldFlags, player state) |
| Ctrl+Shift+J | Start combat with default enemy |
| F8 | (assigned in project settings) |
| Tab | Toggle dialog log during conversations |

### Headless verification scripts

C# verifier scripts in `tests/` can be run headless (C# only — no GDScript outside `addons/`):

```bash
godot --headless --path . --script res://tests/<Name>.cs
```

Current test scripts: `VerifyFactoryExpansion.cs` (Factory 5-room route — the structural contract for the shipped tutorial), `VerifyQuestAutoPin.cs`, `VerifyDialogBranch.cs`, `VerifyCombatOnceFlag.cs`, `WarpFixVerifier.cs`. New levels should ship with a matching `Verify<Level>.cs` following the `VerifyFactoryExpansion.cs` pattern.

---

## 13. MCP Tool Reference

The `godot-mcp` server exposes these commands via the AI assistant (configured in `.omp/mcp.json`):

| Command | Description |
|---------|-------------|
| `get_godot_version` | Return the running Godot version |
| `get_project_info` | Return project configuration settings |
| `get_scene_tree` | List the current scene tree |
| `create_scene` | Create a new scene of a given type (2D, 3D, etc.) |
| `add_node` | Add a node as a child of an existing node |
| `save_scene` | Save the currently open scene |
| `run_project` | Start the project in play mode |
| `stop_project` | Stop the running project |
| `get_debug_output` | Capture Engine debug output |
| `load_sprite` | Load a sprite texture |
| `call_method` | Call a method on a node |
| `set_property` | Set a property on a node |
| `get_node` | Get information about a node |
| `list_scenes` | List all scenes in the project |
| `search_files` | Search for files in the project |
| `execute_code` | Execute arbitrary GDScript in the editor context |

---

## 14. Physics Layers Cheatsheet

From `CollisionConfig.cs` and `project.godot` > `layer_names`:

| Layer # | Name | Constant | Typical use |
|---------|------|----------|-------------|
| 1 | Player | `PlayerLayer` (1) | Player body, camera |
| 2 | Walls | `WallsLayer` (2) | Tilemap collision, arena walls |
| 3 | NPCs | `NPCLayer` (4) | NPC bodies, triggers |
| 4 | Bullets | `BulletLayer` (8) | Enemy bullet projectiles |
| 5 | Interactables | `InteractableLayer` (16) | Pickup items, switches, doors |
| 6 | Enemies | `EnemyLayer` (32) | Enemy bodies |
| 7 | TriggerAreas | `TriggerAreaLayer` (64) | Level transitions, zone triggers |
| 8 | PlayerHitbox | `PlayerHitboxLayer` (128) | Player damage detection |
| 9 | EnemyHitbox | `EnemyHitboxLayer` (256) | Enemy damage detection (parry target) |
| 10 | Items | `ItemLayer` (512) | World item pickups |

Mask constants:
- `PlayerBulletMask` = PlayerLayer | WallsLayer (bullet collision targets)

---

## 15. Flag Naming Conventions

WorldFlags keys follow these patterns:

| Pattern | Example | When to use |
|---------|---------|-------------|
| `cutscene_<id>` | `cutscene_arrest` | Auto-set by `CutsceneTrigger` with `Once=true`. One-shot dedup. |
| `gossip_<A>_about_<B>` | `gossip_herald_about_smith` | Gossip chain progression between NPCs |
| `met_<npc>` | `met_jamitor` | Track first meeting with an NPC |
| `has_<item>` | `has_cell_key` | Track key item acquisition |
| `beat_<enemy>` | `beat_oatmeal` | Combat victory flag |
| `spared_<enemy>` | `spared_oatmeal` | Mercy route flag (choice before combat) |
| `fought_<enemy>` | `fought_oatmeal` | Fight route flag |
| `quest_pinned_id` | — | Reserved by QuestManager for pinned quest id |
| `__unpinned__` | — | Sentinel value for "no pinned quest" |
| `visited_<level>` | `visited_courtyard` | Track level entry for conditional dialog |
| `read_<object>` | `read_warehouse_sign` | Track one-shot readable objects |
| `called_home` | — | PhoneBooth call completion |

---

## 16. Input Reference

| Action | Default Key | Description |
|--------|-------------|-------------|
| `player_up` | W | Move / menu up |
| `player_down` | S | Move / menu down |
| `player_left` | A | Move / menu left |
| `player_right` | D | Move / menu right |
| `interact` | E | Interact / confirm |
| `menu_pause` | Esc | Open/close pause menu |
| `player_sprint` | Shift | Sprint (overworld) |
| `dash` | Space | Dash (overworld) |
| `check` | F | Check/tattle action |
| `combat_parry` | J | Parry (combat) |
| `debug_toggle` | Grave (`) | Toggle debug overlay |
| `debug_start_combat` | Ctrl+Shift+J | Start combat (debug) |
| `debug_start_combat_eggroller` | F8 | Start eggroller combat (debug) |
