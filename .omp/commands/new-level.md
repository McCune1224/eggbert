---
name: new-level
description: Plan, author, and verify a complete Eggbert level
---

1. Inspect matching levels/components with `glob`, `grep`, and `read`. Read `docs/godot-editor-guide.md`, `docs/level-authoring.md`, `docs/factory-opening.md`, `LOGGING.md`, `.omp/skills/godot-authoring/SKILL.md`, and `.omp/skills/factory-level-authoring/SKILL.md`.
2. Ask focused design questions for unresolved narrative, rewards, flags, puzzle outcomes, or difficulty; do not invent load-bearing values.
3. Plan a room/flag/transition graph with arrival, mandatory interactions, gates, puzzle success, save point, optional content, outgoing transition, target node, and return route.
4. Author with the Godot editor and retained addons. Use typed GDScript scripts, Inspector-created `.tres` resources, stable node names, direct-root tilemaps, and snake_case exported fields. Do not hand-edit tilemap data, atlas subresources, nested Resources, or UIDs.
5. Verify structure and behavior with the targeted `tests/verify_*.gd` scripts, both transition directions, save/reload, and `EGGBERT_LOG_LEVEL=debug` log inspection. Use `godot --headless --path . --editor --quit` to import first.
6. Report exact changed files, flags, transitions, and observed verification results.

A scene skeleton is not a finished level: every interaction, gate, transition, and return path must be checked.
