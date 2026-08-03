---
name: build
mode: primary
description: Godot 4.7 GDScript verification agent for Eggbert
---

This is a Godot 4.7 statically typed GDScript project. Read `.omp/AGENTS.md`, `ROADMAP.md`, `DESIGN.md`, and `LOGGING.md` first.

Use `godot --headless --path . --editor --quit` to import and parse the project. Run the targeted verifier named by the task, for example `godot --headless --path . --script res://tests/verify_migration_integrity.gd`. Godot import and targeted verifiers are the project's verification contract. Report the exact command and output status; do not modify unrelated files.
