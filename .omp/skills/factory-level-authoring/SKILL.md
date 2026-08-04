---
name: factory-level-authoring
description: Build and verify repeatable Eggbert levels from the Factory tutorial pattern.
---

This skill is the mandatory execution contract for any agent authoring a new level or materially changing a level's progression. It must be read and followed before any scene editing, C# authoring, or level-graph wiring begins.

## Required prerequisites

Before authoring, read these resources in order:

1. `docs/godot-editor-guide.md` — editor setup, plugins, resource rules, component reference
2. `docs/level-authoring.md` — detailed component recipes, verification playbook, Oh My Pi handoff template
3. `docs/factory-opening.md` — the canonical three-room Factory reference (OpeningZone → SortingFloor → LoadingBay)
4. `LOGGING.md` — debug protocol, log tags, and failure-triage recipes
5. `skill://godot-authoring` — editor-first serialization rules and `.tres` constraints
6. `skill://godot-csharp-patterns` — C# exports, signals, scene loading, and collision-layer patterns

Then inspect the nearest existing level and every component script/scene the level will instantiate.

## 1. Question gate before implementation

Inspect issues, existing scenes, `STORY.md`, `ItemDatabase`, and WorldFlags first. Then use the Oh My Pi `ask` tool in one batch if any load-bearing choice is absent:

- Player objective / exit condition
- Story beat / dialog facts
- Rewards or item effects
- Required progression flags
- Puzzle solution / failure consequence
- Combat / mercy outcome
- Difficulty
- Ambient / visual identity
- Optional-route value

Offer 2–4 concrete choices with a recommended default. Use the answer in the authored data. Never invent narrative, item, or tuning facts. Do not ask questions already answered by the inspected code, docs, or issues.

If all design decisions are resolved after research, record "no questions needed" and proceed. If any remain unresolved, block content authoring on the question tool rather than selecting story or balance values yourself.

## 2. Plan the level as a graph

Write a compact room/beat table before opening the editor. Each row must include:

| Column | Meaning |
|--------|---------|
| Arrival | Spawn point or entry transition |
| Mandatory interactions | NPCs, prompts, required dialog |
| Gate flag / item | Flag or item that must be set/obtained to proceed |
| Puzzle input and success output | What the player does and what changes |
| Save point | Nearby `SavePoint` placement |
| Optional content | Hazards, encounters, or rewards off the mandatory path |
| Outgoing source transition | The `LevelTransition` node that exits this room |
| Target scene | The `res://` path of the destination level |
| Target direct-root node | The `TargetTransitionName` in the destination |
| Return route | How the player gets back (if any) |

Copy Factory's reusable ordering without copying Factory-specific narrative: arrival → interaction/tutorial → physical gate/puzzle → one-shot story event → gated or automatic handoff. Stable node names and WorldFlag keys are APIs — treat them as such.

## 3. Choose the authoring surface

Use the Godot editor and Level Assembly (after enabling the plugin when absent) for scene nodes, TileMap painting, collision, animations, and all nested resources. Use Inspector-created `.tres` for `CutsceneResource` and `DialogBranch` resources; use inline exported `DialogLines` only for simple one-off dialog. Use C# only for missing reusable runtime behavior after confirming no existing component suffices. Never hand-edit `tile_map_data`, atlas subresources, generated UIDs, or nested `.tres` data.

## 4. Compose and configure

Require the following baseline for every level:

- `Node2D` root with `BaseLevel.cs`
- A direct-root scripted tilemap at `(0, 0)` with painted non-empty bounds
- Direct-root gameplay components (not nested under the tilemap)
- Descriptive `LevelName` and audio exports
- Open-floor arrivals with no mandatory hazards on the required path
- A nearby `SavePoint`
- Exact Inspector wiring for each selected component

### Component recipes

#### Transition
- `Level` is a `res://` scene path
- `TargetTransitionName` is an exact direct-root destination node
- Configure `Side`, `Size`, and only an already-defined `RequiredFlag`

#### Save / warp
- Set `LocationName` on `SavePoint`
- A placed `WarpPoint.WarpId` requires a matching `WarpDatabase.All` entry and the documented progression unlock path

#### Puzzle
- Configure NodePaths only after all involved nodes exist
- `PushBlock` needs destination clearance; test that the crate cannot get stuck
- Plate/switch `TargetDoorPath` resolves to the intended `Door` node
- `TeleportPad` pairs configure both directions
- `SequencePuzzle` plates have unique ordered indices, valid controller paths, a target door, and a tested wrong-order reset

#### Dialog / cutscene
- `CutsceneTrigger` uses `OnInteract` for player-initiated dialog and `OnEnter` for automatic one-shot events
- `Once + CutsceneId` means a persistent `cutscene_<id>` lockout
- `ChoiceOptions` and `ChoiceResponses` have matching indexes
- `DialogBranchTrigger` must receive a real `DialogBranch` resource
- Staged events use Inspector-authored `CutsceneResource` steps; do not hand-author nested `.tres`

#### Rewards / combat
- Only use item IDs already in `ItemDatabase.All` unless a separately approved item design is implemented first
- Use an existing arena/encounter component or a new `CombatArena` subclass with a defined combat contract
- No pre-combat save and no mandatory encounter without a known return/checkpoint path

## 5. Use Oh My Pi deliberately

- Use `glob`/`grep`/`read` to map existing patterns
- Use `lsp` for C# symbols and references
- Use `task` only for independent read-only audits
- Use `ask` for the question gate
- Use `todo` for multi-step levels
- Use Godot MCP editor operations for scene construction and runtime inspection
- Use `inspect_image` for screenshots/assets
- Use `hub`-managed project processes only for interactive runtime

The Godot MCP surface exposes operations such as `get_project_info`, `get_scene_tree`, `create_scene`, `add_node`, `save_scene`, `run_project`, `stop_project`, `get_debug_output`, `load_sprite`, `call_method`, `set_property`, `get_node`, `list_scenes`, `search_files`, and `execute_code`. Direct agents to use the operations exposed by their current MCP surface rather than inventing aliases. Project docs may contain prefixed or unprefixed naming drift; the documented operation purpose is the source of truth.

## 6. Verify in layers

### Static verification
1. Use a headless verifier to load and instantiate the level and assert root/type, direct-root arrivals, transition targets, component NodePaths, and referenced item IDs.

### In-editor traversal
2. Run the level in Godot and exercise both directions of every transition.
3. Exercise every interaction and puzzle success and failure path.
4. Test save/reload at the `SavePoint`.
5. Test combat return when present.

### Log inspection
6. Run with `EGGBERT_LOG_LEVEL=debug` and inspect `user://logs/eggbert_YYYY-MM-DD.log` for the relevant tags: `LevelTransition`, `CutsceneTrigger`, `PickupItem`, `WorldFlags`, `SavePoint`, and puzzle-specific tags.

### Build
7. Run `dotnet build` from the repository root.

On any runtime report or failed scene path, inspect the fresh game log before reopening code.
