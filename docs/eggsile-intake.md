# Eggs Isle Intake — Beat Design

STORY.md **beat 2**. The player (Eggbert) arrives at Eggs Isle prison after the factory
arrest handoff. This doc is the load-bearing design contract; implementation follows it.

---

## Entry contract (already wired)

- **Scene:** `res://levels/eggsile/maps/area1.tscn` (root `Node2D` + `BaseLevel.cs`, `LevelName = "Eggsile Area 1"`).
- **Arrival:** `FactoryOpeningFlow.cs` → `LoadLevel(area1.tscn, "HubArrival")` after the arrest cutscene.
  `HubArrival` is the left-gate transition at ≈ `(-640, 320)`.
- **Flags set on entry:** `"arrested"` (fact), `"warp_eggsile_area1"` (warp unlock). Do not re-set.
- **Ambience:** music `lvl_3_the_grassland.ogg`, ambience `isle_tide_ambient.ogg` (already on root).
- **Tilemap:** `CoreTilemapLayer` (external `eggsile_tileset.tres`). Geometry is **reused, not repainted**.

---

## Objective & exit condition

The intake beat is a **character-piece + small scavenger task**:

1. Player spawns at `HubArrival` and meets **Joe** (intake officer) doing intake dialog.
2. Player meets **Frank** (cellmate) who explains the prison layout (Blocks A/B/C, Guard Room north,
   kitchen west) and asks for a small favor — grab the **towels** scattered around the cell block.
3. Player collects **3 towels** (flag-gated collection). Completing the collection sets
   `intake_settled`.
4. Frank hands off; the **Kitchen exit** transition unlocks (requires `intake_settled`)
   → `res://levels/kitchen/maps/Kitchen.tscn`.

Exit condition = collect all towels → Frank's handoff line → gated transition to Kitchen.

---

## Layout & reuse

Reuse existing geometry and the placed NPCs (`Joe` ≈ `(-87,-83)`, `Frank` ≈ `(200,-80)`).
Place the 3 towel triggers in open floor near the cell block so they require light
exploration but no hazards (arrival stays safe per the production contract).

---

## Beats (beat-by-beat)

| # | Beat | Mechanism | Flag(s) set |
|---|------|-----------|-------------|
| 1 | **Joe intake line** | `Joe` NPC `CutsceneTrigger` (OnInteract) | — |
| 2 | **Frank intro + layout + favor** | `Frank` NPC: staged arrival cutscene (One-shot OnEnter), then repeatable OnInteract 'what now' | `cutscene_frank_intake` (one-shot) |
| 3 | **Scavenge towels** | 3 towel triggers (OnEnter one-shot), each with dialog line | `cutscene_towel_1/2/3` |
| 4 | **Collection complete** | `IntakeTowels` tracker sets `intake_settled` when all 3 towels collected | `intake_settled` |
| 5 | **Frank handoff** | `Frank` OnInteract line gated on `intake_settled` | — |
| 6 | **Kitchen gate** | New `LevelTransition` at right of cell block, `RequiredFlag = "intake_settled"`, → `Kitchen.tscn` | — |

---

## Pruning (matches beat 2; removes orphaned non-beat2 content)

Remove from `area1.tscn` (all duplicates/orphans NOT part of beat 2; the real homes already exist
in `levels/kitchen/maps/Kitchen.tscn`):

- `DrainMonsterSequence` + `SeqSwitch1/2/3` + `RewardDoor` (orphaned puzzle, no story home yet)
- `Chef` (Kitchen owns Chef + kitchen hints; `met_chef` belongs to Kitchen beat)
- `ScrambledEggPickup` (Kitchen owns its own scramble pickup)
- `SewersEntrance` + `SewersTopEntrance` (sewers are not part of beat 2; revisit later)
- Remove unused `ext_resource` entries that these nodes referenced (clean the header)

**Keep:** `CoreTilemapLayer`, `Joe`, `Frank`, `CellKeyPickup` (reward), `WarpPoint` (`eggsile_area1`),
`HubArrival`, `HubSavePoint`, `LeftHallwayTransition`/`DummyUp` (hub-gate wiring, verify before pruning).

---

## Reward

**Cell Key** (`cell_key`, existing `ItemDatabase` Key item, Sprite `item_sprite_0009.png`).
Already placed in the scene — confirm its position is on the scavenger path / reachable after
towels. Frank references it ("that's for your cell's short-cut to the drains") once `intake_settled`.

---

## Required flags (all new)

| Flag | Type | Meaning |
|------|------|---------|
| `cutscene_frank_intake` | one-shot | Frank's staged arrival intro fired |
| `cutscene_towel_1/2/3` | one-shot | each towel collected |
| `intake_settled` | fact/gate | all towels collected; gates Kitchen exit + Frank handoff line |

Naming follows existing conventions (`cutscene_<id>` one-shots; short gate flags). No collision
with registered flags (`arrested`, `warp_eggsile_area1`, `met_chef`, etc.).

---

## New component (only place no existing component suffices)

`IntakeTowels` — small `Node2D`/`Node` script `components/eggsile/IntakeTowels.cs` added to
`area1.tscn`. It watches the three `cutscene_towel_*` flags (via `WorldFlags` + one `_Process`
poll or a flag hook) and sets `intake_settled = true` once all three are set. A 3-flag AND gate is
not expressible with any single-`RequiredFlag` component, so a tiny tracker is justified
per `docs/level-authoring.md` ("confirming no existing component suffices").

---

## Dialog (uses existing CutsceneTrigger/DialogLines — no new resources unless needed)

- **Joe** (`CutsceneTrigger` inline): intake line (keep/reuse existing JoeArrival beats).
- **Frank** staged arrival (`CutsceneTrigger`, `Mode=OnEnter`, `Once=true`,
  `CutsceneId="frank_intake"`, `DialogLines`): "You're the factory transfer? Rough night." +
  layout explanation (Guard Room north, Blocks A/B/C, kitchen west) + favor request ("see if
  there's a dry towel anywhere — check the cells, the washbasin, the vents.").
- **Frank post-settle** (OnInteract): "Good. Grab your Cell Key and get to the kitchen. I'll
  cover you." + sets nothing new.
- **Towel triggers** (OnEnter one-shot, dialog): "Found a towel. Somehow both damp and dusty."
  (echo of Joe's intake line).

---

## Kitchen entry (destination wiring)

`kitchen/maps/Kitchen.tscn` currently has `HubArrival` (west → Overworld) and `KitchenExit`
(east → Courtyard). **Add** a `LevelTransition` on its north-side or a matching entry node named
`IntakeArrival` targeted by the intake's exit transition. (Confirm exact side/offset during
implementation; the two scenes' gate names must match.)

---

## Verification plan

1. `dotnet build` clean.
2. Headless verifier `tests/VerifyEggsileIntake.cs` (pattern: `tests/VerifyFactoryExpansion.cs`):
   - instantiate `area1.tscn`; assert root is `BaseLevel`; assert `Joe`, `Frank`, `CellKeyPickup`,
     `IntakeTowels`, the `Kitchen` exit transition + its `RequiredFlag="intake_settled"` all resolve.
   - assert referenced `cell_key` exists in `ItemDatabase`; assert all `ext_resource` files exist.
   - assert pruned nodes (`DrainMonsterSequence`, `SeqSwitch*`, `Chef`, `ScrambledEggPickup`,
     `Sewers*`) are absent.
   - assert `Kitchen.tscn` has the `IntakeArrival` node the exit transition targets.
3. Run `godot --headless --path . --script res://tests/VerifyEggsileIntake.cs`.