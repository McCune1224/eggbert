---
name: read-logs-before-looping
description: "When a fix is applied and the user reports it's still broken, read game logs before any further code investigation"
condition: "still|nope|not fixed|doesn't work|still happening"
scope: "text"
---

When the user reports a fix didn't work, your **first action** is to read the game logs (`~/.local/share/godot/app_userdata/Eggbert/logs/eggbert_*.log`). Do NOT re-open edited files, do NOT grep for more code, do NOT reason in circles. The logs show runtime behavior — use them to find what's actually happening before touching any code.

After reading the logs, cite specific log lines that confirm or rule out your hypothesis. If the logs prove your fix worked (e.g., fade_in alpha=0), the bug is elsewhere — stop investigating that path.