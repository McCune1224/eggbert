# Eggs Isle Intake — Beat Design (LEGACY — superseded 2026-08)

> **Superseded by the First Night overhaul (#175–#181).** The rebuilt exile is a brand-new
> three-map level (Dock → Gatehouse → Overflow wing) documented in
> **`docs/eggsile-first-night.md`**. Nothing from the old intake was carried over — the old
> scene exists only in git history. This doc remains as the historical contract of the
> original ~30-second intake.

STORY.md **beat 2**. The player (Eggbert) arrives at Eggs Isle prison after the factory
arrest handoff. This doc is the load-bearing design contract; implementation follows it.

> **Scope note (2026-08, #174):** all story-chain zones beyond the intake (Kitchen,
> Prison, Courtyard, …) were removed as test content. The intake is now the **end of
> the shipped tutorial** — the exile ends here. The Kitchen exit transition was pruned;
> the only way back to the factory is the `eggsile_area1` warp (fast travel).

---

## Entry contract (already wired)

- **Scene:** `res://levels/eggsile/maps/EggsIsle.tscn` (root `Node2D` + `BaseLevel.cs`, `LevelName = "Eggs Isle — Intake"`).
- **Arrival:** `FactoryOpeningFlow.cs` → `LoadLevel(EggsIsle.tscn, "HubArrival")` after the arrest cutscene.
  `HubArrival` is the left-gate transition at ≈ `(-640, 320)`.
- **Intro cutscene:** one-shot OnEnter `ArrivalCutscene` (CutsceneId `eggsile_arrival`) — ferry dock, Officer
  Bacon hands Eggbert to intake officer Joe at the processing desk, then the player walks into the cell block.
- **Flags set on entry:** `"arrested"` (fact), `"warp_eggsile_area1"` (warp unlock). Do not re-set.
- **Ambience:** music `lvl_3_the_grassland.ogg`, ambience `isle_tide_ambient.ogg` (already on root).
- **Tilemap:** `CoreTilemapLayer` (external `eggsile_tileset.tres`). Geometry is **reused, not repainted**.

---

## Objective & exit condition

The intake beat is a **character-piece + small scavenger task**:

1. Player spawns at `HubArrival` and meets **Joe** (intake officer) doing intake dialog.
2. Player meets **Frank** (cellmate) who explains the prison layout (Blocks A/B/C, Guard Room north,
   cells west) and asks for a small favor — grab the **towels** scattered around the cell block.
3. Player collects **3 towels** (flag-gated collection). Completing the collection sets
   `intake_settled`.
4. Frank hands off; the player grabs the **Cell Key** — the beat (and the tutorial) ends here.

Exit condition = collect all towels → Frank's handoff line → find the Cell Key. There is no
outgoing level transition (the old Kitchen exit was pruned in #174); fast travel via the
`eggsile_area1` warp is the way back to the factory.

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
| 6 | **Cell Key reward** | `CellKeyPickup` (PickupItem) grants `cell_key`, sets `found_cell_key` | `found_cell_key` |

The old beat-6 **Kitchen gate** (`KitchenTransition`, `RequiredFlag="intake_settled"` → `Kitchen.tscn`)
was pruned with the Kitchen zone in #174.

---

## Pruning (matches beat 2; removes orphaned non-beat2 content)

Pruned from the intake scene (the 2026-08 cleanup #174 removed the zones these belonged to):

- `KitchenTransition` (→ Kitchen), `SewersEntrance` (→ EggsileSewers),
  `LeftHallwayTransition`/`DummyUp` (→ Overworld hub), `SandboxArrival` (→ SandboxHub) — all exits
  to removed zones are gone.
- `HubArrival` remains as the arrest-handoff + warp spawn anchor, self-anchored (leads to itself;
  there is no Overworld hub anymore).
- `Chef.tscn` (Kitchen-owned) was deleted with the Kitchen zone.
- Remove unused `ext_resource` entries that these nodes referenced (clean the header)

**Keep:** `CoreTilemapLayer`, `Joe`, `Frank`, `CellKeyPickup` (reward), `WarpPoint` (`eggsile_area1`),
`HubArrival`, `HubSavePoint`.

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
| `intake_settled` | fact/gate | all towels collected; gates Frank handoff line |

Naming follows existing conventions (`cutscene_<id>` one-shots; short gate flags). No collision
with registered flags (`arrested`, `warp_eggsile_area1`, etc.).

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
  layout explanation (Guard Room north, Blocks A/B/C, cells west) + favor request ("see if
  there's a dry towel anywhere — check the cells, the washbasin, the vents.").
- **Frank post-settle** (OnInteract): "Good. Grab your Cell Key — I'll cover you." + sets nothing new.
- **Towel triggers** (OnEnter one-shot, dialog): "Found a towel. Somehow both damp and dusty."
  (echo of Joe's intake line).

---

## Kitchen entry (destination wiring)

**Removed in #174.** The intake's `KitchenTransition` → `Kitchen.tscn/IntakeArrival` link was
pruned with the Kitchen zone. The intake is a terminus; the `eggsile_area1` warp (unlocked by
`FactoryOpeningFlow` after the arrest) is the only return route. If a future story chain is
rebuilt, restore the exit transition here and re-add the `IntakeArrival` node to the destination.

---

## Verification plan

1. `dotnet build` clean.
2. Headless verifier `tests/VerifyEggsileIntake.cs` (pattern: `tests/VerifyFactoryExpansion.cs`):
   - instantiate `EggsIsle.tscn`; assert root is `BaseLevel`; assert `Joe`, `Frank`, `CellKeyPickup`,
     `IntakeTowels`, `HubArrival` (self-anchored) all resolve.
   - assert referenced `cell_key` exists in `ItemDatabase`; assert all `ext_resource` files exist.
   - assert pruned transitions (`KitchenTransition`, `SewersEntrance`, `LeftHallwayTransition`,
     `DummyUp`, `SandboxArrival`) are absent.
   - assert the intake quest has 4 objectives (no `visited_kitchen`).
3. Run `godot --headless --path . --script res://tests/VerifyEggsileIntake.cs`.