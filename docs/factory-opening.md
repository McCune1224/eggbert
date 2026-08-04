# Factory Opening Authoring Guide

The factory is the game's opening tutorial. Its job is to teach movement and interaction without treating the player as a beginner, then establish the inciting incident: **Eggbert is falsely identified as an egg-costumed murderer and arrested by Officer Bacon.** The victim, weapon, and actual perpetrator remain off-screen.

> **Factory-specific content.** The beat sequence, flag names, and handoff logic below are the reference story implementation for the Factory opening. Reusable structure (level root/tilemaps, flag-gated room graph, checkpoint, interaction NPC, physical/timed puzzle, staged cutscene, direct-root transitions) applies to all levels, but the clock-out premise, Jamitor's input lesson, exact flags/dialogue, Officer Bacon arrest, and automatic Eggs Isle transfer are Factory-specific.

## Reference scene graph

The Factory opening is a three-room sequence: `OpeningZone.tscn` → `SortingFloor.tscn` → `LoadingBay.tscn`. Each room is a `BaseLevel` scene with a direct-root tilemap and direct-root gameplay components.

### Room 1: OpeningZone

| Beat | Scene content | Required result |
|------|---------------|-----------------|
| Arrival | Factory floor, `Factory Gate` location banner | Player can move immediately with WASD. |
| Clock-out | `TimeClock` at `(160, -64)` | `E` starts dialog; the clock sets `tutorial_clocked_out`. |
| Sorting Floor entrance | `SortingFloorEntrance` (direct-root `LevelTransition`) | Requires `tutorial_clocked_out`. |

### Room 2: SortingFloor

| Beat | Scene content | Required result |
|------|---------------|-----------------|
| Arrival | Open floor, `ClockOutReturn` transition back to OpeningZone | Player can return to the clock-out room. |
| Jamitor | `FactoryJamitor` at `(-384, 0)` runs `FactoryJamitorTutorial.tres` | Dialog teaches WASD, Shift sprint, Space dash, and E interaction. Sets `met_jamitor`. |
| Crate puzzle | `FactoryCrate` pushed onto `FactoryPressurePlate`, which targets `CrateGate` and sets `tutorial_crate_gate_open` | The gate opens; the player learns physical-object interaction. |
| Loading Bay entrance | `LoadingBayEntrance` (direct-root `LevelTransition`) | Requires `met_jamitor`. |

### Room 3: LoadingBay

| Beat | Scene content | Required result |
|------|---------------|-----------------|
| Arrival | Open floor, `SortingFloorReturn` transition back to SortingFloor | Player can return to the sorting floor. |
| Timed switches | West `LoadingTimedSwitchWest` and east `LoadingTimedSwitchEast` | Both open `LoadingTimedGate` for five seconds. |
| Arrest | `ArrestCutscene` (direct-root `CutsceneTrigger`), `OnEnter`, once-only | Uses `OfficerBaconArrest.tres` (a `CutsceneResource`). Sets `arrested`. |
| Handoff | `EggsileTransition` (direct-root `LevelTransition`) | Requires `arrested`. Targets `res://levels/eggsile/maps/area1.tscn` / direct-root `HubArrival`. |

## Progression keys

| Flag | Set by | Gates |
|------|--------|-------|
| `tutorial_clocked_out` | `TimeClock` cutscene | `SortingFloorEntrance` transition |
| `met_jamitor` | `FactoryJamitor` dialog | `LoadingBayEntrance` transition |
| `tutorial_crate_gate_open` | `FactoryPressurePlate` when `FactoryCrate` is on it | `CrateGate` door |
| `arrested` | `ArrestCutscene` (`OfficerBaconArrest.tres`) | `EggsileTransition` transition |
| `cutscene_arrest` | Auto-set by `CutsceneTrigger` when `Once = true` and `CutsceneId = "arrest"` | Prevents arrest replay on reload |
| `warp_eggsile_area1` | `FactoryOpeningFlow` after arrest cutscene completes | Enables Eggs Isle warp from Overworld |

## Distinguish direct scene gates from post-cutscene handoff

- **Direct scene gates** (`RequiredFlag` on `LevelTransition`): `SortingFloorEntrance` requires `tutorial_clocked_out`; `LoadingBayEntrance` requires `met_jamitor`; `EggsileTransition` requires `arrested`. These block traversal at the transition collision level.
- **FactoryOpeningFlow's post-cutscene warp unlock/handoff**: After the arrest cutscene finishes, `FactoryOpeningFlow` unlocks `eggsile_area1` in `WarpDatabase` and calls `GameController.Instance.LoadLevel(EggsileScenePath, "HubArrival")`. This is the automatic Eggs Isle transfer, not a voluntary exit.

## Optional content

- Factory Zone1 hazards and combat are outside the mandatory opening path.
- The Factory opening has no pickup requirement.
- The `factory_gate` `WarpPoint` is not an opening requirement; it exists for editor traversal.

## Authoring constraints

- The `ArrestCutscene` node name and `OfficerBaconArrest.tres` resource path are stable unless `FactoryOpeningFlow.cs` is updated.
- `FactoryOpeningFlow.cs` owns the mandatory post-arrest handoff. Keep the `ArrestCutscene`, `EggsileTransition`, and `HubArrival` node names stable.
- The scene's inline arrest lines are intentionally replaced by `FactoryOpeningFlow` so the narrative stays synchronized with the transfer.

## Verification assertions

When verifying the Factory reference, assert these observable per-room behaviors:

1. **New-game reset/load**: Starting a new game loads `OpeningZone.tscn`, not the Overworld.
2. **Pre/post-gate transition behavior**: `SortingFloorEntrance` is blocked before `tutorial_clocked_out`; `LoadingBayEntrance` is blocked before `met_jamitor`; `EggsileTransition` is blocked before `arrested`.
3. **Jamitor dialogue/flag**: Talking to Jamitor sets `met_jamitor` and teaches the four controls.
4. **Crate-to-plate gate**: Pushing `FactoryCrate` onto `FactoryPressurePlate` opens `CrateGate` and sets `tutorial_crate_gate_open`.
5. **Both timed switches**: Either `LoadingTimedSwitchWest` or `LoadingTimedSwitchEast` opens `LoadingTimedGate` for five seconds.
6. **One-shot arrest persistence**: Entering the arrest trigger once sets `arrested` and `cutscene_arrest`; reloading a save preserves both.
7. **Post-arrest warp unlock and automatic transfer**: After the arrest cutscene, `eggsile_area1` is unlocked and the player is transferred to Eggs Isle at `HubArrival`.
8. **No save bypass**: A production save checkpoint must not permit a new player to bypass the arrest state.

For generic map conventions, component placement, and transition graph checks, see [Level Authoring Workflow](level-authoring.md).
