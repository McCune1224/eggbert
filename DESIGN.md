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

**Combat**: Dedicated arena screen. Bullet-hell dodge. Player has no direct attacks — near-miss dodging ("grazing") charges a meter. Full charge → auto-hit counter-attack depletes enemy HP.

---

## Overworld Systems (decided)

### Dialog
- **World-state branching**: No choice menu UI. NPC dialog lists change based on `WorldFlags` (e.g. `HasMetPlayer`, `BossDefeated`). Branching is implicit — the world state determines what NPCs say.

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

### Party
- **Solo protagonist**. No companions. Add only if story demands it.

### Cutscenes
- **Signal-chain controller**: `CutsceneController` singleton. In-game, no separate scene files.
- Queue of actions: `LockPlayer`, `MoveNpc`, `SayDialog`, `Wait`, `SetFlag`, `Fade`, `UnlockPlayer`.

### Main Menu
- New Game / Continue / Settings / Quit
- Continue loads the single save slot directly (no slot picker)

### Death / Game Over
- HP hits 0 → fade to black → "You collapsed..." message → respawn at last save location (or level entrance).
- HP restored to some fraction. No item/money loss.

---

## Combat System (decided, not yet built)

### Player
- Movement: WASD (same as overworld), dash (Space), sprint (Shift)
- Offense: Pure dodge. Near-miss grazing enemy bullets builds charge meter. Full charge → auto-hit counter-attack.
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
- [ ] Save expansion — serialize inventory + warps (WorldFlags saved, inventory/warp state pending)

### Phase 3 — Overworld systems ✅
- [x] Dialog branching — WorldFlags-driven NPC dialog (GrandpaSmith, OfficerBacon)
- [x] Warp points — WarpPoint (Area2D + E prompt), WarpDatabase static registry, WorldFlags-backed unlocks
- [x] Region map — pause menu panel listing unlocked warps, click to warp
- [x] Audio — PlaySfx() one-shot, PlayAmbience/StopAmbience, per-level ambience on BaseLevel, meep.mp3 for UI
- [x] Environmental puzzles — PushBlock (CharacterBody2D, 90% auto-scale collision), FloorSwitch (Area2D, Pressed/Released + TargetDoorPath), Door (StaticBody2D, CallDeferred collision toggle)
- [x] Location banner — drops from top on level transition (FadeTransition.ShowLocation)
- [ ] Inventory — key items, consumables, equipment tabs (Items panel is placeholder)

### Phase 4 — Combat
- [ ] `HealthComponent` — reusable HP/damage Node
- [ ] `CombatArena` base scene — bounded box, camera
- [ ] `CombatHUD` — HP bar + enemy name CanvasLayer
- [ ] `GrazeComponent` — near-miss hitbox + charge meter
- [ ] Enemy attack pattern toolkit
- [ ] Enemy state machine
- [ ] `EnterCombat()` unified entry point
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

- **Inventory consumables**: What consumables exist? What do they do?
- **Equipment**: What stats does equipment affect?
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
