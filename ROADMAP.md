# ROADMAP.md — Eggbert

Completed work and remaining objectives for the Godot 4.7 GDScript port. A checked item describes an implemented system; a path points to its snake_case script or scene.

## Dialog system

- ✅ Dialog bubble text speed, fast-forward, and skip — `ui/dialog_bubble.gd`
- ✅ Choice menu (arrow keys + E, 2–4 options) — `ui/choice_menu.gd`
- ✅ WorldFlags-driven branching — `autoload/cutscene_controller.gd`, `autoload/world_flags.gd`
- ✅ Per-NPC voice resource with procedural fallback — `resources/dialog/dialog_voice_resource.gd`
- ✅ Area2D cutscene triggers with OnInteract/OnEnter — `components/npcs/cutscene_trigger.gd`

## Combat

- ✅ Bullet-hell arena — `combat/arenas/combat_arena.gd`, Oatmeal and Generic arenas
- ✅ Proximity parry (J, radius/damage scaling) — `combat/components/parry_component.gd`
- ✅ HP, damage, and defense — `components/core/health_component.gd`
- ✅ Enemy idle → telegraph → attack → cooldown state machine
- ✅ Four CombatOatmeal attack flavors (spread, burst, homing, aimed)
- ✅ Reactive combat HUD — `ui/combat_hud.gd`
- ✅ Enter, win, loss, and overworld-return flow — `autoload/combat_controller.gd`
- ⬜ More enemy types with distinct patterns
- ⬜ Define Attack and Speed equipment effects

## Overworld

- ✅ WASD movement, dash, sprint — `autoload/player/player.gd`
- ✅ Level replacement and camera bounds — `autoload/game_controller.gd`
- ✅ Main menu (New Game/Continue/Settings/Quit) — `ui/main_menu.gd`
- ✅ Pause menu (Items/Equipment/Map/Settings/Save) — `ui/overworld_menu.gd`
- ✅ Fast travel through WarpPoints and transitions
- ✅ Resource-driven cutscenes and dialog branches
- ✅ Location banner on transition
- ⬜ Fill Courtyard, Eggsile, and Prison with final tiles, NPCs, and puzzles
- ⬜ Particle polish (dust, landing, parry sparkle)
- ⬜ Screen shake and other juice

## Items and quests

- ✅ HealthComponent with HP, Heal, signals, and Defense
- ✅ Equipment autoload and stat application (MaxHP, Defense, ParryRadius, ParryDamage)
- ✅ Item Resource and ItemDatabase — `components/items/item.gd`, `item_database.gd`
- ✅ Inventory persistence and item panels
- ✅ Editor-authored ordered quest objectives and objective pinning
- ⬜ Define concrete consumables and effects

## Save system

- ✅ Persist-group contract and automatic save on transition — `saves/save_manager.gd`
- ✅ Fresh-save handling for invalid or legacy resources
- ✅ Save keys `player`, `inventory`, `equipment`, and `world_flags`

## Audio and presentation

- ✅ Per-level ambient loops through BaseLevel
- ✅ Dialog voice chirps with optional `.ogg` clips
- ✅ 640×360 canvas-items display and pixel-art theme

## Story

- ⬜ Full narrative setting and conflict — tone and protagonist are locked in `STORY.md`

## Verification and authoring

- ✅ Headless migration-integrity and targeted verifiers under `tests/`
- ✅ Editor-first nested Resource workflow and retained authoring addons
- ⬜ Expand deterministic coverage for future maps and combat encounters
