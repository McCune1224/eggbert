# Factory Opening Authoring Guide

The factory is the game's opening tutorial. Its job is to teach movement, interaction, and the two core puzzle verbs (push-block + sequence switches) without treating the player as a beginner, then establish the inciting incident: **Eggbert is falsely identified as an egg-costumed murderer and arrested by Officer Bacon.** The victim, weapon, and actual perpetrator remain off-screen.

> **Factory-specific content.** The beat sequence, flag names, and handoff logic below are the reference story implementation for the Factory opening. Reusable structure (level root/tilemaps, flag-gated room graph, checkpoint, interaction NPC, physical/timed puzzle, staged cutscene, direct-root transitions) applies to all levels, but the clock-out premise, Jamitor's input lesson, shutdown-shift premise, exact flags/dialogue, Officer Bacon arrest, and automatic Eggs Isle transfer are Factory-specific.

## Reference scene graph

The Factory opening is a five-room sequence: `OpeningZone.tscn` → `SortingFloor.tscn` → `AssemblyLine.tscn` → `ControlRoom.tscn` → `LoadingBay.tscn`, then an automatic handoff to Eggs Isle. Each room is a `BaseLevel` scene with a direct-root tilemap and direct-root gameplay components. The full route is ~10 minutes.

```
OpeningZone → SortingFloor → AssemblyLine → ControlRoom → LoadingBay ──(arrest)──→ Eggs Isle
   (clock-out)   (Jamitor +    (conveyors +   (sequence       (timed gate +
                  crate puzzle)  checklist)     puzzle)         arrest)
```

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
| Assembly Line entrance | `AssemblyLineEntrance` (direct-root `LevelTransition`) | Requires `tutorial_crate_gate_open`. |

### Room 3: AssemblyLine

| Beat | Scene content | Required result |
|------|---------------|-----------------|
| Arrival | Open floor, `SortingFloorArrival` transition back to SortingFloor | Player can return to the sorting floor. |
| Conveyor lesson | 12 `Conveyor01..12` tiles pushing west at 80 px/s (`ConveyorDirection = (-1, 0)`) | Player learns conveyor traversal (walk against or ride with the belt). |
| Shutdown checklist | `ShutdownChecklist` (direct-root `CutsceneTrigger`), `OnInteract`, once-only | Dialog; sets `factory_shutdown_checklist_signed`. |
| Maintenance pads | `MaintenancePadWest` ↔ `MaintenancePadEast` (paired `TeleportPad`) | Optional shortcut; verifies pad pairs resolve both directions. |
| Control Room entrance | `AssemblyLineExit` (direct-root `LevelTransition`) | Requires `factory_shutdown_checklist_signed`. |

### Room 4: ControlRoom

| Beat | Scene content | Required result |
|------|---------------|-----------------|
| Arrival | Open floor, `AssemblyLineArrival` transition back to AssemblyLine | Player can return. |
| Sequence puzzle | `SequencePlateA/B/C` (`SequencePressurePlate`, indices 0/1/2) + `SequenceController` (`SequencePuzzleController`, `TimeWindow = 5s`, targets `InspectionDoor`) | Stepping A→B→C in order within the window opens `InspectionDoor`. |
| Inspection | `InspectionApproved` (direct-root `CutsceneTrigger`), `OnEnter`, once-only | Dialog; sets `factory_shutdown_inspection_complete`. |
| Reward | `EggdropSoupPickup` (`PickupItem`, `eggdrop_soup`) in the soup alcove | Optional consumable. |
| Loading Bay entrance | `LoadingBayEntrance` (direct-root `LevelTransition`) | Requires `factory_shutdown_inspection_complete`. |

### Room 5: LoadingBay

| Beat | Scene content | Required result |
|------|---------------|-----------------|
| Arrival | Open floor, `ControlRoomReturn` transition back to ControlRoom | Player can return. |
| Timed switches | West `LoadingTimedSwitchWest` and east `LoadingTimedSwitchEast` | Both open `LoadingTimedGate` for five seconds. |
| Arrest | `ArrestCutscene` (direct-root `CutsceneTrigger`), `OnEnter`, once-only | Uses `OfficerBaconArrest.tres` (a `CutsceneResource`). Sets `arrested`. |
| Handoff | `EggsileTransition` (direct-root `LevelTransition`) | Requires `arrested`. Targets `res://levels/eggsile/maps/EggsIsle.tscn` / direct-root `HubArrival` (then the one-shot arrival cutscene plays). |

## Progression keys

| Flag | Set by | Gates |
|------|--------|-------|
| `tutorial_clocked_out` | `TimeClock` cutscene | `SortingFloorEntrance` transition |
| `met_jamitor` | `FactoryJamitor` dialog | (used by FactoryJamitorTutorial; `LoadingBayEntrance` was re-keyed to `tutorial_crate_gate_open` in the five-room route) |
| `tutorial_crate_gate_open` | `FactoryPressurePlate` when `FactoryCrate` is on it | `CrateGate` door, `AssemblyLineEntrance` transition |
| `factory_shutdown_checklist_signed` | `ShutdownChecklist` dialog | `AssemblyLineExit` transition |
| `factory_shutdown_inspection_complete` | `InspectionApproved` trigger | `LoadingBayEntrance` transition |
| `arrested` | `ArrestCutscene` (`OfficerBaconArrest.tres`) | `EggsileTransition` transition |
| `cutscene_arrest` | Auto-set by `CutsceneTrigger` when `Once = true` and `CutsceneId = "arrest"` | Prevents arrest replay on reload |
| `warp_eggsile_area1` | `FactoryOpeningFlow` after arrest cutscene completes | Enables Eggs Isle warp from Overworld |

## Distinguish direct scene gates from post-cutscene handoff

- **Direct scene gates** (`RequiredFlag` on `LevelTransition`): each room's exit is gated by the preceding room's completion flag, in order. These block traversal at the transition collision level.
- **FactoryOpeningFlow's post-cutscene warp unlock/handoff**: After the arrest cutscene finishes, `FactoryOpeningFlow` unlocks `eggsile_area1` in `WarpDatabase` and calls `GameController.Instance.LoadLevel(EggsileScenePath, "HubArrival")`. This is the automatic Eggs Isle transfer, not a voluntary exit.

## Optional content

- Factory `Zone1.tscn` hazards and combat are outside the mandatory opening path.
- The opening has one optional pickup (`EggdropSoupPickup` in ControlRoom) and one optional shortcut (maintenance pads in AssemblyLine).
- The `factory_gate` `WarpPoint` is not an opening requirement; it exists for editor traversal.

## Authoring constraints

- The `ArrestCutscene` node name and `OfficerBaconArrest.tres` resource path are stable unless `FactoryOpeningFlow.cs` is updated.
- `FactoryOpeningFlow.cs` owns the mandatory post-arrest handoff. Keep the `ArrestCutscene`, `EggsileTransition`, and `HubArrival` node names stable. The handoff target is `res://levels/eggsile/maps/EggsIsle.tscn`.
- The scene's inline arrest lines are intentionally replaced by `FactoryOpeningFlow` so the narrative stays synchronized with the transfer.
- Stable node names per room: `SortingFloorEntrance`/`ClockOutReturn`, `AssemblyLineEntrance`/`SortingFloorArrival`, `AssemblyLineExit`/`AssemblyLineArrival`, `LoadingBayEntrance`/`ControlRoomReturn`, `EggsileTransition`/`HubArrival`. Renaming any of them requires updating every source transition and `VerifyFactoryExpansion.cs`.

## Verification assertions

When verifying the Factory reference, assert these observable per-room behaviors:

1. **New-game reset/load**: Starting a new game loads `OpeningZone.tscn`, not the Overworld.
2. **Pre/post-gate transition behavior**: each room's exit is blocked before its completion flag is set: `SortingFloorEntrance` before `tutorial_clocked_out`; `AssemblyLineEntrance` before `tutorial_crate_gate_open`; `AssemblyLineExit` before `factory_shutdown_checklist_signed`; `LoadingBayEntrance` before `factory_shutdown_inspection_complete`; `EggsileTransition` before `arrested`.
3. **Jamitor dialogue/flag**: Talking to Jamitor sets `met_jamitor` and teaches the four controls.
4. **Crate-to-plate gate**: Pushing `FactoryCrate` onto `FactoryPressurePlate` opens `CrateGate` and sets `tutorial_crate_gate_open`.
5. **Conveyor traversal**: All 12 `Conveyor01..12` push west at 80 px/s; the maintenance pads pair bidirectionally.
6. **Sequence puzzle**: Stepping `SequencePlateA`→`B`→`C` within 5 seconds opens `InspectionDoor`; wrong order resets.
7. **Both timed switches**: Either `LoadingTimedSwitchWest` or `LoadingTimedSwitchEast` opens `LoadingTimedGate` for five seconds.
8. **One-shot arrest persistence**: Entering the arrest trigger once sets `arrested` and `cutscene_arrest`; reloading a save preserves both.
9. **Post-arrest warp unlock and automatic transfer**: After the arrest cutscene, `eggsile_area1` is unlocked and the player is transferred to Eggs Isle at `HubArrival`.
10. **No save bypass**: A production save checkpoint must not permit a new player to bypass the arrest state.

For generic map conventions, component placement, and transition graph checks, see [Level Authoring Workflow](level-authoring.md). The structural contract for the five rooms is enforced by `tests/VerifyFactoryExpansion.cs` (C#).
