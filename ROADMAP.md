# ROADMAP.md — Eggbert

Feature objectives by area. Issues are the source of truth; this is a human-readable view.
Dependencies are noted per item.

## Dialog system
✅ Dialog bubble with text speed, fast-forward, skip — DialogBubble.cs
✅ Choice menu (arrow keys + E, 2-4 options) — ChoiceMenu.cs
✅ WorldFlags-driven branching — CutsceneController + WorldFlags
✅ DialogVoiceResource per NPC, procedural fallback — resources/dialog/DialogVoiceResource.cs
✅ CutsceneTrigger (Area2D, OnInteract/OnEnter) — components/npcs/CutsceneTrigger.cs

## Combat
✅ Bullet-hell dodge arena — CombatArena base, OatmealArena, GenericArena
✅ Proximity parry (J key, radius/damage scaling) — ParryComponent
✅ HP/damage/Defense system — HealthComponent
✅ Enemy state machine (idle→telegraph→attack→cooldown) — CombatOatmeal
✅ 4 attack flavors (spread, burst, homing, aimed) — CombatOatmeal
✅ CombatHUD (reactive HP bars) — ui/CombatHUD.cs
✅ EnterCombat / win / lose flow — CombatController
⬜ More enemy types (3+ with distinct patterns) — #27 partially
⬜ Define Attack/Speed stat effects — #7

## Overworld
✅ WASD movement, dash (Space), sprint (Shift) — Player.cs
✅ Level loading/unloading + camera bounds — GameController
✅ Main menu (New Game/Continue/Settings/Quit) — MainMenu.cs
✅ Pause menu (Items/Equipment/Map/Settings/Save) — OverworldMenu.cs
✅ Fast travel via WarpPoints — levels/LevelTransition.cs
✅ CutsceneResource-driven cutscenes (13 actions) — CutsceneController
✅ Dialog with 3 working NPCs — #24
✅ Location banner on transition

## Puzzles
✅ PushBlock, FloorSwitch, Door (latching/timed) — components/puzzles/
✅ KeyDoor, MultiSwitchGate, SequencePuzzle — components/puzzles/
✅ Level transitions + Checkpoints — levels/

## Player & Equipment
✅ HealthComponent (HP, Heal, signals, Defense) — components/core/
✅ Equipment autoload (equip/unequip Weapon/Armor/Accessory) — Equipment.cs
✅ Stat application (MaxHP, Defense, ParryRadius, ParryDamage) — Equipment.cs wired
⬜ Define concrete consumable items (names, effects, heal values) — #6

## Inventory
✅ Item resource (flat class, categories) — components/items/Item.cs
✅ ItemDatabase static registry — components/items/ItemDatabase.cs
✅ Inventory autoload (ISavable, test items) — Inventory.cs
✅ Items panel with Key/Consumables/Equipment tabs — OverworldMenu.cs

## Save system
✅ ISavable interface, persist group, auto-save on transition — saves/
✅ Single slot, ResourceSaver → user://savegame.tres — SaveLoadManager
✅ Player position/health, WorldFlags, warp unlocks, inventory, equipment saved

## Audio
✅ Music cross-fade (2-player pool), PlaySfx, PlayAmbience — AudioManager
✅ Per-level ambient loops on BaseLevel
✅ Dialog voice chirp system (procedural + .ogg clips)

## Content
⬜ Fill Courtyard, Eggsile, Prison with tiles, NPCs, puzzles — #27
⬜ Particles (dust on landing, sparkle on parry, etc.)
⬜ Polish pass (screen shake, juice)

## Story
⬜ Full narrative (setting, conflict, plot) — #9 (tone + protagonist locked in STORY.md)

---

## Notes
- **No branches, no PRs** — all work commits directly to main
- **No test project** — intentional, game is pre-alpha
- **No CI** — intentional
