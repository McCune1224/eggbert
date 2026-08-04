# Logging System — Eggbert

## Overview

`util/GameLogger.cs` is the central structured logger. Every game state change routes through it. Future AI sessions: grep this doc or the log file to trace anything.

## File output

- **Path:** `user://logs/eggbert_YYYY-MM-DD.log`
- **Format:** `[HH:mm:ss.fff] LEVEL [tag] message (source.cs:N)`
  - Source file + line appended by the compiler at every callsite — lets AI trace exactly where a log was emitted.
- **Rotation:** Keeps last 5 files, auto-deletes oldest
- **Thread-safe:** Internal lock on file writes

## JSONL mirror (agent-parseable) — since 2026-08-03

Every log line is also written as one JSON object per line to `user://logs/eggbert_YYYY-MM-DD.jsonl`:

```json
{"ts":"2026-08-03T23:20:46.785","level":"INFO","tag":"GameLogger","msg":"...","src":"GameLogger.cs:56","session":"ff8ae35de514"}
```

| Field | Meaning |
|-------|---------|
| `ts` | ISO-8601 timestamp (yyyy-MM-dd'T'HH:mm:ss.fff) |
| `level` | DEBUG / INFO / WARN / ERROR |
| `tag` | System tag (same as the `[tag]` in the text log) |
| `msg` | Log message |
| `src` | `File.cs:Line` of the callsite |
| `session` | Session id — `EGGBERT_SESSION_ID` env var if set, else a per-boot id |

Session ids let you trace one play session across the whole file (and correlate
with a verifier run: pass `EGGBERT_SESSION_ID=<id>` when launching).

### JSONL query recipes (agents)

```bash
LOG_DIR=~/.local/share/godot/app_userdata/Eggbert/logs

# All errors as JSON
grep '"level":"ERROR"' $LOG_DIR/eggbert_*.jsonl

# Everything one system emitted during one session
grep '"tag":"Combat"' $LOG_DIR/eggbert_*.jsonl | grep '"session":"<id>"'

# Latest session id (tail of the file)
tail -1 $LOG_DIR/eggbert_*.jsonl | python3 -c 'import json,sys; print(json.loads(sys.stdin.read())["session"])'

# Summary: error/warn counts per tag
python3 - "$LOG_DIR"/eggbert_*.jsonl <<'PY'
import json, sys, collections
c = collections.Counter(); sev = collections.Counter()
for f in sys.argv[1:]:
    for line in open(f):
        try: e = json.loads(line)
        except Exception: continue
        c[(e["tag"], e["level"])] += 1
for (tag, lvl), n in c.most_common(25): print(f"{lvl:5} {tag:20} {n}")
PY
```

## Diagnostics bundle (share with a future session)

`tools/collect-diagnostics.sh [godot-binary]` packages logs, `dotnet build`
result, every `tests/Verify*.cs` result, and git state into
`diagnostics/<stamp>/` with a `summary.json` manifest and a `PASTE_ME.md`
block you can paste into a fresh agent session. Run it before handing a bug
off:

```bash
tools/collect-diagnostics.sh ~/.local/opt/Godot_v4.7-stable_mono_linux_x86_64/Godot_v4.7-stable_mono_linux.x86_64
```

Requires Python 3 + `jq`-free (pure stdlib). The bundle is the feedback loop:
an agent in a future session reads `summary.json` + the JSONL logs and can
assess build/verifier/log state without re-running anything.

## Control

| Env var | Values | Default | Effect |
|---------|--------|---------|--------|
| `EGGBERT_LOG_LEVEL` | debug, info, warn, error, off | info | Filter level |
| `EGGBERT_LOG_ECHO` | 1, 0 | 1 | Mirror to GD.Print (0 = file-only) |
| `EGGBERT_SESSION_ID` | any string | per-boot id | Session id stamped on every JSONL entry |

## Engine error capture

`util/GameLogBridge.cs` — `Godot.Logger` subclass registered via `OS.AddLogger()`. Captures engine-level errors/warnings into file (no console echo). Tags: `Engine/Error`, `Engine/Warn`, `Engine/Script`, `Engine/Shader`, `Engine/Msg`.

## Instrumentation map

### Core systems
| Tag | What's logged | Level |
|-----|--------------|-------|
| `GameController` | Level load start (pos-based and transition-based), scene load failure, transition not found | INFO/ERROR |
| `Combat` | Enter arena path, arena setup, enemy spawns, enemy defeats + remaining, player death, battle won/lost, entity damage, arena cleanup | INFO/DEBUG |
| `SaveManager` / `SaveLoad` | Save start (location + scene), per-node ISavable errors, load start, load failure, old/corrupt save deletion, deserialization order | INFO/DEBUG/ERROR |
| `GameInit` | Boot→logger initialized, SKIP_MENU flow, MainMenu.tscn load, DialogLog init | INFO/ERROR/WARN |
| `BaseLevel` | Level loaded with music/ambience paths, level _ExitTree cleanup | INFO/DEBUG |
| `LevelTransition` | Zone activation → target level (player-gated) | INFO/ERROR |

### Player
| Tag | What's logged | Level |
|-----|--------------|-------|
| `Player` | Death → checkpoint reload, interaction start/end, serialize (pos/hp/scene), deserialize (keys/health/position/LoadLevel) | INFO/DEBUG/WARN |
| `Camera` | Tilemap bounds update failures (null/empty limits) | ERROR |

### Dialog & Cutscene
| Tag | What's logged | Level |
|-----|--------------|-------|
| `Dialog` | Start (line count), choices shown, choice selected, dialog reset, DialogBubble pages built/advanced, DialogLog toggle, ChoiceMenu selection | DEBUG/INFO |
| `Cutscene` | Start (resource path), per-step errors (MoveNpc/MovePlayer/PlayAnimation/CameraMove), stop/cancel | INFO/ERROR |
| `CutsceneTrigger` | Fire (cutscene/dialog/signal), Once lifecycle, skip-already-seen cleanup | INFO/DEBUG |
| `FirstBoot` | Text speed chosen, skip-due-to-flag | INFO/DEBUG |

### Items & Equipment
| Tag | What's logged | Level |
|-----|--------------|-------|
| `Inventory` | Add (id x count), remove (id x count), unknown-id errors | DEBUG/ERROR |
| `Equipment` | Equip (id → slot), unequip (id from slot) | INFO |
| `PickupItem` | Item picked up (id, count), flag set, dialog shown, destroyed, empty-id warning | INFO/DEBUG/WARN |
| `ConditionalItem` | Condition check result (visible/hidden), item picked up (id, count), dialog shown, destroyed, empty-id warning | INFO/DEBUG/WARN |
| `TradeComponent` | Trade outcome (missing item, success with reward, already completed flag) | INFO/DEBUG/WARN |

### World Flags
| Tag | What's logged | Level |
|-----|--------------|-------|
| `WorldFlags` | Flag set (key=value), flag cleared, all flags cleared | DEBUG/INFO |

### Health & Combat actions
| Tag | What's logged | Level |
|-----|--------------|-------|
| `Health` | TakeDamage (raw, defense, net, resulting HP), heal, entity died, revived | DEBUG/INFO |
| `Parry` | Parry success — bullet(s) reflected, parry miss — no valid targets | DEBUG |
| `RollingEgg` | State transitions (Idle/Telegraph/Attacking/Cooldown/Stunned) | DEBUG |
| `CombatOatmeal` | State transitions (with flavor) | DEBUG |
| `CheckableComponent` | Auto-created fallback collision shape | DEBUG |
| `KeybindManager` | Rebind action, reset to defaults, save/load bindings | INFO/DEBUG |
| `Crackpot` | Defeated, parried! | INFO/DEBUG |
| `CrackpotPuddle` | Spawned (damage, lifetime), damage ticks to player, despawned | DEBUG |

### Audio
| Tag | What's logged | Level |
|-----|--------------|-------|
| `Audio` | PlayMusic, PlayAmbience, StopAmbience | DEBUG |
| `AudioManager` | Music/ambience transitions | DEBUG |
| `ReverbZone` | Reverb added (wet/dry/room parameters), removed, player enter/exit | INFO/DEBUG |
| `WindZone` | Player enter/exit, wind fade in/out | DEBUG |
| `ZoneStinger` | Stinger playback, missing stinger warning | DEBUG/WARN |
| `FootstepManager` | Floor type detection changes, step playback, missing SFX warning, null player error | DEBUG/WARN/ERROR |

### NPCs
| Tag | What's logged | Level |
|-----|--------------|-------|
| `InteractableArea` | Player enter/exit range, interact triggered | DEBUG |
| `FleeComponent` | Flee start (player distance), caught (flag + dialog), reset, already-caught disabled | INFO/DEBUG |
| `PatrolComponent` | Waypoint reached (index), invalid waypoint warning, interaction pause/resume, missing parent | DEBUG/WARN/ERROR |
| `SleepingNPC` | Initial awake state, woken up (flag + dialog) | INFO/DEBUG |
| `ReadableObject` | Default vs alternate lines shown, already-read removal | DEBUG |
| `ComplaintComponent` | Complaint retrieved (NPC id, count, template) | DEBUG |
| `RumorComponent` | Rumor retrieved (NPC id, index, rumor text) | DEBUG |
| `PhoneBooth` | Call placed (intro/midgame/endgame), flag set | INFO |

### Puzzles
| Tag | What's logged | Level |
|-----|--------------|-------|
| `Door` | Open, close, toggle (with state), StartOpen on ready | INFO/DEBUG |
| `TimedDoor` | Opened (duration), blink start, auto-close | INFO/DEBUG |
| `KeyDoor` | Unlocked by flag, locked (missing flag) | INFO/DEBUG |
| `FloorSwitch` | Pressed (body entered), released (last body exited) | INFO |
| `TimedSpikes` | State transitions (Inactive→Telegraphing→Active→Inactive), player damaged | DEBUG |
| `PushBlock` | Push attempt (direction), success/failure | DEBUG |
| `ConveyorTile` | Body entered, direction/speed | DEBUG |
| `MovingPlatform` | Direction reversal at endpoints | DEBUG |
| `SpikeTile` | Player damaged, one-shot deactivation | DEBUG |
| `SequencePressurePlate` | Pressed (sequence index) | DEBUG |
| `SequencePuzzle` | Correct step progress, wrong order reset, completion | DEBUG/INFO |
| `MultiSwitchGate` | Evaluation result (AND/OR), door open/close, latch engaged | INFO/DEBUG |
| `ItemDatabase` | Item not-found warnings | WARN |
| `LightMirror` | Rotation change (degrees) | DEBUG |
| `LightSensor` | Beam enter/exit | DEBUG |
| `TeleportPad` | Player teleport (destination) | INFO |
| `WeightedPressurePlate` | Press/release (weight) | DEBUG |
| `FakeWall` | Revealed | DEBUG |

### Warp & Maps
| Tag | What's logged | Level |
|-----|--------------|-------|
| `WarpPoint` | _Ready (id, unlock state), unlocked | INFO/DEBUG |
| `LevelTileMapLayer` | Bounds sent to GameController | DEBUG |

### UI & Menus
| Tag | What's logged | Level |
|-----|--------------|-------|
| `MainMenu` | New game, continue, quit, volume/scale/fullscreen changes | INFO/DEBUG |
| `WarpDatabase` | Unlock warp, get unlocked count | INFO/DEBUG |
| `FadeTransition` | Location banner shown, duplicate instance freed | INFO/WARN |
| `ChoiceMenu` | Choices presented (count), choice selected (index + text) | INFO/DEBUG |
| `FirstBoot` | Text speed chosen, skip-due-to-flag | INFO/DEBUG |

### Settings & Save Points
| Tag | What's logged | Level |
|-----|--------------|-------|
| `Settings` | Load/save config values | DEBUG |
| `FontCache` | Static font cache — reports load failures | ERROR |
| `FlickeringLight` | Buzz SFX playback start | DEBUG |
| `DialogTagParser` | Malformed dialog tags (unclosed brackets, unknown tags, invalid values) | WARN |
| `SavePoint` | Save activation (location, scene, position, heal), missing SFX warning | INFO/DEBUG/WARN |


## Files without GameLogger (intentionally excluded)

These `.cs` files deliberately have no `GameLogger` calls. Each falls into one of the
categories below — adding logging would produce noise without actionable signal.

| File | Reason |
|------|--------|
| `util/GameLogger.cs`, `util/GameLogBridge.cs` | The logging infrastructure itself |
| `saves/ISavable.cs`, `saves/SaveFile.cs` | Interface and data-transfer class — no runtime logic |
| `resources/cutscene/CutsceneCondition.cs`, `CutsceneResource.cs` | Pure data resources — no runtime behavior |
| `resources/dialog/DialogVoiceResource.cs` | Pure data resource — no runtime behavior |
| `components/items/Item.cs` | Data class (fields only) — no runtime logic |
| `components/core/CollisionConfig.cs` | Static collision-layer constants only |
| `combat/components/Targeting.cs` | Single-line static helper (`CombatTargeter.GetPlayerPosition()`) |
| `autoload/player/animations/DashGhost.cs` | Purely visual ghost sprite — no game state |
| `components/world/HangingSign.cs` | Purely cosmetic sine-wave rotation — no state changes |
| `util/Fps.cs` | FPS counter utility — no game state |
| `autoload/DebugOverlay.cs` | F2 debug HUD — *consumer* of log data, not a producer |
| `ui/FontCache.cs` | Lazy-load cache with error logging built in |
| `ui/DialogTagParser.cs` | Stateless text transformation (malformed-tag warnings use GameLogger directly) |

## Quick AI-agent recipes

```bash
LOG_DIR=~/.local/share/godot/app_userdata/Eggbert/logs

# All errors across all sessions
grep ERROR $LOG_DIR/eggbert_*.log

# Combat trace (enter → hits → outcome)
grep -E '\[Combat\]' $LOG_DIR/eggbert_*.log

# Player lifecycle (movement, death, save, load)
grep -E '\[Player\]' $LOG_DIR/eggbert_*.log

# Full dialog flow (triggers, display, choices)
grep -E '\[Dialog\]' $LOG_DIR/eggbert_*.log

# Puzzle interactions (doors, switches, beams)
grep -E '\[(Door|Switch|Puzzle|Light)\]' $LOG_DIR/eggbert_*.log

# Item & equipment changes
grep -E '\[(Inventory|Equipment|Item)\]' $LOG_DIR/eggbert_*.log

# World flag changes
grep -E '\[WorldFlags\]' $LOG_DIR/eggbert_*.log

# Audio system (reverb, wind, footsteps, music)
grep -E '\[(Audio|Reverb|Wind|ZoneStinger|Footstep)\]' $LOG_DIR/eggbert_*.log

# NPC behavior
grep -E '\[(Flee|Patrol|NPC|Trade|Sleeping|Readable|Complaint|Rumor|Phone)\]' $LOG_DIR/eggbert_*.log

# Verbose run for debugging
EGGBERT_LOG_LEVEL=debug godot --path .
```

## Key constants

| Symbol | File | Value |
|--------|------|-------|
| `MaxLogFiles` | GameLogger.cs | 5 |
| `LogPrefix` | GameLogger.cs | `eggbert_` |
| Log dir | GameLogger.cs | `user://logs` |
| JSONL mirror | GameLogger.cs | `eggbert_YYYY-MM-DD.jsonl` beside the `.log` |
| Session id env | GameLogger.cs | `EGGBERT_SESSION_ID` (override, else per-boot) |
| Bridge registration | GameLogger.cs | `OS.AddLogger(new GameLogBridge())` |
| Init call | GameInit.cs | `GameLogger.InitializeFromEnv()` |
