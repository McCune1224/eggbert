# Eggs Isle — First Night (Exile Overhaul) — Beat Design

STORY.md **beat 2**. The player (Eggbert) arrives at Eggs Isle prison after the factory
arrest handoff. This doc is the load-bearing design contract for the rebuilt exile
(issues #175–#181); implementation follows it.

> **Scope (2026-08, #174):** all story-chain zones beyond the intake were removed as test
> content. The exile is a **terminus** — the only way back is the `eggsile_area1` warp
> (fast travel). There is no outgoing level transition.
>
> **Fresh rebuild (v3):** the 2026-08 overhaul replaces the old ~30-second intake with a
> **brand-new three-map level** — nothing is carried over from the old scene (different
> cast, geography, quest, and beats). The old intake is preserved only in git history;
> no legacy scene is shipped. All cinematics run on the new AnimationPlayer cutscene
> system (`components/cutscene/CutsceneDirector.cs`), which replaces the fragile async
> step-runner for cinematics.

---

## Entry contract (stable API)

- **Scene:** `res://levels/eggsile/maps/EggsIsle.tscn` (root `Node2D` + `BaseLevel.cs`,
  `LevelName = "Eggs Isle — The Dock"`).
- **Arrival:** `FactoryOpeningFlow.cs` → `LoadLevel(EggsIsle.tscn, "HubArrival")` after the
  arrest cutscene. `HubArrival` remains the left-edge anchor at ≈ `(-640, 320)`.
- **Music:** `lvl_5_the_oasis_or_resting_place.ogg` (night calm). **Ambience:**
  `isle_tide_ambient.ogg` (dock) / `prison_night_ambient.ogg` (gatehouse + wing).
- **Tilemap:** `CoreTilemapLayer` (external `eggsile_tileset.tres`). Floor = (2,2),
  accents = (3,4)/(4,4), walls = (10,4)/(11,4), bars = (5,2). Walls are painted atlas
  tiles + explicit `StaticBody2D` strips (tileset has no physics — the factory pattern).
  `LevelTileMapLayer` builds perimeter `MapBorders`.

---

## Structure: three chained maps

The exile is three scenes with real level transitions (the factory pattern), matching the
player's flow: **Dock → Check-in → Cell (Frank)**.

| Map | Scene | LevelName | Content | Transitions |
|-----|-------|-----------|---------|-------------|
| The Dock | `EggsIsle.tscn` | "Eggs Isle — The Dock" | moonlit pier, sea strip, arrival cutscene, warp + save | `HubArrival` (anchor), `DockGate` → Gatehouse |
| The Gatehouse | `EggsIsleGatehouse.tscn` | "Eggs Isle — Gatehouse" | Mr Tea's booking room, check-in cutscene, readables, save | `DockArrival` → Dock, `GatehouseExit` (needs `met_tea`) → Wing |
| The Overflow | `EggsIsleBlock.tscn` | "Eggs Isle — The Overflow" | cell + Frank, boilers, Tank, Gallery, count, tunnel hatch | `GatehouseArrival` → Gatehouse |

---

## Beats (beat-by-beat)

All cutscenes are **AnimationPlayer scenes** (`levels/eggsile/cutscenes/*.tscn`) driven by
`CutsceneDirector` — the timeline pauses during dialog and resumes on advance; no async
step runner.

### Map 1 — The Dock (~1–2 min)

| # | Beat | Mechanism | Flag(s) |
|---|------|-----------|---------|
| 1 | **Arrival cutscene** | `ArrivalCutscene` (OnEnter one-shot) → `DockArrival.tscn`: LockPlayer → FadeIn → Bacon's farewell (2 lines) → UnlockPlayer | `met_bacon` |
| 2 | **The pier** | walk the moonlit pier; boat berth visual; save "Dock Lantern"; `eggsile_area1` warp (return route) | — |
| 3 | **To the gatehouse** | `DockGate` (east edge, Right) → Gatehouse | — |

### Map 2 — The Gatehouse (~2–3 min)

| # | Beat | Mechanism | Flag(s) |
|---|------|-----------|---------|
| 4 | **Check-in cutscene** | `CheckInCutscene` (OnEnter one-shot) → `GatehouseCheckIn.tscn`: Mr Tea's ritual — name/costume/occupation, prisoner #77, the routine | `met_tea` |
| 5 | **The booking room** | ledger, rules board, stamp readables; Mr Tea flavor dialog; save "Gatehouse Bench" | `read_ledger`, `read_rules`, `read_stamp` |
| 6 | **To the wing** | `GatehouseExit` (east edge, Right, `RequiredFlag=met_tea`) → Overflow | — |

### Map 3 — The Overflow (~8–10 min)

| # | Beat | Mechanism | Flag(s) |
|---|------|-----------|---------|
| 7 | **Cell placement** | `CellPlacement` (OnEnter one-shot) → `CellPlacement.tscn`: FadeOut → PlacePlayer into the cell → FadeIn → Frank's intro (the wing layout + the count warning) | `met_frank` |
| 8 | **The cell** | bunk save "Cell Niche — Bunk"; WallScratch + TinCup readables; `TunnelKeyPickup` (tunnel_key) on the bunk | `found_tunnel_key` |
| 9 | **Frank's dialog** | `Frank` OnInteract → `FrankIntake.tres` (conditional): pre-count = routine/reminder; post-count = the tunnel secret | — |
| 10 | **Boiler puzzle** | push `BoilerCrate` onto `BoilerPlate` → `BoilerDoor` opens; Mikan (boiler room) + deviled-egg reward | `boiler_gate_open` |
| 11 | **The Tank** | flooded cell; Scrambles + tide-pool hardboiled egg | `met_scrambles` |
| 12 | **The Gallery** | collapsed walkway + rubble; GallerySign readable; Wiener Cop in the corridor | `read_gallery`, `met_cop` |
| 13 | **The count** | `CountTrigger` (OnEnter one-shot, mid-corridor) → `CountEvent.tscn`: Mr Tea's voice over the speaker, Frank's line, the boilers stop | `eggsile_count_survived` |
| 14 | **The hatch (optional payoff)** | `TunnelHatch` `KeyDoor` (`RequiredFlag=found_tunnel_key`) → alcove: lucky-yolk reward + Sunnyside note | `tunnel_opened`, `read_hatch` |
| 15 | **Settle in** | Frank's final line + bunk save + warp return | — |

Exit condition = witness the count (`eggsile_count_survived`) + find the tunnel key
(`found_tunnel_key`). Optional: the hatch (`tunnel_opened`). No outgoing transition;
`eggsile_area1` warp + bunk save are the return routes.

---

## Layout (tile grid, 16 px tiles; world = tile × 16)

### The Dock (160×52: x −80…+80, y −26…+26)
- Sea strip x −80…−75 (west, accent2), pier walkway x −74…−24 (y −8…−3), boat berth
  x −74…−66, shore everywhere else.
- `HubArrival` ≈(−640,320), `ArrivalCutscene` trigger ≈(−640,296), Bacon ≈(−1000,−80),
  save "Dock Lantern" ≈(−300,300), warp `eggsile_area1` ≈(−150,300), `DockGate` at east
  edge (1280,0).

### The Gatehouse (120×60: x −60…+60, y −30…+30)
- Rim walls 3 tiles thick; booking desk visual at tiles (−21…−16, −6…−3).
- `DockArrival` at ≈(−880,0) (just inside the west rim), `GatehouseExit` at ≈(880,0)
  (just inside the east rim, `RequiredFlag=met_tea`), Mr Tea
  ≈(−300,−60), ledger ≈(−420,−100), rules board ≈(−380,60), stamp ≈(−220,−80),
  `CheckInCutscene` trigger ≈(−660,0, 240×320 — generous box), save "Gatehouse Bench" ≈(−500,200).

### The Overflow (208×76: x −104…+104, y −38…+38)
- Rim walls 3 tiles thick. North rooms (y −38…−12) / corridor (y −8…+8) / south rooms
  (y +12…+36), separated by wall bands y −12…−9 and y +9…+11 with door gaps.
- **Cell** (x −104…−40): bars front with open door gap x −71…−67; Frank ≈(−1300,−400),
  bunk save ≈(−1480,−520), WallScratch ≈(−1350,−550), TinCup ≈(−900,−300),
  TunnelKeyPickup ≈(−1480,−500).
- **Boiler room** (x −20…+34): boiler visual + collision at tiles (−16…−8, −34…−28);
  door gap x −5…−3 (`BoilerDoor` at (−64,−168)); crate ≈(176,−496), plate ≈(176,−384),
  backstop at tile (11,−23); Mikan ≈(0,−420); deviled-egg reward ≈(300,−500).
- **Tank** (x +40…+104): entrance gap x +60…+66; Scrambles ≈(1000,−400); tide-pool
  hardboiled egg ≈(1400,−500).
- **Gallery** (x +40…+104, south): entrance gap x +60…+66; GallerySign ≈(900,300);
  rubble block x +96…+104.
- **Hatch alcove** (x −104…−40, south): `TunnelHatch` KeyDoor at (−1408,168) (gap
  x −89…−87); lucky-yolk reward ≈(−1500,350); HatchNote ≈(−1300,450).
- `GatehouseArrival` at west edge (−1664,0); `CellPlacement` trigger ≈(−1500,0, 200×160);
  `CountTrigger` ≈(400,0, 200×130); Wiener Cop ≈(700,0).

---

## NPCs (fresh cast — rendered from .aseprite, all new)

| NPC | Scene | Sprite (assets/generated/characters/) | Position ≈ | Role |
|-----|-------|----------------------------------------|-----------|------|
| Officer Bacon | existing `FactoryOfficerBacon.tscn` | `officer_bacon.png` | (−1000, −80) pier | dock farewell (cutscene) |
| Mr Tea | NEW `MrTea.tscn` | `mr_tea.png` | (−300, −60) desk | gatehouse clerk — check-in ritual, flavor |
| Frank | existing `Frank.tscn` | `frank.png` | (−1300, −400) cell | cellmate — intro/conditional dialog |
| Mikan | NEW `Mikan.tscn` | `mikan.png` (scaled 1.5) | (0, −420) boiler room | boiler/tunnel rumor |
| Scrambles | NEW `Scrambles.tscn` | `scrambles.png` (scaled 1.6) | (1000, −400) Tank | damp-cell flavor, porridge warning |
| Wiener Cop | NEW `WienerCop.tscn` | `wiener_cop.png` | (700, 0) corridor | night guard, count warning |

The full roster (26 characters) is now renderable from `assets/characters/*.aseprite`
via the Python renderer in `tools/render_aseprite_cast.py` — future zones can use any
cast member (Apollo, Tamaki, Milk, Sunnyside Leader/Follower, …).

New dialog stays EarthBound-weird (mundane + surreal); no story facts beyond this doc.

---

## Required flags

| Flag | Set by | Gates |
|------|--------|-------|
| `met_bacon` | ArrivalCutscene trigger | arrival flavor |
| `met_tea` | CheckInCutscene | GatehouseExit, quest 1 |
| `met_frank` | CellPlacement | quest 2 |
| `met_mikan` / `met_scrambles` / `met_cop` | NPC triggers | quest flavor |
| `eggsile_count_survived` | CountTrigger | quest 3, Frank's post-count lines |
| `found_tunnel_key` | TunnelKeyPickup | TunnelHatch KeyDoor, quest 4 |
| `tunnel_opened` | TunnelRewardPickup | quest 5 (optional) |
| `boiler_gate_open` | BoilerPlate | BoilerDoor |
| `read_ledger` / `read_rules` / `read_stamp` / `read_scratch` / `read_cup` / `read_gallery` / `read_hatch` | ReadableObject GateFlag | quest flavor |
| `arrested`, `warp_eggsile_area1` | factory flow (never re-set) | — |

## Quest ("The Overflow", id `eggs_isle_overflow`, StartFlag `arrested`)

1. `meet_tea` — Get processed at the gatehouse. (`met_tea`)
2. `meet_frank` — Get placed in your cell and meet Frank. (`met_frank`)
3. `witness_count` — Witness the nine o'clock count. (`eggsile_count_survived`)
4. `find_tunnel_key` — Get the tunnel key from Frank. (`found_tunnel_key`)
5. `open_hatch` — OPTIONAL: Open the maintenance hatch. (`tunnel_opened`)

## New/changed components

- **`components/cutscene/CutsceneDirector.cs`** (NEW) — AnimationPlayer-driven cutscenes:
  `Say` (pauses the timeline until dialog advances), `FadeIn/FadeOut`, `SetFlag`,
  `LockPlayer/UnlockPlayer`, `PlacePlayer`, `HidePlayer/ShowPlayer`, `ShowLocation`.
  The cutscene scene frees itself on `AnimationFinished`.
- **`CutsceneTrigger.CutsceneScene`** (NEW export) — a `PackedScene` cutscene takes
  priority over `Cutscene` (resource runner) and `DialogLines`; instantiated under the
  current level, played via the Director.
- `FrankIntake.tres` — kept as a resource-runner dialog (conditional line selection is
  its job); all *cinematics* moved to AnimationPlayer scenes.
- `ItemDatabase`: `tunnel_key` added (Key category).
- No other new C# gameplay components — all beats use existing components.

## Verification plan

1. `dotnet build` — 0 errors.
2. `godot --headless --path . --script res://tests/VerifyEggsileFirstNight.cs` — all
   three maps (anchors, transitions, cutscene triggers, puzzle wiring, pickups, readables),
   the Overflow quest (5 objectives), item IDs.
3. `godot --headless --path . --script res://tests/VerifyAllLevels.cs` — 8 levels,
   15 transitions, 2 warps.
4. Live game log (EGGBERT_LOAD_STATE=post-arrest-eggsile): DockArrival → CheckIn →
   CellPlacement → CountEvent all play through the Director; pickups set their flags.
5. Regenerate maps: `godot --headless --path . --script res://tests/GenerateEggsIsleFirstNight.cs`
   (one-shot, committed scene files are the source of truth).
