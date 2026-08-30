# Eggs Isle Arrival/Intake Level — Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Replace the bare single-room `area1.tscn` with a full Eggs Isle arrival/intake level (`EggsIsle.tscn`) that is the factory→eggsile destination: boat-arrival intro cutscene, intake dialog (Joe/Frank), a quest-log quest, and layout familiarization — while keeping every external wiring point stable.

**Architecture:** One new `BaseLevel` scene absorbs the existing intake content (Joe, Frank, towels, cell key, warp, save point) plus new content (arrival cutscene, Officer Bacon handoff, "Eggs Isle: A History" readable, quest). All ~11 references to `area1.tscn` are repointed to the new scene; `area1.tscn` is deleted. Quest is a `QuestDefinition` registered in `QuestManager`.

**Tech Stack:** Godot 4.7 Mono / C# · `CutsceneTrigger` + `CutsceneResource` · `QuestManager`/`QuestDefinition`/`QuestObjective` · `LevelTransition` · `WorldFlags` · `PickupItem`/`ReadableObject`.

**Tracking:** GitHub issue **#130** (filed, labeled `content,priority-high,demo,story`, milestone Phase 1). Also closes **#75** (superseded intake criteria) and **#127** (dead sewers→eggsile transition) along the way.

---

## 0. Context & current state (verified 2026-08-04)

- `levels/eggsile/maps/area1.tscn` is a 147-line single room: `CoreTilemapLayer`, `Joe`, `Frank`, `Towel1/2/3` + `IntakeTowels` tracker, `CellKeyPickup` (`cell_key`), `KitchenTransition` (gated `intake_settled` → `Kitchen.tscn/IntakeArrival`), `HubArrival`, `HubSavePoint`, `WarpPoint` (`eggsile_area1`), `LeftHallwayTransition`, `DummyUp`, `SandboxArrival`.
- `docs/eggsile-intake.md` is the load-bearing intake design contract; the towel scavenger + `IntakeTowels` tracker (`components/eggsile/IntakeTowels.cs`) are implemented and verifier-covered (`tests/VerifyEggsileIntake.cs`).
- Factory handoff: `levels/factory/FactoryOpeningFlow.cs:11` (`EggsileScenePath`) + `levels/factory/maps/LoadingBay.tscn:404` (`EggsileTransition`, `RequiredFlag=arrested`, → `area1.tscn/HubArrival`).
- **#127 (open bug):** `levels/eggsile/maps/EggsileSewers.tscn:83` `Area1Exit` → `area1.tscn/SewersEntrance` — that target node does **not** exist in area1. The new scene must include `SewersEntrance`, which fixes #127 for free.
- Quest system: `QuestManager` autoload exists; only `FactoryGateQuest.tres` is registered (`autoload/QuestManager.tscn`). `QuestDefinition` = `Id/Title/Description/StartFlag/Objectives[]`; status completes when the **final** objective's `CompletionFlag` is set.
- `ReadableObject` (`components/npcs/ReadableObject.tscn` + `.cs`) exists and is unused in eggsile — perfect for the #75 "Eggs Isle: A History" book.
- `LevelTransition` has `RequiredFlag` gating but **no** flag-setting on fire — one small additive C# export is needed for the quest's final objective (`visited_kitchen`).
- Verifiers auto-discover scenes (`tests/VerifyAllLevels.cs` walks `levels/*/maps/*.tscn`), so deleting `area1.tscn` + adding `EggsIsle.tscn` is picked up automatically. Current counts: 82 transitions, 19 warps.

### Design decisions (confirmed via question gate — no open questions remain)

| # | Decision |
|---|----------|
| 1 | **Scene strategy:** new `levels/eggsile/maps/EggsIsle.tscn` becomes the arrival/intake level; repoint all `area1.tscn` references; absorb area1 content; delete `area1.tscn`. |
| 2 | **Intro cutscene:** boat/ferry arrival — Officer Bacon hands Eggbert to intake officer Joe at a processing desk, then player walks into the cell block. One-shot OnEnter, `CutsceneId="eggsile_arrival"`. |
| 3 | **Quest:** one `QuestDefinition` "First Night on Eggs Isle" registered in `QuestManager`: meet Joe → meet Frank → gather towels → find cell key → reach kitchen. Kitchen gate stays `intake_settled`. |

---

## 1. New level as a graph (room/beat table)

Single scene, three connected beats: **dock/processing** → **cell block** → **kitchen gate** (plus side links preserved for graph stability).

| Beat | Arrival | Mandatory interactions | Gate flag/item | Puzzle in/out | Save point | Optional | Outgoing transition | Target scene | Return route |
|------|---------|------------------------|----------------|---------------|------------|----------|---------------------|--------------|--------------|
| Dock + processing | `HubArrival` (factory/overworld/warp) | `ArrivalCutscene` (OnEnter one-shot); Joe intake desk (`met_joe`); "Eggs Isle: A History" readable (`read_history_book`) | — | — | `HubSavePoint` ("EggsIsle Hub") | Officer Bacon repeat line post-cutscene | `LeftHallwayTransition`, `DummyUp` (→ Overworld), `SandboxArrival` (→ SandboxHub) | `Overworld.tscn` / `SandboxHub.tscn` | Overworld gates |
| Cell block | walk in from dock | Frank (`met_frank`, intro + `intake_settled` handoff); Towel1/2/3; `IntakeTowels` tracker | `intake_settled` (3 towels) | scavenge 3 towels → tracker sets flag | — | `CellKeyPickup` (`cell_key`, `found_cell_key`); `SewersEntrance` (→ sewers, **fixes #127**) | `KitchenTransition` (requires `intake_settled`, sets `visited_kitchen`) | `Kitchen.tscn` / `IntakeArrival` | Kitchen `IntakeArrival` → back |
| Kitchen gate | `KitchenTransition` (east) | — | `intake_settled` | — | — | — | — | `Kitchen.tscn` | Kitchen's `IntakeArrival` → `KitchenTransition` |

**Stable node names (API — referenced by other scenes; keep EXACT):** `HubArrival`, `KitchenTransition`, `LeftHallwayTransition`, `DummyUp`, `SandboxArrival`, `SewersEntrance`, `WarpPoint`, `HubSavePoint`.

---

## 2. Task list (bite-sized, ordered)

### Phase A — Preflight

**Task A1: Rebase + fresh-worktree import**
- Run: `git rebase main` (from `feature/new-eggsile-level` worktree).
- Run: `godot --headless --path . --import` — a new worktree lacks the `.godot/` import cache; verifiers fail with bogus missing-resource errors otherwise (known pitfall).
- Confirm baseline green: `godot --headless --path . --script res://tests/VerifyAllLevels.cs` exits 0.

### Phase B — Small C# + resource scaffolding

**Task B1: Add `SetFlagsOnFire` to `LevelTransition`**
- File: `levels/LevelTransition.cs` (additive, after `RequiredFlag` at line 61):
  ```csharp
  /// <summary>WorldFlags set to true when the transition fires (after RequiredFlag passes).</summary>
  [Export]
  public string[] SetFlagsOnFire { get; set; }
  ```
- In `SceneTransition(Node2D body)`, after the `RequiredFlag` check passes and before `LoadLevel` (around line 147):
  ```csharp
  if (SetFlagsOnFire != null)
      foreach (string flag in SetFlagsOnFire)
          if (!string.IsNullOrEmpty(flag))
              WorldFlags.Instance.SetFlag(flag, true);
  ```
- Verify: `dotnet build` → 0 errors/warnings. (Pattern mirrors `CutsceneTrigger.SetFlagsOnFire`.)

**Task B2: Author the quest resource**
- Create `resources/quests/EggsIsleIntakeQuest.tres` — **use the Inspector** (`Resource > New > QuestDefinition`, add 5 `QuestObjective` sub-resources, save). Copy the shape of `resources/quests/FactoryGateQuest.tres` (flat `Array[Resource]` of objective sub-resources with `script = ExtResource("QuestObjective")`).

| Field | Value |
|-------|-------|
| `Id` | `eggs_isle_first_night` |
| `Title` | `First Night on Eggs Isle` |
| `Description` | `Get processed at intake, meet your cellmate, and find your way to the kitchen.` |
| `StartFlag` | `arrested` (quest activates when the player arrives from the factory) |
| Objective 1 | `Id=meet_joe` · `Description="Report to intake officer Joe."` · `CompletionFlag=met_joe` |
| Objective 2 | `Id=meet_frank` · `Description="Meet your cellmate Frank."` · `CompletionFlag=met_frank` |
| Objective 3 | `Id=gather_towels` · `Description="Find the 3 intake towels for Frank."` · `CompletionFlag=intake_settled` |
| Objective 4 | `Id=find_cell_key` · `Description="Find the cell key."` · `CompletionFlag=found_cell_key` |
| Objective 5 | `Id=reach_kitchen` · `Description="Reach the kitchen."` · `CompletionFlag=visited_kitchen` |

- Verify: `dotnet build`; load the .tres headless in a scratch verifier or the editor inspector (no validation warnings).

**Task B3: Register the quest in QuestManager**
- File: `autoload/QuestManager.tscn` — append to `Quests` array: `[ExtResource("2_yqvp3"), ExtResource("3_intake")]` with a new `[ext_resource type="Resource" path="res://resources/quests/EggsIsleIntakeQuest.tres" id="3_intake"]` line.
- Verify: `dotnet build`; `QuestManager` logs no validation errors at boot.

### Phase C — Author the arrival cutscene resource

**Task C1: Create `EggsIsleArrivalCutscene.tres`**
- Path: `levels/eggsile/npcs/EggsIsleArrivalCutscene.tres`. **Inspector-authored** (nested `CutsceneStep` sub-resources — never hand-write; use the Cutscene Inspector plugin).
- Steps (CutsceneResource, `StepType` per `docs/godot-editor-guide.md` §6):
  1. `LockPlayer`
  2. `Fade` (in from black — the ferry arrival)
  3. `SayDialog` — Officer Bacon: handoff line ("Eggs Isle, Eggbert. Try not to cause any more trouble." — final wording tuned to arrest lines in `FactoryOpeningFlow.ArrestDialogLines`)
  4. `SayDialog` — Joe: intake speech ("Factory transfer? Rough first night. Let's get you processed." …)
  5. `SayDialog` — Joe: orientation ("Desk's here, cell block's through there. Kitchen's past that when you're settled in.")
  6. `MoveNpc` (optional) — Bacon walks toward the dock / off-screen
  7. `UnlockPlayer`
- No `SetFlag` step needed: the trigger's `Once + CutsceneId="eggsile_arrival"` auto-sets `cutscene_eggsile_arrival`.
- Verify: `dotnet build`; headless-load the resource and print step count (pattern: `CHECKPOINT.md`'s cutscene step-count checks).

### Phase D — Build the new scene (editor/MCP work)

> Author via the Godot editor / MCP (`create_scene`, `add_node`, `set_property`, `save_scene`, tile painting). Never hand-edit `tile_map_data` or nested `.tres` data.

**Task D1: Create `levels/eggsile/maps/EggsIsle.tscn`**
- Root `Node2D` + `BaseLevel.cs`: `LevelName = "Eggs Isle — Intake"`, `LevelMusic = lvl_3_the_grassland.ogg`, `LevelAmbience = isle_tide_ambient.ogg` (same ext_resources as area1).
- `CoreTilemapLayer` (instanced `components/core/CoreTilemapLayer.tscn`, script `LevelTileMapLayer.cs`, `tile_set = eggsile_tileset.tres`) — paint a **larger** floor than area1 (≥1536×1024 world px per production contract): dock/processing area on the west, cell block center, kitchen gate east. Do not reuse area1's `tile_map_data`; paint fresh in the editor.

**Task D2: Arrival + save + warp**
- `HubArrival` (LevelTransition → `Overworld.tscn/HubEggsileArea1Gate`, Side=2/Down, Size=4, pos ≈ (−640, 320)) — destination marker for factory handoff, overworld gate, warp.
- `HubSavePoint` (`LocationName = "EggsIsle Hub"`, near arrival).
- `WarpPoint` (`WarpId = "eggsile_area1"` — **keep the id**; `WarpDatabase` entry is repointed, not renamed, so unlock state persists).

**Task D3: Intro cutscene trigger**
- `ArrivalCutscene` (CutsceneTrigger): `Mode = OnEnter`, `Once = true`, `CutsceneId = "eggsile_arrival"`, `Cutscene = EggsIsleArrivalCutscene.tres`, positioned to overlap the spawn point at `HubArrival` so it fires immediately on factory arrival.

**Task D4: Intake NPCs**
- `Joe` (instance `levels/eggsile/npcs/Joe.tscn`) at the processing desk; add `SetFlagsOnFire = ["met_joe"]` on its `CutsceneTrigger`.
- `OfficerBacon` for the handoff — instance the existing arrest NPC (`levels/factory/npcs/FactoryOfficerBacon.tscn`; fallback `levels/overworld/npcs/OfficerBacon.tscn`), placed near the dock. Optional repeatable OnInteract line gated on `cutscene_eggsile_arrival` (post-cutscene flavor).
- `HistoryBook` — instance `components/npcs/ReadableObject.tscn` at the desk: text = "Eggs Isle: A History" (prison lore that seeds Egguardo's Courtyard quiz answers per #75), `SetFlagsOnFire = ["read_history_book"]` (or the readable's own read-flag mechanism — match `read_<object>` convention).

**Task D5: Cell block content (absorbed from area1)**
- `Frank` (instance `levels/eggsile/npcs/Frank.tscn`) — **upgrade to conditional dialog**: `Cutscene = FrankIntake.tres` (new Inspector-authored CutsceneResource, or inline `DialogLines` + a second gated trigger): intro lines when `intake_settled` not set; handoff line ("Grab your Cell Key and get to the kitchen — I'll cover you.") when `intake_settled` is set (use `CutsceneCondition.FlagSet`). Add `SetFlagsOnFire = ["met_frank"]` (currently missing — #75).
- `Towel1/2/3` (CutsceneTrigger, `Mode=OnEnter`, `Once=true`, `CutsceneId=towel_1/2/3`, existing dialog lines) scattered in the cell block.
- `IntakeTowels` (script `components/eggsile/IntakeTowels.cs`) — unchanged; sets `intake_settled`.
- `CellKeyPickup` (`PickupItem`, `ItemId = cell_key`, sprite `item_sprite_0009.png`) on the scavenger path; **add `SetFlag = ["found_cell_key"]`** (#75).

**Task D6: Outgoing transitions (all direct-root)**
- `KitchenTransition` → `Kitchen.tscn/IntakeArrival`, `RequiredFlag = "intake_settled"`, `SetFlagsOnFire = ["visited_kitchen"]`, Side=1/Down, Size=5, pos ≈ (400, 0) (east side of cell block).
- `SewersEntrance` → `EggsileSewers.tscn/Area1Exit` (**NEW — resolves the #127 dead link**; place near the cell block).
- `LeftHallwayTransition` → `Overworld.tscn/RightWalkwayTransition` (preserved).
- `DummyUp` → `Overworld.tscn/DummyDown` (preserved).
- `SandboxArrival` → `SandboxHub.tscn/EastToArea1` (preserved).
- Verify in-editor: every transition's destination node exists in the target scene, both directions.

### Phase E — Repoint references + delete area1

**Task E1: Factory handoff (2 files)**
- `levels/factory/FactoryOpeningFlow.cs:11`: `EggsileScenePath` → `"res://levels/eggsile/maps/EggsIsle.tscn"`.
- `levels/factory/maps/LoadingBay.tscn:404`: `EggsileTransition.Level` → `"res://levels/eggsile/maps/EggsIsle.tscn"` (target `HubArrival` unchanged).

**Task E2: Kitchen return trip (1 file)**
- `levels/kitchen/maps/Kitchen.tscn:115`: `IntakeArrival.Level` → `"res://levels/eggsile/maps/EggsIsle.tscn"` (target `KitchenTransition` unchanged).

**Task E3: Overworld gates (1 file, 3 props)**
- `levels/overworld/maps/Overworld.tscn` lines 38, 46, 108: `RightWalkwayTransition`, `DummyDown`, `HubEggsileArea1Gate` — `Level` → `"res://levels/eggsile/maps/EggsIsle.tscn"` (targets `LeftHallwayTransition`, `DummyUp`, `HubArrival` unchanged).

**Task E4: Warp database (1 file)**
- `components/maps/WarpDatabase.cs:37`: `eggsile_area1.LevelPath` → `"res://levels/eggsile/maps/EggsIsle.tscn"` (keep `Id = "eggsile_area1"`, `TargetTransitionName = "HubArrival"`).

**Task E5: Sewers + sandbox (3 files)**
- `levels/eggsile/maps/EggsileSewers.tscn:83`: `Area1Exit.Level` → `"res://levels/eggsile/maps/EggsIsle.tscn"` (target `SewersEntrance` — now exists; **fixes #127**).
- `levels/sandbox/maps/SandboxHub.tscn:52`: `EastToArea1.Level` → `"res://levels/eggsile/maps/EggsIsle.tscn"` (target `SandboxArrival` unchanged).
- `tests/GenerateSandboxLevels.cs:67`: update the `area1.tscn`/`SandboxArrival` tuple → `EggsIsle.tscn`/`SandboxArrival` (keep in sync with the sandbox hub).

**Task E6: Delete area1**
- After E1–E5: `git rm levels/eggsile/maps/area1.tscn` (and its `.uid` sidecar if present).
- Verify: `grep -rn "area1.tscn" --include='*.tscn' --include='*.cs' --include='*.tres' .` → only `backups/` hits remain (never touch backups).

### Phase F — Verifier + docs

**Task F1: Rewrite the intake verifier**
- Update `tests/VerifyEggsileIntake.cs` (rename class/file to `VerifyEggsIsleIntake.cs` — or update in place; keep `res://tests/` C# SceneTree pattern from `VerifyFactoryExpansion.cs`). Assert:
  - `EggsIsle.tscn` exists, loads, **instantiates** (`scene.Instantiate()` — Load ≠ Instantiate), root is `BaseLevel`.
  - Direct-root nodes resolve: `HubArrival`, `Joe`, `Frank`, `CellKeyPickup`, `IntakeTowels`, `WarpPoint`, `HubSavePoint`, `ArrivalCutscene`, `HistoryBook`, `KitchenTransition`, `SewersEntrance`, `LeftHallwayTransition`, `DummyUp`, `SandboxArrival`.
  - `ArrivalCutscene`: `Mode=OnEnter`, `Once=true`, `CutsceneId="eggsile_arrival"`, `Cutscene` resource non-null.
  - `KitchenTransition`: `Level=Kitchen.tscn`, `TargetTransitionName=IntakeArrival`, `RequiredFlag=intake_settled`, `SetFlagsOnFire` contains `visited_kitchen`.
  - `SewersEntrance`: `Level=EggsileSewers.tscn`, `TargetTransitionName=Area1Exit` (**#127 regression guard**).
  - Towels: 3 present, OnEnter, one-shot, ids `towel_1/2/3`.
  - `cell_key` in `ItemDatabase`; `CellKeyPickup.SetFlag` contains `found_cell_key`.
  - Flag wiring: Joe trigger sets `met_joe`; Frank trigger sets `met_frank`.
  - `area1.tscn` absent from `levels/eggsile/maps/` (deleted).
  - `Kitchen.tscn/IntakeArrival` → `EggsIsle.tscn/KitchenTransition` (both directions resolve).
  - Quest: `QuestManager.GetQuest("eggs_isle_first_night")` non-null with 5 objectives (load `autoload/QuestManager.tscn` or assert the .tres + registration).

**Task F2: Docs**
- `docs/eggsile-intake.md`: entry contract (scene path → `EggsIsle.tscn`, arrival, flags) + note the quest + cutscene.
- `docs/factory-opening.md:61`: handoff row → `EggsIsle.tscn`/`HubArrival`.
- `AGENTS.md` demo-route line ("arrest → `levels/eggsile/maps/area1.tscn`") → `EggsIsle.tscn` (additive, one line).
- `ROADMAP.md`/`MASTER_ROADMAP.md`: add #130 to Phase 1 issue map; mark #127 fixed.

### Phase G — Verification & landing

**Task G1: Full verification**
- `dotnet build` → 0 errors, 0 warnings.
- `godot --headless --path . --script res://tests/VerifyEggsIsleIntake.cs` (or updated name) → ALL CHECKS PASSED.
- `godot --headless --path . --script res://tests/VerifyAllLevels.cs` → all scenes instantiate; every transition + warp resolves (counts shift: 82→~83 transitions, 19 warps). Exit 0.
- In-editor traversal (if feasible): run project → New Game → factory → arrest → **must land on the dock with the arrival cutscene**; towel scavenge → Frank handoff → kitchen gate opens → Kitchen; return trip works; sewers `Area1Exit` → `SewersEntrance` round-trip (closes #127); save/reload at `HubSavePoint`.
- Log check: `EGGBERT_LOG_LEVEL=debug`, grep `user://logs/eggbert_*.jsonl` for `CutsceneTrigger`, `LevelTransition`, `PickupItem`, `WorldFlags`, `SavePoint`, `QuestManager` tags.

**Task G2: Commit + land (concurrent-agents workflow)**
- Commit message: `feat: full Eggs Isle arrival/intake level replacing area1 — Closes #130, Closes #75, Fixes #127` (one commit; #75's remaining criteria — Frank dialog, history book, `met_frank`/`read_history_book`/`found_cell_key` — are all delivered here).
- Rebase onto main (`git rebase main`), re-run G1 gates, then fast-forward main in the main worktree: `git merge --ff-only feature/new-eggsile-level` + `git push origin main` (per `eggbert-git-rebase-workflow` skill).
- Verify issues closed as `completed` with a comment citing the commit + verifier (close-reason discipline; never bulk-close).

---

## 3. Files likely to change

| File | Change |
|------|--------|
| `levels/eggsile/maps/EggsIsle.tscn` | **Create** — the new level |
| `levels/eggsile/npcs/EggsIsleArrivalCutscene.tres` | **Create** — intro cutscene |
| `levels/eggsile/npcs/FrankIntake.tres` (or inline) | **Create** — Frank conditional dialog |
| `resources/quests/EggsIsleIntakeQuest.tres` | **Create** — quest |
| `levels/LevelTransition.cs` | Add `SetFlagsOnFire` export + fire |
| `autoload/QuestManager.tscn` | Register quest |
| `levels/eggsile/npcs/Joe.tscn` | Add `SetFlagsOnFire=["met_joe"]` |
| `levels/eggsile/npcs/Frank.tscn` | Add `met_frank` + conditional handoff |
| `levels/factory/FactoryOpeningFlow.cs` | Repoint scene path |
| `levels/factory/maps/LoadingBay.tscn` | Repoint `EggsileTransition` |
| `levels/kitchen/maps/Kitchen.tscn` | Repoint `IntakeArrival` |
| `levels/overworld/maps/Overworld.tscn` | Repoint 3 gates |
| `components/maps/WarpDatabase.cs` | Repoint `eggsile_area1` |
| `levels/eggsile/maps/EggsileSewers.tscn` | Repoint `Area1Exit` (**fixes #127**) |
| `levels/sandbox/maps/SandboxHub.tscn` + `tests/GenerateSandboxLevels.cs` | Repoint sandbox gate |
| `tests/VerifyEggsileIntake.cs` | Rewrite for new scene |
| `docs/eggsile-intake.md`, `docs/factory-opening.md`, `AGENTS.md`, `ROADMAP.md`, `MASTER_ROADMAP.md` | Docs |
| `levels/eggsile/maps/area1.tscn` (+ `.uid`) | **Delete** (after repoint; `backups/` untouched) |

**Ownership note (AGENTS.md file map):** this work is Levels-agent territory (`levels/**`, `docs/factory-opening.md`, `docs/level-authoring.md`). It touches `components/maps/WarpDatabase.cs` and `autoload/QuestManager.tscn` (Systems/Content agent files) — both **additive string/resource registrations only**; flag them when landing so concurrent agents rebase cleanly.

## 4. Tests / validation

- `dotnet build` — 0 errors, 0 warnings.
- `godot --headless --path . --script res://tests/VerifyEggsIsleIntake.cs` — intake structural contract.
- `godot --headless --path . --script res://tests/VerifyAllLevels.cs` — full scene gate (all 24→25 scenes instantiate; transitions + warps resolve; **catches any missed `area1.tscn` reference** as a dead transition).
- Manual demo route: New Game → factory (5 rooms) → arrest → arrival cutscene → intake quest → kitchen.

## 5. Risks, tradeoffs, open questions

- **Scene repoint blast radius (~11 refs).** Mitigated by keeping every stable node name (`HubArrival`, `KitchenTransition`, `SewersEntrance`, `SandboxArrival`, `LeftHallwayTransition`, `DummyUp`) identical in the new scene, so all repoints are single-property `Level` string swaps. `VerifyAllLevels` is the safety net.
- **Warp id stability.** `eggsile_area1` id is preserved so `FactoryOpeningFlow`'s `WarpDatabase.Unlock("eggsile_area1")` and saved unlock state keep working.
- **`visited_kitchen` flag.** Requires the `LevelTransition.SetFlagsOnFire` addition (Task B1). Fallback if unwanted: drop objective 5 and let the quest complete at `intake_settled` (objective 3) — but the confirmed design keeps 5 objectives.
- **Cutscene/quest .tres authoring.** Both have nested sub-resources → Inspector-authored only (never hand-written). If the editor is unavailable, fall back to inline `DialogLines` on Frank and a `SetFlagsOnFire`-based flow, and document the deviation.
- **Delete vs. keep area1.** Decision 1 says delete after absorption. If any reference is missed (grep in E6), the scene gate fails loudly — fix by repointing, not by keeping area1.
- **Boat sprite.** Placeholder-art-ok: the arrival is dialog + fade, no boat mesh required (YAGNI). A ferry sprite is a Phase 4 polish item.
- **Open questions:** none blocking — all design choices confirmed via question gate and recorded in #130. Dialog wording is content-tuned at implementation (tone: EarthBound-weird, per STORY.md).

---

## 6. Execution handoff

Plan complete. Ready to execute task-by-task — dispatch a fresh subagent per phase with two-stage review (spec compliance, then code quality) via subagent-driven-development, or implement directly on `feature/new-eggsile-level`. Landing closes **#130**, **#75**, and **#127** with one commit.
