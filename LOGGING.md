# Logging System — Eggbert

## Overview

`util/game_logger.gd` is the central structured logger for game-originated state changes. It is a `class_name` utility and is called directly by runtime systems; engine diagnostics stay in Godot's stdout/stderr.

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

## Instrumentation map

The following tags and observables are part of the current logging contract:

- **Core:** `GameController` level-load start/failure and missing transitions; `Combat` arena entry, spawns, damage, defeats, and outcomes; `SaveManager` save/load, persistent-node failures, and invalid-resource deletion; `GameInit` boot flow; `BaseLevel` lifecycle; `LevelTransition` activation.
- **Player:** `Player` interaction, death, save/restore; `Camera` tilemap-bound failures.
- **Dialog/cutscene:** `Dialog`, `Cutscene`, `CutsceneTrigger`, `FirstBoot` sessions, choices, step errors, stop/cancel, and one-shot state.
- **Items/equipment:** `Inventory`, `Equipment`, `PickupItem`, `ConditionalItem`, and `TradeComponent` changes and warnings.
- **Flags:** `WorldFlags` set, clear, and reset operations.
- **Health/combat:** `Health`, `Parry`, `RollingEgg`, `CombatOatmeal`, `Crackpot`, and `CrackpotPuddle` state and damage events; `KeybindManager` binding changes.
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

## Constants

| Symbol | File | Value |
|---|---|---|
| `MAX_LOG_FILES` | `util/game_logger.gd` | `5` |
| Log prefix | `util/game_logger.gd` | `eggbert_` |
| Log directory | `util/game_logger.gd` | `user://logs` |
| Initialization | `boot/game_init.gd` | logger configured from environment |
