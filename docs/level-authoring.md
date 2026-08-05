# Level Authoring Workflow

Use the **Level Assembly** dock in Godot to compose gameplay locations. It appears on the right after opening the project; the plugin is **disabled by default** and must be enabled through `Project > Project Settings > Plugins > Level Assembly > Enable`.

For the shipped opening's beat-by-beat layout, dialog, puzzle, arrest, and handoff contract, see [Factory Opening Authoring Guide](factory-opening.md).

## 1. Preflight — question gate

Before authoring content, resolve every load-bearing design decision. Inspect issues, existing scenes, `STORY.md`, `ItemDatabase`, and WorldFlags first. Then use the Oh My Pi `ask` tool in one batch if any decision remains unresolved:

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

## 2. Tools by task

| Task | Tool |
|------|------|
| Scene node placement and TileMap painting | Godot editor + Level Assembly dock |
| Collision, animations, and nested resources | Godot editor / Inspector |
| CutsceneResource and DialogBranch authoring | Inspector-created `.tres` via Cutscene Inspector |
| Simple one-off dialog | Inline `DialogLines` on `CutsceneTrigger` |
| C# runtime behavior | Only after confirming no existing component suffices |
| Flat scalar `.tres` | Inspector `Save As` (never hand-edit) |
| Nested `.tres` resources | Inspector only (Godot serializes sub-resources and UIDs) |
| Research existing patterns | `search_files` (grep) / `read_file` (read) — or `glob`/`grep`/`read` in Oh My Pi |
| C# symbol/reference intelligence | `lsp` (Oh My Pi) or `search_files` over `*.cs` |
| Independent read-only audits | `task` (scout agent, Oh My Pi) or `delegate_task` |
| Design questions | `ask` (Oh My Pi) or `clarify` — one batch |
| Multi-step level work | `todo` |
| Scene construction and runtime inspection | Godot MCP editor operations |
| Screenshots/assets | `inspect_image` / `vision_analyze` |
| Interactive runtime | `hub`-managed project processes or local `godot` run |

## 3. Production map contract

Every level must satisfy these baseline requirements:

| Requirement | Detail |
|-------------|--------|
| Root node | `Node2D` with `BaseLevel.cs` |
| Tilemap | Direct child of root, position `(0, 0)`, scripted `LevelTileMapLayer` |
| Used rect | Painted non-empty bounds; minimum `1536×1024` world pixels |
| Camera | `LevelTileMapLayer` derives camera limits from the used rect |
| Components | Direct-root children of the level node, not nested under the tilemap |
| Naming | Stable PascalCase node names; names referenced by other systems are API |
| Collision layers | Use constants from `CollisionConfig` (see `components/core/CollisionConfig.cs`) |
| Arrivals | Open floor at spawn, no mandatory hazards on the required path |
| Save point | `SavePoint` near every arrival and hub gate |
| Transitions | `Level` uses `res://` scene path; `TargetTransitionName` is a direct-root `LevelTransition` in the destination scene |

## 4. Component recipe table

| Component | Scene / Class | Key fields / paths to set | Outcome to observe | Authoring trap |
|-----------|---------------|---------------------------|--------------------|----------------|
| Level Transition | `LevelTransition.tscn` / `LevelTransition` | `Level` (res:// path), `TargetTransitionName`, `Side`, `Size`, `RequiredFlag` | Player exits at correct offset in destination | `TargetTransitionName` must be a direct-root child of the destination scene; rename it only after updating every source transition |
| Save Point | `SavePoint.tscn` / `SavePoint` | `LocationName` | Interact heals + saves; reload restores position | Place near every arrival/hub gate |
| Warp Point | `WarpPoint.tscn` / `WarpPoint` | `WarpId` | Unlocks in `WarpDatabase`; enables overworld warp | A placed `WarpId` requires a matching `WarpDatabase.All` entry and the documented progression unlock path |
| Cutscene Trigger | `CutsceneTrigger.tscn` / `CutsceneTrigger` | `Mode` (OnInteract/OnEnter), `Once`, `CutsceneId`, `DialogLines` or `Cutscene` resource | Fires dialog or cutscene on trigger | Use `OnInteract` for player-initiated dialog; `OnEnter` for automatic one-shot events. `Once + CutsceneId` sets persistent `cutscene_<id>` flag |
| Dialog Branch Trigger | `DialogBranchTrigger` scene / `DialogBranchTrigger` | `DialogBranch` resource reference | Multi-choice NPC interaction | Must receive a real `DialogBranch` resource; `ChoiceOptions` and `ChoiceResponses` must have matching indexes |
| Pickup Item | `PickupItem.tscn` / `PickupItem` | `ItemId` (must exist in `ItemDatabase.All`) | Item added to inventory, flag set, dialog shown | Only use item IDs already in `ItemDatabase.All` unless a separately approved item design is implemented first |
| Conditional Item | `ConditionalItem.tscn` / `ConditionalItem` | `ItemId`, visibility condition | Item appears/hides based on WorldFlag | Same item-ID rule as `PickupItem` |
| Door | `Door.tscn` / `Door` | `StartOpen` | Toggles open/closed; emits `Opened`/`Closed` | Use for simple open/close gates |
| Key Door | `KeyDoor.tscn` / `KeyDoor` | `RequiredFlag`, `LockedMessage` | Gated by WorldFlag; shows message when missing | Do not invent progression flags without a design decision |
| Floor Switch | `FloorSwitch.tscn` / `FloorSwitch` | `TargetDoorPath` (set after both nodes exist) | Opens/closes target door when body presses | Set `TargetDoorPath` in the Inspector after both switch and door are placed |
| Push Block | `PushBlock.tscn` / `PushBlock` | `DirectionalMode` (optional) | Player pushes by walking into it | Ensure destination clearance; a stuck crate is an invisible hard lock |
| Weighted Pressure Plate | `WeightedPressurePlate.tscn` / `WeightedPressurePlate` | `TargetDoorPath` | Stays pressed while a body is on it | Pairs with `PushBlock` or `Door` for stateful gates |
| Teleport Pad | `TeleportPad.tscn` / `TeleportPad` | `TargetPadPath` (both directions) | Player teleports to paired pad | Place both pads first, then set each other's `TargetPadPath` |
| Timed Door | `TimedDoor.tscn` / `TimedDoor` | `OpenDuration` | Auto-closes after duration | Use for time-gated passages; ensure escape route exists |
| Sequence Puzzle | `SequencePuzzle.cs` / `SequencePressurePlate` | `ExpectedOrder` on controller, unique ordered indices on plates | Plates must be stepped on in correct order | Wrong order resets; test the reset path before committing |
| Fake Wall | `FakeWall.cs` | — | Walk-through wall toggles collision on proximity | Use for secret routes; not a required-path gate |
| Combat Arena | `CombatArena.tscn` / `CombatArena` | `PlayerSpawnPosition`, `EnemiesRemaining` | Enter via `CombatController.Instance.EnterCombat()` | No pre-combat save; death reloads from last save point |

### Reference levels

The only shipped levels are the tutorial chain: the five Factory rooms (`levels/factory/maps/`,
see `docs/factory-opening.md`) and the Eggs Isle intake (`levels/eggsile/maps/EggsIsle.tscn`, see
`docs/eggsile-intake.md`). These are the canonical references for level authoring — the sandbox,
sample, and story-chain placeholder zones were removed in the 2026-08 cleanup (#174) because
agents were misusing them as reference content. Do not reintroduce test levels under `levels/`;
use `tests/` generators + verifiers for throwaway scenes.

## 5. Dialog / prompt / cutscene recipe

### One-off inline dialog

Use `CutsceneTrigger` with `DialogLines` (string array). Good for simple NPC lines:

- Set `Mode` = `OnInteract` (press E) or `OnEnter` (automatic)
- Set `Once` = `true` with a `CutsceneId` for one-time triggers (auto-sets `cutscene_<id>` flag)
- `ChoiceOptions` + `ChoiceResponses` for flavor choices (optional)

When the `Cutscene` resource path is missing/null, the trigger falls back to `DialogLines` if set; otherwise logs an error.

### Branching DialogBranch

Create a `DialogBranch` resource via the Inspector:

1. `Resource > New > DialogBranch`
2. Save as `.tres`
3. The Cutscene Inspector shows a node-card UI for editing nodes
4. Each `DialogNode` has: `Id`, `SpeakerName`, `Lines`, `Responses`, `Condition`, `SetFlagsOnEnter`
5. Each `DialogResponse` has: `Text`, `NextNodeId` (empty = end dialog), `SetFlagOnSelect`, `Condition`

Use `DialogBranchTrigger` (instead of `CutsceneTrigger`) for multi-choice NPC interactions.

### Staged CutsceneResource

Create a `CutsceneResource` via the Inspector, then add steps using the Cutscene Inspector step-card UI. Do not hand-author nested `.tres` resources — Godot must serialize their sub-resources and IDs.

Available step types: `LockPlayer`, `MoveNpc`, `MovePlayer`, `FaceDirection`, `CameraMove`, `SayDialog`, `Wait`, `SetFlag`, `Fade`, `PromptChoice`, `Stop`, `DialogBranch`.

### Flag naming patterns

- Fact flags (e.g., `met_jamitor`, `tutorial_clocked_out`): set by NPCs/interactions; not substitutes for collision gates
- Gate flags (e.g., `tutorial_crate_gate_open`, `arrested`): control transition/door availability
- Cutscene lockout flags: auto-generated as `cutscene_<id>` when `Once = true` with a `CutsceneId`
- Progression flags (e.g., `eggsile_area1`): unlock warp points or enable transitions

## 6. Verification and debug playbook

### Static checks
1. Open the scene in Godot; verify no missing resource warnings.
2. Inspect the TileMap used rect and camera bounds.
3. Confirm every `LevelTransition` has a loadable `Level` and a `TargetTransitionName` that resolves to a direct-root child of the destination level.
4. Confirm every `WarpPoint.WarpId` has a matching `WarpDatabase.All` entry.
5. Confirm every referenced item ID exists in `ItemDatabase.All`.
6. Confirm every `NodePath` export resolves to an existing node.
7. Write a C# verifier in `tests/` (`SceneTree` subclass) that instantiates the scene headless and asserts root/type, transitions, NodePaths, flags, and item IDs — the Factory route is the reference: `tests/VerifyFactoryExpansion.cs`. Run it with `godot --headless --path . --script res://tests/<Name>.cs`.

### In-editor traversal
1. Run the scene from the editor. Confirm `BaseLevel` loads with no missing-resource errors.
2. Traverse each arrival to every transition, save point, puzzle control, NPC, encounter, and exit.
3. Follow every transition both directions. Confirm the target scene loads and places the player outside the destination trigger.

### Save/reload
4. Save at the `SavePoint`, reload, and confirm the player returns to the save location with the correct flag state.

### Flag and item checks
5. Verify that gate flags block early traversal and that one-shot cutscene flags persist across reloads.

### Build
6. Run `dotnet build` from the repository root.

### Log inspection
7. Run with `EGGBERT_LOG_LEVEL=debug` and inspect `user://logs/eggbert_YYYY-MM-DD.log` for the relevant tags.

Key log tags by system:

| Tag | Use when |
|-----|----------|
| `LevelTransition` | Verify transition activation and target level loading |
| `CutsceneTrigger` | Verify cutscene/dialog firing and Once lifecycle |
| `WorldFlags` | Verify flag set/clear timing and values |
| `Door` | Verify door open/close state transitions |
| `FloorSwitch` | Verify switch press/release and door linkage |
| `SavePoint` | Verify save activation and reload restoration |
| `WarpDatabase` | Verify warp unlock state |
| `PickupItem` | Verify item pickup and flag/set-dialog flow |
| `Combat` | Verify combat enter/exit and win/lose flow |

### Failure triage
On any runtime report or failed scene path, inspect the fresh game log before reopening code. The log source file + line numbers (appended by the compiler) let you trace exactly where a log was emitted.

## 7. Oh My Pi handoff template

When delegating level authoring to a subagent, provide the following information so the subagent can follow the `factory-level-authoring` skill without asking unnecessary questions:

- **Goal**: What the level must accomplish (e.g., "a two-room area with a timed gate puzzle")
- **Already-set design facts**: Flags already defined, items already approved, existing scenes the level will reference
- **Allowed flags/items**: Which WorldFlags and ItemDatabase IDs are in scope
- **Source/destination scenes**: The `res://` paths of adjacent levels and their `TargetTransitionName` nodes
- **Acceptance checks**: Which verification steps matter most (e.g., "both transition directions must work, arrest must persist on reload")

The subagent asks only unresolved design questions through `ask`. It does not invent narrative, item, or tuning facts.
