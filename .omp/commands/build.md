---
name: build
description: Import and parse the Godot project, then report targeted verifier status
---

Run from the repository root:

```bash
godot --headless --path . --editor --quit
```

Then run the verifier named by the task, for example:

```bash
godot --headless --path . --script res://tests/verify_migration_integrity.gd
```

Report the exact command and diagnostics. Godot import plus targeted verifier scripts are the project's verification contract.
