# DESIGN.md — Eggbert

Game design document. What we're building, what's decided, what's still open.

---

## Core Loop

```
Overworld (NPCs, quests, puzzles) ──→ Combat Arena ──→ Overworld
       ↑                              (dodge + counter)      │
       └──────────────────────────────────────────────────────┘
```

**Overworld**: Top-down, zero gravity. Talk to NPCs, get quests, solve puzzles, explore. Story-driven.

**Combat**: Dedicated arena screen. Bullet-hell dodge. Player has no direct attacks — proximity parry (J key) within a brief window deals damage. Items extend parry radius and parry damage.

---

## Overworld Systems (decided)

### Dialog
- **World-state branching**: NPC dialog lists change based on `WorldFlags` (e.g. `HasMetPlayer`, `BossDefeated`). Branching is implicit — the world state determines what NPCs say.
- **Choice menu**: Optional in-dialog choice menu for explicit player responses (pick from 2–4 options). Arrow keys + E to select. Selection sets a WorldFlag — the cutscene caller picks which flag based on the choice index. Not all dialogs use choices; the default path remains WorldFlags-only.

### Pause Menu (F1/Esc)
- **EarthBound-style**: Items · Status · Map · Save · Settings
- No Load button (that's main menu only).

### Fast Travel
- **Warp points**: Place `WarpPoint` nodes in levels. Touch one → unlocked. Menu lists unlocked warps → pick → fade → arrive.

### World Map
- **Pause menu region map**: Stylized region map panel in pause menu. No real-time player tracking.

### Audio
- **Ambient + UI**: Per-level ambient loops (attached to `BaseLevel`). UI/menu/interaction sounds via `AudioManager.PlaySfx()`.

### Traversal
- **Environmental puzzles**: Push blocks, floor switches, switch-gated doors.

---

## Core Systems (decided)

### World Flags
- Singleton autoload. `Godot.Collections.Dictionary<string, Variant>` (bool, int, string).
- Saved/loaded as part of `SaveResource`. Underpins dialog branching, quest progression, warp unlocks, map reveals.

### Save System
- **1 save slot** — `user://savegame.tres`
- **What's saved**: Player position + health + level path, WorldFlags, warp unlocks, inventory, quest state.
- **Save triggers**: Menu (OverworldMenu Save button) + auto-save on level transition.
- **Format**: Godot .tres via `ResourceSaver`. Each `ISavable` implementation writes its data into `SaveResource` sub-objects.

> **Status — 2026-07-06**: Player, WorldFlags, and Inventory all implement `ISavable` and persist via `SaveResource`. Warps persist implicitly through WorldFlags (`warp_<id>` flags). Quest state will ride on WorldFlags once quests exist. Player health field exists in `SaveDataPlayer` but is unused until `HealthComponent` lands.

### Settings
- Volume sliders (MUSIC, SFX) — buses already exist
- Text speed (Instant / Fast / Normal) — critical for dialog feel
- Fullscreen toggle
- Window scale (1x–4x) — 640×360 is tiny on modern screens
- **Skip**: Key rebinding

### Quests
- **No quest log**. WorldFlags drive everything. NPCs remember what happened, dialog changes accordingly. EarthBound/Undertale style — the world *is* the quest tracker.

### Inventory
- **Categories**: Key Items (story), Consumables (overworld healing), Equipment (1-2 slots, stat boosts)
- **EarthBound-style UI**: Categorized tabs in pause menu
- **Overworld only**: No item usage during combat (combat is dodge-only)

> **Status — 2026-07-06**: Framework shipped — `Item` resource (flat, one class for all categories), `ItemDatabase` static registry (4 test items), `Inventory` autoload (ISavable, persist group, seeds test items on new game), Items panel in OverworldMenu with Key/Consumables/Equipment tabs + description + Use button. **Effects deferred**: consumable Use is a no-op stub (no HP system), equipment stats are data-only (no stat system to apply to). Both unblock when `HealthComponent` lands. Add real items to `ItemDatabase.All`; add world pickups (Area2D layer 10) when content phase starts.

### Party
- **Solo protagonist**. No companions. Add only if story demands it.

### Cutscenes
- **Signal-chain controller**: `CutsceneController` singleton. In-game, no separate scene files.
- Queue of actions: `LockPlayer`, `UnlockPlayer`, `MoveNpc`, `MovePlayer`, `FaceDirection`, `PlayAnimation`, `CameraMove`, `SayDialog`, `Wait`, `SetFlag`, `Fade`, `PromptChoice`, `Stop`.
- **CutsceneTrigger node** (Area2D, `components/npcs/`): reusable component that eliminates per-NPC `_Input` + prompt boilerplate. `[Export] TriggerMode { OnInteract, OnEnter }`, `[Export] bool Once` + `CutsceneId` (dedup via `cutscene_<id>` WorldFlag). Emits `Triggered` signal; NPC connects and calls `CutsceneController.StartCutscene()`.
- **Stop()**: aborts the current cutscene after the in-progress action finishes.

### Main Menu
- New Game / Continue / Settings / Quit
- Continue loads the single save slot directly (no slot picker)

### Death / Game Over
- HP hits 0 → fade to black → "You collapsed..." message → respawn at last save location (or level entrance).
- HP restored to some fraction. No item/money loss.

---

## Combat System (decided, not yet built)

> **Status — 2026-07-09**: Phase 4 underway. `HealthComponent` (HP/damage Node) being built — unblocks consumable effects, equipment stats, combat HP, death/respawn. Parry mechanic replacing graze meter. Choice menu in dialog added. See Implementation Roadmap below.

### Player
- Movement: WASD (same as overworld), dash (Space), sprint (Shift)
- Offense: Proximity parry (J key) — press near enemy within a brief window to deal damage. Items extend parry radius and parry damage.
- Defense: Collision with bullets = damage (PlayerHitbox layer 8).
- Victory: Reduce enemy HP to 0.

### Enemy
- Attack patterns: bullet spreads, aimed shots, zone denial, laser beams
- State machine: idle → telegraph → attack → cooldown
- HP: depleted = win.

### Combat UI
- Minimal overlay: HP bar + enemy name as CanvasLayer

### Transition
- `EnterCombat(enemyData, arenaPath)` — touch enemy, random encounter, or scripted trigger.

---

## Implementation Roadmap

> **Checkpoint — 2026-07-09**: Phases 1–3 complete. Phase 4 underway — `HealthComponent` (HP/damage Node), dialog choice menu, parry mechanic, combat arena + entry flow.

### Phase 1 — Polish & consolidate ✅
- [x] Delete stale csproj backups, dead code
- [x] Fix SaveLoadManager error strings
- [x] Fix ComponentPromptCollision sprite cache + AlignLabelText
- [x] Migrate OfficerBacon to component pattern

### Phase 2 — Core infrastructure ✅
- [x] `WorldFlags` singleton — Dictionary<string, Variant>, ISavable, autoload, "persist" group
- [x] `CutsceneController` singleton — async action queue (LockPlayer, MoveNpc, SayDialog, Wait, SetFlag, Fade, UnlockPlayer)
- [x] Settings — volume sliders (MUSIC/SFX), fullscreen toggle, window scale (1x–4x). Persisted via ConfigFile.
- [x] Overworld menu — EarthBound layout. Map (warp list), Settings panels functional. Items/Status are placeholder buttons.
- [x] Save system — WorldFlags saved via ISavable. Single slot, auto-save on level transition.
- [x] Save expansion — inventory serialized via SaveDataInventory; warps already persist via WorldFlags

### Phase 3 — Overworld systems ✅
- [x] Dialog branching — WorldFlags-driven NPC dialog (GrandpaSmith, OfficerBacon)
- [x] Warp points — WarpPoint (Area2D + E prompt), WarpDatabase static registry, WorldFlags-backed unlocks
- [x] Region map — pause menu panel listing unlocked warps, click to warp
- [x] Audio — PlaySfx() one-shot, PlayAmbience/StopAmbience, per-level ambience on BaseLevel, meep.mp3 for UI
- [x] Environmental puzzles — PushBlock (CharacterBody2D, 90% auto-scale collision), FloorSwitch (Area2D, Pressed/Released + TargetDoorPath), Door (StaticBody2D, CallDeferred collision toggle)
- [x] Location banner — drops from top on level transition (FadeTransition.ShowLocation)
- [x] Inventory — key items, consumables, equipment tabs (OverworldMenu Items panel). Effects deferred until HealthComponent exists.

### Phase 4 — Combat + Dialog
- [ ] Dialog fixes (Reset soft-lock, null-sfx, text-speed, fast-forward)
- [ ] `ChoiceMenu` — in-dialog choice UI with arrow-key nav + cutscene action
- [ ] `HealthComponent` — reusable HP/damage Node (HP, Defense, signals)
- [ ] `Equipment` autoload — equip/unequip, stat application (MaxHP, Defense)
- [ ] Wire consumable Use + Equipment stats to HealthComponent
- [ ] `CombatArena` base scene — bounded box, camera
- [ ] `CombatHUD` — HP bar + enemy name CanvasLayer
- [ ] `ParryComponent` — proximity parry (J key), item radius/damage scaling
- [ ] Enemy attack pattern toolkit
- [ ] Enemy state machine
- [ ] `EnterCombat()` unified entry point + scene swap
- [ ] Win/lose flow + return to overworld

### Phase 5 — Game structure
- [ ] Main menu (New Game / Continue / Settings / Quit)
- [ ] Death / game over + respawn
- [ ] 3+ enemy types with distinct patterns
- [ ] 2+ combat arenas

### Phase 6 — Content & polish
- [ ] More level areas (courtyard, eggsile, prison are empty)
- [ ] Polish pass (screen shake, particles, juice)

---

## Open Design Questions

- **Inventory consumables**: What consumables exist? What do they do? *(Framework ready; effects blocked on HealthComponent — decide concrete items + heal values before Phase 4.)*
- **Equipment**: What stats does equipment affect? *(Data fields exist: attack/defense/speed. Decide which stats the combat system actually uses before Phase 4.)*
- **Difficulty**: HP scaling? Easy mode?
- **Story/Narrative**: What's the game about? Who is Eggbert?
- **Difficulty**: HP scaling? Easy mode?
- **Story/Narrative**: What's the game about? Who is Eggbert?

---

## Excluded (unless future need)

- Quest log UI
- Party/companion system
- Key rebinding
- Multiple save slots
- Combat item usage

---

## Constraints

- Godot 4.7 + C# only (GDScript is addons/ only)
- 640×360 resolution, canvas_items stretch
- Top-down, zero gravity
- No tests, no CI (yet)
- Save format: Godot .tres via ResourceSaver
