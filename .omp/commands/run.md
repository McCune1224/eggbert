---
name: run
description: Launch the game and capture debug output
---
1. Run `dotnet build` to verify compilation.
2. Use `godot_run_project` (MCP) to launch the game.
3. Wait a few seconds, then call `godot_get_debug_output`.
4. Report any errors or warnings. If clean, confirm the game is running.
