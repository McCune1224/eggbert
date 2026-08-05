# Artist Contributor Guide — Eggbert

A friendly, non-programmer-oriented guide to building levels and content in the Godot editor.
Every property mentioned here shows a **tooltip when you hover over it in the Inspector** — if you
ever wonder what a field does, hover it.

> Scope note: overworld content (levels, puzzles, NPCs, items, ambience) is designed to be
> editable here. Combat arenas, quest definitions, and custom logic are programmer-owned —
> you can place their triggers, but don't need to understand their internals.

---

## 1. Before you start

- Open the project in **Godot 4.7** (the Mono build — the plain download won't load C#).
- The game runs at **640×360** and tiles are **16px**. Snap positions to 16px when you can
  (hold `Ctrl` while dragging usually helps).
- **Save often.** Godot autosaves the open scene, but not new scenes.
- If you see a **yellow warning triangle** on a node in the Scene tree, the Inspector will
  explain what's misconfigured. Fix those before testing.

### How to add a puzzle/NPC/item to a level

1. Open the level scene (e.g. `levels/factory/maps/OpeningZone.tscn`).
2. Right-click the level root → **Add Child Node**, or use the **Level Assembly** dock
   (Project → Project Settings → Plugins → enable *Level Assembly*) which inserts
   preconfigured pieces.
3. Select the new node, fill in the Inspector fields, hover fields for help.
4. Press **F6** (or run the project) to test.

---

## 2. Level music & ambience

Every level has a `BaseLevel` root node. Select it and you'll find:

| Field | What it does |
|-------|--------------|
| `LevelName` | Shown in location banners. Leave empty to use the node name. |
| `LevelMusic` | The looping music track for the level. Drag an `.ogg`/`.wav` in. |
| `LevelAmbience` | A quiet looping background sound (wind, machines, rain). Stops when you leave. |

- Imported audio goes in `assets/audio/` (or anywhere under `assets/`).
- Music is cross-faded by the game automatically when you walk through a transition.

---

## 3. Wiring puzzles together

"Wiring" = telling a switch which door it opens. Most puzzle pieces have a
**NodePath field** (e.g. `TargetDoorPath`, `TargetPadPath`) that points at another node.
The editor draws a **colored line** from the switch/plate/sensor to its target door so
you can see the wiring at a glance.

**The golden rule: place BOTH nodes first, then set the paths.**

| Puzzle piece | Wire this field | Points at |
|--------------|-----------------|-----------|
| Floor Switch | `TargetDoorPath` | The `Door` it opens (yellow line) |
| Weighted Plate | `TargetDoorPath` | The `Door` it holds open (orange line) |
| Light Sensor | `TargetDoorPath` | The `Door` it unlocks (green line) |
| Teleport Pad | `TargetPadPath` | The pad you arrive at (blue line) |
| MultiSwitch Gate | `SwitchPaths` + `TargetDoorPath` | The switches it watches + the door |
| Sequence Puzzle | `SwitchSequence` + `TargetDoorPath` | Switches in press order + the door |
| Sequence Puzzle Controller | `PlatePaths` + `TargetDoorPath` | Plates in order + the door |

To set a NodePath: select the node, click the field's **target icon**, then click the
node in the Scene tree. Or drag a node from the tree onto the field.

### Common wiring mistakes

- Setting a path before the target node exists → it won't resolve. Set it after.
- Teleport pads need **two** pads pointing at each other (or the first leads nowhere).
- Sequence puzzles: plates must have **unique, ascending** `SequenceIndex` values (0, 1, 2…).
- `MultiSwitchGate` mode: **And** = all switches pressed, **Or** = any one.

---

## 4. Placing puzzles in the overworld

These are the pieces you'll place. All numeric fields have **sliders** with sensible ranges.

| Piece | Node type | What the player does | Key fields |
|-------|-----------|----------------------|------------|
| **Door** | StaticBody2D | Walks through when opened | `StartOpen`, `OpenSfx`, `CloseSfx` |
| **Key Door** | Door | Opens when it has the key flag | `RequiredFlag` (e.g. `has_cell_key`), `LockedMessage`, `UnlockJingle` |
| **Timed Door** | Door | Opens briefly, auto-closes | `OpenDuration` (seconds), `BlinkBeforeClose` |
| **Floor Switch** | Area2D | Steps on it | `TargetDoorPath`, `Latching` |
| **Push Block** | CharacterBody2D | Pushes it into switches | `PushSpeed`, `DirectionalMode` |
| **Teleport Pad** | Area2D | Walks onto it | `TargetPadPath`, `CooldownSeconds` |
| **Conveyor Tile** | Area2D | Gets pushed along | `ConveyorDirection`, `ConveyorSpeed` |
| **Moving Platform** | AnimatableBody2D | Rides it | `Speed` (needs a `move` animation) |
| **Timed Spikes** | Area2D | Times their cycle | `ActiveDuration`, `InactiveDuration`, `TelegraphDuration`, `Damage` |
| **Spike Tile** | Area2D | Touches it, takes damage | `Damage`, `OneShot` |
| **Weighted Plate** | Area2D | Stays pressed by a block | `TargetDoorPath`, `PushablePressedFlag` |
| **Fake Wall** | StaticBody2D | Walks through it | `RequireInteract`, `RevealDialogLines` |
| **Light Beam** | Node2D | — (automatic) | `BeamLength`, `BeamColor`, `BeamWidth` |
| **Light Mirror** | StaticBody2D | Rotates it with push | `MirrorTexture` |
| **Light Sensor** | Area2D | — (automatic) | `TargetDoorPath`, `ActiveColor` |

### Direction tips

- **Conveyors**: the arrow gizmo shows the push direction. Set `ConveyorDirection` to
  `(1,0)` = right, `(-1,0)` = left, `(0,1)` = down, `(0,-1)` = up.
- **Transitions**: `Side` says which screen edge the exit is on. The collider auto-sizes.
  Set `Level` to the target `.tscn` and `TargetTransitionName` to the destination node's name.
- **Don't place hazards on the player's spawn path** — no unfair hits.

---

## 5. NPC dialog & choices

NPCs use **triggers** (Area2D nodes) that fire dialog when the player interacts (press `E`)
or enters the area.

### Simple dialog (CutsceneTrigger)

Select the NPC's `CutsceneTrigger` node:

| Field | What it does |
|-------|--------------|
| `Mode` | `OnInteract` = press E to talk. `OnEnter` = auto-fires when you walk in. |
| `DialogLines` | The lines shown, one bubble at a time. |
| `ChoiceOptions` | Optional — 2+ options makes it a choice prompt. |
| `ChoiceResponses` | One response line per option, **same order**. |
| `Once` + `CutsceneId` | One-time dialog (won't repeat after you've seen it). |
| `SetFlagsOnFire` | Internal — story flags set when this fires. Ask a dev before adding new flags. |
| `Voice` (inherited) | This NPC's voice blips — see [Audio blips](#7-audio-blips). |

> The editor warns you if `ChoiceOptions` and `ChoiceResponses` don't match up.

### Branching dialog (DialogBranchTrigger)

For NPCs with real conversation trees (multiple branches, conditions, flags):

1. Create a branch: in the FileSystem dock, right-click → **Resource → New → DialogBranch**,
   save as `.tres`.
2. Open it and use the node-card inspector to add dialog nodes and responses.
3. Assign it to the NPC's `DialogBranchTrigger.DialogBranch` field.

### Readables, sleeping NPCs, quizzes

- **ReadableObject** (signs/books): `DialogLines` + optional `AlternateLines` when a
  `GateFlag` is set. `Once` makes it single-read.
- **SleepingNPC**: `WakeLines` (first wake), `AwakeLines` (after). `NpcId` keeps it
  consistent across saves.
- **QuizNpc**: build `QuizQuestion` resources (`Resource → New → QuizQuestion`) — each has
  prompt lines, options, and the index of the correct answer. Wire them into `Questions`,
  set `PassFlag`/`GrantItemId` for the reward. Questions asked in order; wrong answers
  reset with `FailLines`.

---

## 6. Items & pickups

| Piece | What it does | Key fields |
|-------|--------------|------------|
| **PickupItem** | Walk over it to collect | `ItemId` (e.g. `cell_key`), `Count`, `DialogLines`, `SetFlag` |
| **ConditionalItem** | Only appears after a story flag | `ItemId`, `RequiredFlag`, `RequiresNotSet` |

- `ItemId` must match an entry in `ItemDatabase` (dev-owned). If you need a new item,
  ask a dev to add it — the editor will warn you if the id is empty.
- `SetFlag` on a pickup is how "you got the key" progression works.

---

## 7. Audio blips

Every character's dialog voice is a **DialogVoiceResource** (shared by the NPC's `Voice`
field). Create one via `Resource → New → DialogVoiceResource`, save it, assign it, then
tune:

| Field | What it does |
|-------|--------------|
| `VoiceStream` | Optional real `.ogg` voice clip. Empty = procedural blips. |
| `SpeakerName` | Name shown in the dialog bubble. |
| `Portrait` | Optional face portrait texture. |
| `BasePitch` | Overall voice pitch (1.0 = normal). |
| `BlipDuration` | Length of each blip (0.08s default). |
| `VolumeDb` | Loudness. |
| `ConsonantPitchVariance` / `VolumeVariance` | How "alive" it sounds. |
| Vowel / Punctuation pitches | Per-letter and per-`?!.` pitch shapes — the voice's character. |

Give each named character their own voice resource — it's the cheapest way to make them
feel distinct.

---

## 8. Atmosphere & world dressing

| Piece | What it does | Key fields |
|-------|--------------|------------|
| **HangingSign** | Swinging sign (cosmetic) | `SwingSpeed`, `SwingAngle` |
| **FlickeringLight** | Flickering light + optional buzz | `MinEnergy`, `MaxEnergy`, `FlickerSpeed`, `BuzzSfx` |
| **AmbientParticles** | Dust, leaves, steam, bubbles… | `Preset` (pick one), `EmissionRate` |
| **WeatherSystem** | Timed rain events | `MinInterval`, `MaxInterval`, `RainDuration` |
| **Scuttler** | Small creature that runs when you get near | `TriggerRadius`, `ScuttleDistance`, `ScuttleSpeed` |
| **WindZone** | Wind ambience inside an area | `WindLoop`, `MaxVolume`, `FadeSeconds` |
| **ReverbZone** | Echo inside caves/rooms | `ReverbWet`, `ReverbDry`, `ReverbRoomSize` |
| **FootstepManager** | Floor-specific step sounds | One SFX per floor type, `StepInterval` |

---

## 9. Checking your work

1. **Look for yellow warnings** on nodes — the Inspector text tells you what's missing.
2. **Playtest the room**: F6 from the level scene, or run the project and walk through.
3. Verify every puzzle's happy path **and** failure path (e.g. wrong order resets).
4. Make sure a save point is nearby and reachable.
5. If something looks wrong, the game logs details to
   `user://logs/eggbert_YYYY-MM-DD.log` — a dev can read those.

### Good default habits

- Name nodes clearly (`Door_Cellar`, `Switch_Statue`) — paths stay readable.
- Keep puzzle pieces as direct children of the level root, not inside the tilemap.
- Don't move the tilemap; paint on it instead.
- When in doubt, hover the field. Every overworld component field now explains itself.
