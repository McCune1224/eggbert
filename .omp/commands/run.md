---
name: run
description: Launch Eggbert and inspect runtime output
---

1. Import and parse resources with `godot --headless --path . --editor --quit`.
2. Launch with `godot --path .` (use the hub-managed process for an interactive session).
3. Exercise the requested flow, then inspect Godot stdout/stderr and the newest `user://logs/eggbert_YYYY-MM-DD.log`.
4. For deterministic checks, run the relevant `tests/verify_*.gd` script directly with `godot --headless --path . --script`.

Report observed errors and the exact path/command used. Do not claim a pass from launch alone.
