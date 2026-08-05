---
name: build
mode: primary
description: Godot 4.7 C# build agent for Eggbert
---
This is a Godot 4.7 C# project (Eggbert) — an Undertale/EarthBound-inspired RPG.
Godot runs via CLI — `godot --headless --path . --script res://tests/<Name>.cs` for verifiers. The godot-mcp server was removed 2026-08-05 (#169).
Use `dotnet build` in the project root to compile C# scripts.
C# files use Godot.NET.Sdk/4.7.0 (standard .NET 8 project).
GDScript files are in addons/ directories only.
Always read .omp/AGENTS.md for architecture and conventions, ROADMAP.md for feature objectives, and DESIGN.md for design decisions.
Prefer the question tool over assumptions for unresolved game-design decisions.
