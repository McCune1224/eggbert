# DESIGN.md — Eggbert

Game design document. What we're building, what's decided, what's still open.

> This file records settled design decisions. For feature status and objectives, see ROADMAP.md.

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

### Pause Menu (Esc)
- **EarthBound-style**: Items · Status · Map · Save · Settings
- Inventory and Equipment panels fully wired (Key Items, Consumables, Equipment tabs with Use/Equip buttons).

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
- **Categories**: Key Items (story), Consumables (overworld healing), Equipment (1–2 slots, stat boosts)
- **EarthBound-style UI**: Categorized tabs in pause menu
- **Overworld only**: No item usage during combat (combat is dodge-only)

### Party
- **Solo protagonist**. No companions. Add only if story demands it.

### Cutscenes
- **Resource-driven controller**: `CutsceneController` singleton. Cutscenes are authored as `CutsceneResource` (.tres) files containing an array of `CutsceneStep` resources with optional `CutsceneCondition` branching (FlagSet/FlagNotSet/ChoiceEquals). In-game, no separate scene files.
- Actions: `LockPlayer`, `UnlockPlayer`, `MoveNpc`, `MovePlayer`, `FaceDirection`, `PlayAnimation`, `CameraMove`, `SayDialog`, `Wait`, `SetFlag`, `Fade`, `PromptChoice`, `Stop`.
- **CutsceneTrigger node** (Area2D, `components/npcs/`): `[Export] CutsceneResource Cutscene`, `[Export] string[] DialogLines` (inline fallback), `[Export] TriggerMode { OnInteract, OnEnter }`, `[Export] bool Once` + `CutsceneId` (dedup via `cutscene_<id>` WorldFlag). Calls `CutsceneController.StartCutscene()` directly.
- **Stop()**: aborts the current cutscene after the in-progress action finishes.

### Main Menu
- New Game / Continue / Settings / Quit
- Continue loads the single save slot directly (no slot picker)

### Death / Game Over
- HP hits 0 → fade to black → "You collapsed..." message → respawn at last save location (or level entrance).
- HP restored to some fraction. No item/money loss.

---

## Combat System

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

## Open Design Questions

- **Inventory consumables**: What consumables exist? What do they do? *(Framework ready; effects wire through HealthComponent.Heal(). Need concrete item names + heal values.)*
- **Equipment**: What stats does equipment affect? *(Data fields exist: attack/defense/speed. MaxHP/Defense/ParryRadius/ParryDamage are wired; Attack and Speed are unused.)*
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
