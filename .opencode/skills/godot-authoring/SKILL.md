---
name: godot-authoring
description: Rules for authoring Godot content — when to use the editor, .tres files, or C# code. Prevents serialization mistakes like broken sub-resources in hand-written .tres files.
---

## When to use the Godot editor vs. code vs. .tres

### Always use the Godot editor for:
- Creating `.tres` Resource files with **nested sub-resources** — Godot's sub-resource deserialization from hand-written `.tres` is unreliable. Use the Inspector to create Resources, then save as `.tres`.
- Placing nodes in scenes (use the MCP `godot_add_node` tool, which delegates to the editor)
- Setting up AnimationPlayers and sprite frames
- Configuring collision layers/masks (use `CollisionConfig` constants in code)

### Use C# code for:
- Dynamic scene construction at runtime
- Building simple Programmatic Resources (e.g., `new CutsceneResource()`)
- Game logic, signals, state machines
- Creating test/demo data

### Use hand-written `.tres` for:  
- Simple flat Resources with no sub-resources (scalar fields, arrays of primitives, ext_resource refs)
- Fallback: use `[Export]` fields on components instead (e.g., `DialogLines` on `CutsceneTrigger`)

### .tscn file format rules:
- `[ext_resource]` lines MUST appear before any `[node]` blocks
- `load_steps` should count: ext_resources + `[sub_resource]` blocks + 1 (the scene itself)
- When setting an exported property on an instanced node, the property name must match the C# `[Export]` name exactly

### Common Godot serialization gotchas:
- `Array[ExtResource("N")]` is NOT valid in .tres/.tscn — use `Array[Resource]` for typed resource arrays
- Sub-resources in arrays need `script = ExtResource("N")` to set the C# type
- UIDs in `uid://` format must be hash-generated (editor does this) — don't hand-craft them
