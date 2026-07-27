# Eggbert Development Checkpoint

**Date:** 2026-07-18  
**Project:** Godot 4.7 Mono / C# RPG  
**Working tree:** Not committed. The repository contains many pre-existing and unrelated modifications; preserve them.

## Current stable state

The game now boots into the overworld and can transition through the repaired Courtyard and Prison paths without the previously reported scene-loading failures.

### Scene-loading fixes completed

- Fixed malformed `PatrolComponent` nodes in `levels/courtyard/maps/courtyard.tscn`.
  - `PatrolComponent.cs` is a script, not a `PackedScene`.
  - Courtyard patrol nodes now use `script = ExtResource("13_patrol")`.
- Fixed malformed `SleepingInmate` declaration in `levels/prison/maps/prison.tscn`.
  - It is now a normal `Area2D` with `SleepingNPC.cs` assigned through `script`.
- Converted hand-written dictionary-based `CutsceneResource.Steps` arrays into typed `CutsceneStep` subresources:
  - `levels/prison/npcs/WafflesCutscene.tres`
  - `levels/prison/npcs/MonsieurCroissantCutscene.tres`
  - `levels/shrine/npcs/SunnysideRevelationCutscene.tres`
  - `levels/beach/npcs/GreatBeyondFinaleCutscene.tres`

These changes fixed:

- `Unable to convert array index 0 from "Dictionary" to "Object"`
- `Scene instance is missing`
- `Required object "rp_child" is null`
- `GameController.LoadLevel` null reference cascades

## Character visual work completed

Added visible sprites to previously sprite-less character triggers:

- Courtyard Depths Gardener
- Eggsile Sewers Plumber
- Prison Sleeping Inmate

Corrected spritesheet frame configuration for:

- Courtyard Egguardo
- Eggsile Chef
- Eggsile Frank

Existing NPC scenes with sprites include Grandpa Smith, Officer Bacon, Joe, Frank, Factory Jamitor, Waffles, Monsieur Croissant, and Home's Eggatha/EJ.

## Console/logging cleanup completed

The console spam was traced to `PatrolComponent` resolving waypoint paths from the wrong base node.

- Changed Courtyard patrol paths from `../../Waypoint...` to `../Waypoint...`.
- Added per-waypoint warning throttling in `components/npcs/PatrolComponent.cs` so malformed paths cannot emit a warning every frame.

Expected normal output is now limited to intentional lifecycle messages such as level loading and `BaseLevel._Ready`.

## Placeholder visual work completed earlier

Standalone non-tilemap visuals were added and wired into sparse scenes/components:

- `assets/placeholders/room_backdrop.png`
- `assets/placeholders/door.png`
- `assets/placeholders/interaction_marker.png`
- `assets/placeholders/encounter_marker.png`
- SVG source files remain beside the PNG assets.
- `components/visuals/PlaceholderBackdrop.tscn`

Backdrops were added to Kitchen, Rec Room, Warden's Quarters, Secret Tunnels, Sunnyside Shrine, Solitary, and Home. Door, exit, and encounter markers were also wired.

## Verification completed

- `dotnet build` passes with 0 warnings and 0 errors.
- Direct headless launches succeeded for:
  - Courtyard
  - Courtyard Depths
  - Eggsile Sewers
  - Prison
- Overworld → Courtyard transition succeeded.
- Overworld → Prison transition succeeded.
- All four repaired cutscene resources load with expected step counts:
  - Waffles: 7
  - Monsieur Croissant: 7
  - Sunnyside Revelation: 12
  - Great Beyond Finale: 9
- Courtyard transition smoke output no longer contains repeated PatrolComponent warnings.

## Important working-tree guidance

Do not reset or discard the working tree. `git status` currently reports roughly 98 modified paths and 15 untracked paths, including broad unrelated gameplay, logging, UI, scene, and asset work from earlier sessions.

Before the next implementation session:

1. Read this checkpoint.
2. Read `.omp/AGENTS.md`, `ROADMAP.md`, and `DESIGN.md`.
3. Read the latest runtime log before diagnosing a new issue.
4. Preserve unrelated modifications byte-for-byte.
5. Run `dotnet build` and a representative headless scene smoke test after changes.

## Likely next work

- Continue visual audit in the running game, especially any remaining NPCs or encounter actors that appear blank.
- Inspect the console after traversing more story transitions; do not suppress new errors without tracing their source.
- Commit/push only after the current broad working-tree changes have been reviewed and grouped appropriately.
