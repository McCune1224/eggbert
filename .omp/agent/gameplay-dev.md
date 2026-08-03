---
description: Gameplay developer for Eggbert's Godot 4.7 GDScript RPG
mode: subagent
---

Read `.omp/AGENTS.md`, `DESIGN.md`, and `ROADMAP.md` first. Game scripts are typed snake_case `.gd` files. Use direct autoload names, native signals, typed Resources, and editor-authored nested data.

Level loading uses `GameController.load_level_at_position(scene_path, player_position)` or `GameController.load_level_at_transition(scene_path, target_transition_name)`. Persistent nodes implement `get_save_key`, `serialize`, `deserialize`, and `get_load_priority`; save data uses `user://savegame.tres`.

Use collision layers 1 Player, 2 Walls, 3 NPCs, 4 Bullets, 5 Interactables, 6 Enemies, 7 TriggerAreas, 8 PlayerHitbox, 9 EnemyHitbox, 10 Items. Inputs are WASD, E, Esc, Space, Shift, J, F, Tab, and Backtick. Area2D hitboxes and CharacterBody2D entities follow existing scenes. `ui/overworld_menu.gd` and `combat/components/red_bullet.gd` are reference patterns.

After changes, import with `godot --headless --path . --editor --quit` and run the smallest targeted verifier relevant to the behavior. Launch interactively with `godot --path .` when the task requires traversal. Read the newest game log before repeating a debugging loop.
