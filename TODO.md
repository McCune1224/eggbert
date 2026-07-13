# TODO — Eggbert

Curated index of work, sorted by priority. The **source of truth is GitHub Issues** — this file is a human-readable summary that links to issues. Read `AGENTS.md` for architecture & conventions, `DESIGN.md` for roadmap & open design questions.

**Issue tracker:** https://github.com/McCune1224/eggbert/issues

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
- DialogBubble (CanvasLayer-based), ChoiceMenu (CanvasLayer, wrap-around nav)

### Phase 5 — Game Structure + Overworld Systems
- Main menu (New Game/Continue/Settings/Quit)
- CutsceneController full rewrite: CutsceneResource + CutsceneStep + CutsceneCondition — fully data-driven, editor-authorable .tres cutscenes (#23)
- EarthBound-weird dialog for 3 working NPCs as CutsceneResources (#24)
- MonsieurCroissant + FactoryJamitor migrated to CutsceneTrigger pattern (#24)
- Fast travel wired: WarpPoints placed in 6 levels, stale courtyard path fixed, 6 warp destinations in DB (#25)
- Puzzle variants: FloorSwitch latching, TimedDoor, KeyDoor, MultiSwitchGate, SequencePuzzle (#26)
- Doc reconciliation: AGENTS.md stale entries fixed, DESIGN.md checkbox synced, DialogVoiceResource blip-duration consistent (#28)
- STORY.md created — EarthBound-weird tone locked, protagonist anchored (#9 partially resolved) (#28)

### Dev tooling + structure cleanup
- Level folder standardization — all areas now `maps/` + `npcs/` subfolders (#20)
- Combat component consolidation — `ParryComponent`/`Targeting`/`HealthComponent` moved to `combat/components/` (#20)
- `CutsceneTrigger` node — reusable Area2D, eliminates per-NPC `_Input` + prompt boilerplate (#21)
- New `CutsceneAction` types (superseded by CutsceneStep rewrite in #23)
- `CutsceneController.Wait()` fix, `Stop()` via `CancellationTokenSource` (#21)
- Migrated GrandpaSmith, OfficerBacon, Joe to CutsceneTrigger pattern (#21)
- Debug auto-start — `EGGBERT_SKIP_MENU=1` env var skips menu, loads last save (#22)

---

## 🟡 Blocked on Design Decisions

These need a human to decide before implementation.

| Issue | Title | What's needed |
|-------|-------|---------------|
| **#6** | Define concrete consumable items | Names, effects, heal values |
| **#7** | What do Attack/Defense/Speed stats affect? | Decide which stats combat uses |
| **#9** | Who is Eggbert? (story/narrative) | Full plot (tone + protagonist locked in STORY.md) |

---

## 🟢 Phase 6 — Content & Polish
- [ ] Fill Courtyard, Eggsile, Prison with tiles, NPCs, puzzles (#27)
- [ ] Particles: dust on landing, sparkle on parry, etc.

---

## 🔍 Notes
- **No branches, no PRs** — all work commits directly to `main` (see AGENTS.md)
- **No test project** — intentional, game is pre-alpha
- **No CI** — intentional
