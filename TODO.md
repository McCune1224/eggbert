# TODO — Eggbert

Curated index of work, sorted by priority. The **source of truth is GitHub Issues + the Project Board** — this file is a human-readable summary that links to issues. Read `AGENTS.md` for architecture & conventions, `DESIGN.md` for roadmap & open design questions.

**Issue tracker:** https://github.com/McCune1224/eggbert/issues
**Project Board:** https://github.com/users/McCune1224/projects/2

---

## ✅ Completed

### Phase 4 — Combat + Dialog (done)
- Dialog system: Reset, null-sfx, text speed, fast-forward, ChoiceMenu, DialogBubble, voice chirp overhaul
- DialogVoiceResource — Godot Resource with Inspector-exported pitch/volume/blip-duration per NPC
- HealthComponent: fully wired to Player, enemies, HUD, items, Equipment stats
- Equipment autoload: equip/unequip, stat application (MaxHP, Defense, Parry), save/load
- Consumable Use wired: `OverworldMenu.OnUsePressed` calls `HealthComponent.Heal`
- CombatArena base + OatmealArena + GenericArena wired
- CombatHUD: reactive HP bars with color thresholds
- ParryComponent: proximity parry, `combat_parry` input action, item stat scaling
- Enemy attack pattern toolkit: Spread/Homing/Aimed/Burst
- Enemy state machine: idle/telegraph/attack/cooldown on CombatOatmeal
- EnterCombat + Win/Lose flow, re-entry guard
- Lambda unsubscribe bug (CombatController) fixed
- TODO.md reconciliation started
- DialogBubble (CanvasLayer-based), ChoiceMenu (CanvasLayer, wrap-around nav), screen shake

### Phase 5 — Game Structure
- Main menu (New Game/Continue/Settings/Quit) — completed

### Dev tooling + structure cleanup (done on main)
- [x] Level folder standardization — all areas now `maps/` + `npcs/` subfolders (#20)
- [x] Combat component consolidation — `ParryComponent`/`Targeting`/`HealthComponent` moved to `combat/components/` (#20)
- [x] `CutsceneTrigger` node — reusable Area2D, eliminates per-NPC `_Input` + prompt boilerplate (#21)
- [x] New `CutsceneAction` types — `MovePlayer`, `FaceDirection`, `PlayAnimation`, `CameraMove`, `Stop` (#21)
- [x] `CutsceneController.Wait()` fix — uses `GetTree().CreateTimer` (respects pause) (#21)
- [x] `CutsceneController.Stop()` — abort via `CancellationTokenSource` (#21)
- [x] Migrated GrandpaSmith, OfficerBacon, Joe to CutsceneTrigger pattern (#21)
- [x] Debug auto-start — `EGGBERT_SKIP_MENU=1` env var skips menu, loads last save (#22)

---

## 🟡 Blocked on Design Decisions

These need a human to decide before implementation.

| Issue | Title | What's needed |
|-------|-------|---------------|
| **#6** | Define concrete consumable items | Names, effects, heal values |
| **#7** | What do Attack/Defense/Speed stats affect? | Decide which stats combat uses |
| **#9** | Who is Eggbert? (story/narrative) | Plot, setting, tone |

---

## 🟢 Phase 6 — Content & Polish
- [ ] Fill Courtyard, Eggsile, Prison with tiles, NPCs, puzzles (once story is decided)
- [ ] Particles: dust on landing, sparkle on parry, etc.

---

## 🔍 Notes
- **No test project** — intentional, game is pre-alpha
- **No CI** — intentional
