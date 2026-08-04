---
name: new-level
description: Create a new Eggbert level from the Factory tutorial pattern. Usage: /new-level <level goal>
---
Create a new Eggbert level using the factory-level-authoring skill workflow. The first argument is the level goal (e.g., `/new-level a two-room area with a timed gate puzzle`).

## Execution order

1. **Inspect matching issue/reference level**: use `search_files`/`read_file` (or `glob`/`grep`/`read`) to find existing levels and components that match the goal. Read `docs/godot-editor-guide.md`, `docs/level-authoring.md`, `docs/factory-opening.md`, `LOGGING.md`, `skill://godot-authoring`, and `skill://godot-csharp-patterns`.
2. **Ask unresolved design questions**: use the `ask`/`clarify` question tool in one batch for any load-bearing choice that remains unresolved after research. Offer 2–4 concrete choices with a recommended default. Do not invent narrative, item, or tuning facts.
3. **Write an explicit room/flag/transition plan**: a compact table with arrival, mandatory interactions, gate flag/item, puzzle input and success output, save point, optional content, outgoing source transition, target scene, target direct-root node, and return route.
4. **Delegate/execute**: invoke the `level-author` subagent (`.omp/agent/level-author.md`) with the plan and all resolved design facts. The subagent follows the `factory-level-authoring` skill.
5. **Verify**: run the verification steps from the skill — static C# verifier (`tests/*.cs`, e.g. the `VerifyFactoryExpansion.cs` reference), transition pairs, save/reload, `dotnet build`, and `EGGBERT_LOG_LEVEL=debug` log inspection.

## Important

- The caller must supply approved narrative/reward/puzzle decisions, or the command will ask focused questions before proceeding.
- A scene skeleton is not a finished level. The command does not treat a skeleton as complete; it verifies every transition, interaction, and gate before reporting done.
- If no unresolved design questions remain after research, the command records "no questions needed" and proceeds.
