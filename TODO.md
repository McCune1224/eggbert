# TODO — Eggbert

Curated index of work, sorted by priority. The **source of truth is GitHub Issues + the Project Board** — this file is a human-readable summary that links to issues. Read `AGENTS.md` for architecture & conventions, `DESIGN.md` for roadmap & open design questions.

**Issue tracker:** https://github.com/McCune1224/eggbert/issues

> **Project Board**: pending `gh auth refresh -s project` (interactive OAuth — see AGENTS.md note dated 2026-07-10).

---

## 🔴 Config / Dev Experience

- [ ] **Slash commands still broken?** — The `opencode.json` was fixed (instructions → prompt in agent blocks). If `/build` etc. still error, the provider may need restarting or the model may not support the `prompt` field being passed through. Try a different model or check opencode logs.
- [ ] **Find out what model/provider we're actually using** — No `model` or `provider` set in `.opencode/opencode.json`, so it relies on global config. If the model doesn't support custom system prompts, that could also cause the error. Consider pinning a known-good model.

---

## 🔴 Phase 4 — Combat + Dialog (in progress)

Audit on 2026-07-10 found most checklist items already done. Remaining work tracked as issues.

### Dialog system — ✅ done
- [x] **Dialog Reset soft-lock** — `DialogManager.Reset()` exists and clears state.
- [x] **null-sfx on dialog advance** — DialogVoice handles blips; `DialogManager.Reset()` clears dangling refs.
- [x] **Text speed** — Instant / Fast / Normal persisted via ConfigFile, read by TextBox.
- [x] **Fast-forward** — `advance_dialog` action edge-checked in `_Process`.
- [x] **ChoiceMenu** — code-based `Control`, wired into `CutsceneController.PromptChoice`.

### Health / Damage — ✅ done
- [x] **HealthComponent** — fully wired to Player, enemies, HUD, items, Equipment stats.
- [ ] **Death / respawn stub** — Phase 5 priority; needs HealthComponent first (now unblocked). *Not yet filed.*

### Equipment & Consumables — framework ✅, content pending
- [x] **Equipment autoload** — equip/unequip, stat application (MaxHP, Defense, Parry), save/load.
- [x] **Wire consumable Use** — `OverworldMenu.OnUsePressed` calls `HealthComponent.Heal`.
- [ ] **Define concrete consumables** — names, effects, heal values → **#6** (design)
- [ ] **Equipment stats — what do Attack/Speed affect?** → **#7** (design)

### Combat arena — mostly ✅, 3 items remain
- [x] **CombatArena base** — script + `OatmealArena.tscn` (4 enemy flavors).
- [x] **CombatHUD** — reactive HP bars, color thresholds.
- [x] **ParryComponent** — proximity parry, item stat scaling. *(Known bug → #4: uses hardcoded Key.J.)*
- [x] **Enemy attack pattern toolkit** — 4 patterns (Spread/Homing/Aimed/Burst).
- [x] **EnterCombat() + scene swap** — `CombatController`. *(Known bug → #1: lambda unsubscribe causes double-fire.)*
- [x] **Win/lose flow** — return to overworld, revive at 50%. *(Inherits #1.)*
- [ ] **Enemy state machine** — idle → telegraph → attack → cooldown FSM → **#2**
- [ ] **Wire GenericArena.tscn** with a CombatArena script → **#3**
- [ ] **Fix ParryComponent input action** — use `combat_parry` instead of `Key.J` → **#4**

### Docs
- [ ] **Update TODO.md to actual state** — this file is the start; full reconciliation → **#5**

---

## 🟡 Phase 5 — Game Structure (milestone #2)

- [ ] **Main menu** — New Game / Continue / Settings / Quit → **#8**
- [ ] **Death / game over + respawn** — fade → "You collapsed..." → respawn at save. *Not yet filed.*
- [ ] **3+ enemy types** — distinct attack patterns, not Oatmeal clones. *Not yet filed.*
- [ ] **2+ combat arenas** — different layouts, maybe hazards. *Not yet filed.*

---

## 🟢 Phase 6 — Content & Polish (milestone #3)

- [ ] **Courtyard** — empty, needs tiles, NPCs, puzzles.
- [ ] **Eggsile** — empty.
- [ ] **Prison** — empty.
- [ ] **Screen shake** — on hit, on parry, on death.
- [ ] **Particles** — dust on landing, sparkle on parry, etc.

---

## ❓ Open Design Decisions

These block content work. Decide before diving into Phase 5/6.

- **Who is Eggbert? What's the plot?** — drives NPC dialog, quests, level theming → **#9**
- **Consumable items** — names, effects, heal values? (Framework ready.) → **#6**
- **Equipment stats** — what does Attack/Defense/Speed actually affect in combat? → **#7**
- **Difficulty mode** — easy mode (half damage, double heals)? HP scaling? *Not yet filed.*

---

## 🔍 Known Weirdness

- **GODOT_PATH mismatch** — `opencode.json` says `/usr/local/bin/godot`, `AGENTS.md` says `/usr/lib/godot-mono/godot.linuxbsd.editor.x86_64.mono`. One of these is wrong. The MCP server won't start if the path is bad. Resolve before relying on Godot MCP tools.
- **SoundConfig.cs** — File exists at `components/core/SoundConfig.cs` but is wired to nothing. Dead code or future use?

---

## Issue index

| # | Title | Labels | Milestone |
|---|-------|--------|-----------|
| #1 | Fix CombatController lambda unsubscribe bug | bug, priority-high, phase-4 | Phase 4 |
| #2 | Enemy state machine — idle/telegraph/attack/cooldown | enhancement, priority-high, phase-4 | Phase 4 |
| #3 | Wire GenericArena.tscn with a CombatArena script | enhancement, priority-medium, phase-4 | Phase 4 |
| #4 | ParryComponent uses hardcoded Key.J | bug, priority-medium, phase-4 | Phase 4 |
| #5 | Update TODO.md to reflect actual Phase 4 state | documentation, priority-medium, phase-4 | Phase 4 |
| #6 | Design: Define concrete consumable items | design, priority-medium, phase-4 | Phase 4 |
| #7 | Design: What do Attack/Defense/Speed stats affect? | design, priority-medium, phase-5 | Phase 4 |
| #8 | Phase 5: Main menu scene | enhancement, priority-medium, phase-5 | Phase 5 |
| #9 | Design: Who is Eggbert? (story/narrative) | design, priority-low, phase-6 | Phase 6 |