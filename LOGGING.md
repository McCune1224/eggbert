# Logging System — Eggbert

## Overview

`util/game_logger.gd` is the central structured logger for game-originated state changes. It is a `class_name` utility and is called directly by runtime systems; engine diagnostics stay in Godot's stdout/stderr.

The log is the primary audit trail for AI-assisted development: every gameplay session writes a dated file that an AI (or human) can read to reconstruct exactly what the player did — which autoloads loaded, which quests were active, what was interacted with, and how flags/state evolved.

## File output

- **Path:** `user://logs/eggbert_YYYY-MM-DD.log`
- **Format:** `[HH:mm:ss.fff] LEVEL [tag] message`
- **Source suffix:** omitted; GDScript does not provide the former compiler caller-file/line metadata.
- **Rotation:** daily files named with the local date; the five newest files are retained.
- **Implementation:** `Time.get_datetime_dict_from_system()`, `DirAccess`, and `FileAccess`.

## Control

| Environment variable | Values | Default | Effect |
|---|---|---|---|
| `EGGBERT_LOG_LEVEL` | `debug`, `info`, `warn`, `error`, `off` | `info` | Minimum level written |
| `EGGBERT_LOG_ECHO` | `1`, `0` | `1` | Mirror to Godot output; `0` is file-only |

Levels are ordered DEBUG, INFO, WARN, ERROR, and OFF. Tagged messages are preserved. There is no engine-error bridge; inspect Godot stdout/stderr for parser, scene, shader, and engine failures.

**AI workflow note:** run a session with `EGGBERT_LOG_LEVEL=debug godot --path .` to capture the full event trace (per-flag sets, damage ticks, branch steps, objective-tracker decisions). The default `info` level captures session-level state changes: boot manifest, quest status transitions, level loads, dialogs, combat outcomes, pickups, saves.

## Instrumentation map

The following tags and observables are part of the current logging contract:

- **Boot/startup:** `GameInit` logs the full autoload manifest at boot — every expected autoload (`WorldFlags`, `QuestManager`, `GameController`, `DialogManager`, `AudioManager`, `Player`, `FadeTransition`, `CutsceneController`, `DebugOverlay`, `SaveManager`, `Inventory`, `Equipment`, `CombatController`, `KeybindManager`, `FactoryOpeningFlow`) is confirmed loaded, and any missing autoload is logged as ERROR. This is the first thing to check when state "isn't showing".
- **Quests:** `QuestManager` logs quest registration at boot (ids + titles), validation failures, pin/unpin, and every status transition (LOCKED → ACTIVE → COMPLETED) with the current objective text. `ObjectiveTracker` logs why the HUD shows or hides (missing QuestManager API, no pinned quest, or the objective being displayed).
- **Core:** `GameController` level-load start/failure and missing transitions; `Combat` arena entry, spawns, damage, defeats, and outcomes; `SaveManager` save/load, persistent-node failures, and invalid-resource deletion; `BaseLevel` lifecycle; `LevelTransition` activation.
- **Player:** `Player` interaction checks, death, save/restore; `Camera` tilemap-bound failures.
- **Dialog/cutscene:** `Dialog`, `Cutscene`, `CutsceneTrigger`, `FirstBoot` sessions, choices, step errors, stop/cancel, and one-shot state. Dialog-branch traversal logs each node visited, flags set on enter, and choices taken.
- **Items/equipment:** `Inventory`, `Equipment`, `PickupItem`, `ConditionalItem`, and `TradeComponent` changes and warnings.
- **Flags:** `WorldFlags` set, clear, and reset operations (DEBUG level).
- **Health/combat:** `Health` damage, healing, and death (DEBUG for ticks, INFO for death); `CombatEnemy` defeats; `Parry`, `RollingEgg`, `CombatOatmeal`, `Crackpot`, and `CrackpotPuddle` state and damage events; `KeybindManager` binding changes.
- **Audio:** `Audio`, `AudioManager`, `ReverbZone`, `WindZone`, `ZoneStinger`, and `FootstepManager` playback and missing-resource diagnostics.
- **NPCs:** `InteractableArea`, `FleeComponent`, `PatrolComponent`, `SleepingNPC`, `ReadableObject`, `ComplaintComponent`, `RumorComponent`, and `PhoneBooth` interactions.
- **Puzzles:** `Door`, `TimedDoor`, `KeyDoor`, `FloorSwitch`, `TimedSpikes`, `PushBlock`, `ConveyorTile`, `MovingPlatform`, `SpikeTile`, `SequencePressurePlate`, `SequencePuzzle`, `MultiSwitchGate`, `LightMirror`, `LightSensor`, `TeleportPad`, `WeightedPressurePlate`, and `FakeWall` state changes.
- **Maps/UI:** `WarpPoint`, `LevelTileMapLayer`, `MainMenu`, `WarpDatabase`, `FadeTransition`, `ChoiceMenu`, `Settings`, `FontCache`, `DialogTagParser`, and `SavePoint` lifecycle and warnings.

Pure data resources and cosmetic nodes intentionally remain quiet: `SaveFile`, `CutsceneCondition`, `CutsceneResource`, `DialogVoiceResource`, `Item`, `CollisionConfig`, `DashGhost`, `HangingSign`, `Fps`, and `DebugOverlay` consumers.

## Quick diagnostics

Godot user-data paths differ by platform. Locate the `Eggbert/logs` directory, then use the platform's text search tools or read the file directly. A verbose run is:

```bash
EGGBERT_LOG_LEVEL=debug godot --path .
```

For file-only output:

```bash
EGGBERT_LOG_LEVEL=debug EGGBERT_LOG_ECHO=0 godot --path .
```

Always read the newest log before repeating a debugging loop; an engine error may exist only in Godot stdout/stderr.

## Reading a session log (AI-assisted development)

A typical `info`-level session reads like a story:

1. **Boot:** one `GameInit` line per autoload confirming it loaded — if a quest/flag system "isn't working", check for a `MISSING at boot` ERROR here first.
2. **Quest state:** `QuestManager` registration lines, then status transitions (`Quest 'factory_gate_shift_end' status -> ACTIVE`) with the objective text.
3. **Interaction:** `Player` check lines, `Dialog` start lines, `PickupItem` collections, `Inventory` additions.
4. **Progression:** `WorldFlags` sets (DEBUG), `ObjectiveTracker` showing new objectives, `GameController` level loads.

## Constants

| Symbol | File | Value |
|---|---|---|
| `MAX_LOG_FILES` | `util/game_logger.gd` | `5` |
| Log prefix | `util/game_logger.gd` | `eggbert_` |
| Log directory | `util/game_logger.gd` | `user://logs` |
| Initialization | `boot/game_init.gd` | logger configured from environment |
