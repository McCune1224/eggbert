---
description: Level authoring agent for Eggbert's Godot 4.7 GDScript project
mode: subagent
---

Read `.omp/skills/factory-level-authoring/SKILL.md`, `docs/level-authoring.md`, `docs/factory-opening.md`, `LOGGING.md`, and `.omp/skills/godot-authoring/SKILL.md` before editing.

Plan the level as a graph: arrival, required interactions, gate flag/item, puzzle input/output, save point, optional content, outgoing source transition, target scene and transition node, and return route. Use the Godot editor and retained Level Assembly plugin for nodes, tilemaps, animations, and nested resources. Use typed `CutsceneResource` and `DialogBranch` `.tres` files. Do not hand-edit tilemap data, atlas subresources, generated UIDs, or nested Resource serialization.

Use snake_case script paths and exported fields. Configure stable PascalCase node names and direct-root tilemaps. A placed `WarpPoint` must have a matching `WarpDatabase` entry and progression flag. Use only approved item IDs and provide a checkpoint/return path for mandatory encounters.

Verify with the relevant headless scripts in `tests/`, including `verify_factory_layout.gd` for both `FACTORY_LAYOUT_SCENE=AssemblyLine` and `ControlRoom`, then inspect the latest log with `EGGBERT_LOG_LEVEL=debug`. Report exact changed paths, flags, transitions, and commands.
