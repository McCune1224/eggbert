# Factory Opening Authoring Guide

The factory is the game's opening tutorial. Its job is to teach movement and interaction without treating the player as a beginner, then establish the inciting incident: **Eggbert is falsely identified as an egg-costumed murderer and arrested by Officer Bacon.** The victim, weapon, and actual perpetrator remain off-screen.

## Shipped flow

`MainMenu.OnNewGamePressed()` clears the single save and world flags, then loads `res://levels/factory/maps/OpeningZone.tscn` at `(0, 0)`.

| Beat | Scene content | Required result |
|---|---|---|
| Arrival | Factory floor, `Factory Gate` location banner | Player can move immediately with WASD. |
| Jamitor | `FactoryJamitor` at `(167, -58)` | `E` starts dialog that teaches WASD, Shift sprint, Space dash, and E interaction. It sets `met_jamitor`. |
| Crate | `TutorialPushBlock` at `(520, 160)` | The player pushes the crate aside to clear the route; it teaches physical-object interaction without a separate instruction panel. |
| Arrest | `ArrestCutscene` at `(580, 160)`, `OnEnter`, one-shot | Officer Bacon gives the false-identification accusation and sets `arrested`. |
| Handoff | `FactoryOpeningFlow` | The Eggs Isle exit is unavailable before `arrested`. When the arrest dialog closes, the flow unlocks `eggsile_area1` and loads `area1.tscn` at `HubArrival`. |

`levels/factory/FactoryOpeningFlow.cs` owns the mandatory post-arrest handoff. Keep the `ArrestCutscene`, `EggsileTransition`, and `HubArrival` node names stable unless the corresponding code is updated. The scene’s inline arrest lines are intentionally replaced by this flow so the narrative stays synchronized with the transfer.

## Create or rebuild the map in Godot

1. Open the project in Godot 4.7, then open `levels/factory/maps/OpeningZone.tscn`.
2. For a new map, create a `Node2D` root, attach `BaseLevel.cs`, set `LevelName`, and add the packaged `CoreTilemapLayer` scene as a direct child.
3. Assign `assets/tilemaps/factory_tileset.tres` in the TileSet editor. Paint the map in the Godot TileMap editor—never edit `tile_map_data`, atlas sub-resources, or generated UIDs by hand.
4. Paint a non-empty used rectangle of at least `1536 x 1024` world pixels. `CoreTilemapLayer` derives camera limits and its outside collision border from that rectangle.
5. Build a readable route rather than a maze:
   - open floor at the spawn, Jamitor, crate, arrest trigger, and exit;
   - solid machinery/walls that make the crate the obvious blockage;
   - a distinctive landmark at each beat (clock-out station, jam-cleaning cart, stalled conveyor, loading bay);
   - no hazards on the required tutorial path.
6. Save the scene, then run it from the editor and inspect the TileMap used rect, collision, and camera bounds.

The existing route is intentionally short. The player should see Jamitor before the crate and see the loading-bay route before encountering the arrest trigger.

## Author the tutorial NPC and dialog

Use `levels/factory/npcs/FactoryJamitor.tscn` as the reusable Jamitor scene.

1. Place one instance on open floor. Its root `StaticBody2D` blocks the player; its child `CutsceneTrigger` supplies the interaction prompt and dialog range.
2. Select `CutsceneTrigger` and use **Trigger > Mode = OnInteract**. Leave `Once` disabled so the instructions can be repeated.
3. Write dialog as short lines, one intent per line. The current sequence covers every input once:
   - `W A S D` — movement;
   - `Shift` — sprint;
   - `Space` — dash;
   - `E` — interaction.
4. Add `met_jamitor` to **Set Flags On Fire**. This is a fact flag; it should not be used as a substitute for collision gates.
5. Test from outside the trigger radius: the prompt must appear only when the player is nearby, E must open dialog, and each line must be skippable using the existing dialog controls.

For non-NPC readable scenery, use `ReadableObject`; do not overload Jamitor with sign text.

## Author the crate tutorial

The opening uses the packaged `components/puzzles/PushBlock.tscn`.

1. Place `TutorialPushBlock` across the only walkable line to the loading bay.
2. Leave `DirectionalMode` off unless the surrounding wall geometry needs to constrain it. The player pushes the block by walking into it; `Player` calls `PushBlock.TryPush()` from collision.
3. Ensure there is empty floor on the block’s destination side. A crate that cannot move is an invisible hard lock.
4. Keep the puzzle to one push. This opening teaches a vocabulary; it must not test spatial reasoning or require a reset.
5. Run the route in both directions. The crate must not let the player clip through a wall, strand the player, or block the return path.

If a later factory room needs a real stateful gate, pair a push block with the existing `WeightedPressurePlate` and `Door` scenes. Configure their exported node paths in the Inspector after placing both nodes, then test whether the gate remains open when the block is moved off the plate.

## Configure the arrest cutscene

The opening arrest is a `CutsceneTrigger` because it is a one-shot spatial event.

1. Place a `CutsceneTrigger` at the loading-bay exit and give it a `CollisionShape2D` covering the full route.
2. Set **Mode = OnEnter**, **Once = true**, and **CutsceneId = arrest**. The id persists as `cutscene_arrest`, preventing a replay after loading a save.
3. Add `arrested` to **Set Flags On Fire**. It is set before the dialog begins, so `FactoryOpeningFlow` can detect the completed event.
4. Do not reveal the murderer, victim, weapon, or motive here. Bacon only has a witness report identifying “the egg.”
5. For the shipped opening, keep the trigger named `ArrestCutscene`; `FactoryOpeningFlow` supplies the dialog and performs the scene handoff after `CutsceneFinished`.

Use inline `DialogLines` for a short one-character exchange. For a staged scene—movement, camera work, waits, multiple speakers, choices—create a `CutsceneResource` in the Godot Inspector and add `CutsceneStep` sub-resources there. Do not hand-author nested `.tres` resources: Godot must serialize their sub-resources and IDs. Available step types include `LockPlayer`, `MoveNpc`, `MovePlayer`, `FaceDirection`, `CameraMove`, `SayDialog`, `Wait`, `SetFlag`, `Fade`, `PromptChoice`, and `Stop`.

## Transitions, save, and warp behavior

- `EggsileTransition` remains the authored physical route to `res://levels/eggsile/maps/area1.tscn` / `HubArrival`.
- `FactoryOpeningFlow` assigns its `RequiredFlag` at runtime, preventing an early departure. It then transfers the player immediately after Bacon’s dialog, so the arrest feels like being taken away rather than a voluntary exit.
- The flow unlocks the registered `eggsile_area1` WarpPoint after the arrest. The factory’s existing `factory_gate` WarpPoint is not an opening requirement.
- `SavePoint` remains at the factory floor for editor traversal and recovery. A production save checkpoint must not permit a new player to bypass the arrest state.

When creating a new transition, set a loadable `Level` path and a `TargetTransitionName` that is a direct-root `LevelTransition` in the destination scene. The target node name is an API: rename it only after updating every source transition.

## Factory editing checklist

1. Open `OpeningZone.tscn` in Godot; verify no missing resource warnings.
2. Check the tilemap used rect, camera bounds, outer wall collision, and every open-floor placement.
3. Talk to Jamitor; verify the four controls and `met_jamitor` flag.
4. Push the crate aside from the intended approach and confirm the route stays traversable.
5. Attempt the Eggs Isle exit before the arrest; it must remain gated.
6. Enter the arrest trigger; verify it runs once, sets `arrested`, unlocks `eggsile_area1`, and transfers to Eggs Isle intake.
7. Launch a new game from the menu; it must load the factory, not the Overworld.
8. Run `dotnet build` from the repository root.

For generic map conventions, component placement, and transition graph checks, see [Level Authoring Workflow](level-authoring.md).
