# Logging System — Eggbert

## Overview

`util/GameLogger.cs` is the central structured logger. Every game state change routes through it. Future AI sessions: grep this doc or the log file to trace anything.

## File output

- **Path:** `user://logs/eggbert_YYYY-MM-DD.log`
- **Format:** `[HH:mm:ss.fff] LEVEL [tag] message`
- **Rotation:** Keeps last 5 files, auto-deletes oldest
- **Thread-safe:** Internal lock on file writes

## Control

| Env var | Values | Default | Effect |
|---------|--------|---------|--------|
| `EGGBERT_LOG_LEVEL` | debug, info, warn, error, off | info | Filter level |
| `EGGBERT_LOG_ECHO` | 1, 0 | 1 | Mirror to GD.Print (0 = file-only) |

## Engine error capture

`util/GameLogBridge.cs` — `Godot.Logger` subclass registered via `OS.AddLogger()`. Captures engine-level errors/warnings into file (no console echo). Tags: `Engine/Error`, `Engine/Warn`, `Engine/Script`, `Engine/Shader`, `Engine/Msg`.

## Instrumentation map

| Tag | What's logged | Level |
|-----|--------------|-------|
| `GameController` | Level load start (pos-based and transition-based) | INFO |
| `Combat` | Enter arena path, enemy defeats + remaining, parry success/miss, battle won, battle lost | INFO/DEBUG |
| `SaveLoad` | Save start, load start, per-node ISavable errors, load failure | INFO/ERROR |
| `Player` | Death → checkpoint reload, dash start/end, interaction start/end | INFO/DEBUG |
| `Dialog` | Start (line count), choices shown, choice selected, dialog reset | DEBUG |
| `Cutscene` | Start (resource path), per-step errors (MoveNpc/MovePlayer/PlayAnimation/CameraMove), stop/cancel | INFO/ERROR |
| `Inventory` | Add (id × count), remove (id × count), unknown-id errors | DEBUG/ERROR |
| `Equipment` | Equip (id → slot), unequip (id from slot) | INFO |
| `Health` | TakeDamage (raw, defense, net, resulting HP), heal, entity died, revived | DEBUG/INFO |
| `WorldFlags` | Flag set, flag cleared, all flags cleared | DEBUG/INFO |
| `Audio` | PlayMusic, PlayAmbience, StopAmbience | DEBUG |
| `LevelTransition` | Zone activation → target level (player-gated) | INFO |
| `GameInit` | Boot→MainMenu.tscn load failure | ERROR |
| `RollingEgg` | State transitions (Idle/Telegraph/Attacking/Cooldown/Stunned) | DEBUG |
| `CombatOatmeal` | State transitions (with flavor) | DEBUG |

## Quick AI-agent recipes

```bash
LOG_DIR=~/.local/share/godot/app_userdata/Eggbert/logs

# All errors across all sessions
grep ERROR $LOG_DIR/eggbert_*.log

# Combat trace (enter → hits → outcome)
grep -E '\[Combat\]' $LOG_DIR/eggbert_*.log

# Player lifecycle
grep -E '\[Player\]' $LOG_DIR/eggbert_*.log

# Full dialog flow
grep -E '\[Dialog\]' $LOG_DIR/eggbert_*.log

# Verbose run for debugging
EGGBERT_LOG_LEVEL=debug godot --path .
```

## Key constants

| Symbol | File | Value |
|--------|------|-------|
| `MaxLogFiles` | GameLogger.cs | 5 |
| `LogPrefix` | GameLogger.cs | `eggbert_` |
| Log dir | GameLogger.cs:35 | `user://logs` |
| Bridge registration | GameLogger.cs:43 | `OS.AddLogger(new GameLogBridge())` |
| Init call | GameInit.cs:14 | `GameLogger.InitializeFromEnv()` |
