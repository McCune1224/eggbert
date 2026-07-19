# Level Authoring Workflow

Use the **Level Assembly** dock in Godot to compose gameplay locations. It appears on the right after opening the project; the plugin is enabled in `project.godot`.

For the shipped opening’s beat-by-beat layout, dialog, puzzle, arrest, and handoff contract, see [Factory Opening Authoring Guide](factory-opening.md).

## 1. Start with a production map

1. Duplicate or create a map scene under `levels/<area>/maps/`.
2. Use a root `Node2D` with `BaseLevel.cs`.
3. Add one scripted `CoreTilemapLayer` or `TileMapLayer` at root-local `(0, 0)`.
4. Assign a valid TileSet in the TileSet editor. Paint a non-empty rect at least `1536×1024` world pixels. `LevelTileMapLayer` derives camera limits and the generated outer collision border from that used rect.

Do not hand-edit `tile_map_data`, TileSet atlas sub-resources, or UID values. Paint through Godot's TileMap editor and save the scene.

## 2. Paint readable traversal

- Keep arrivals, exits, NPCs, save points, encounters, and puzzle controls on visibly open floor.
- Give each route a landmark: a prop cluster, machinery, table, shrine feature, wall recess, or path material change.
- Put hazards on optional or clearly telegraphed routes. Never cover an arrival, required interaction, save point, or mandatory exit.
- Use the existing location tile resource. Reuse `stone-interior_tileset.tres` for stone interiors and `factory_tileset.tres` for factory spaces.

## 3. Add core components

Open **Level Assembly** and click a component button. The dock inserts the packaged scene at `(0, 0)`, selects it, and records the operation in Godot Undo/Redo. Move it into place, configure exported fields in the Inspector, then save.

| Component | Required setup |
|---|---|
| Level Transition | Set `Level` to a `res://…/*.tscn` path and `TargetTransitionName` to a direct-root `LevelTransition` in that destination scene. Set `Side` and `Size`. |
| Save Point | Set a descriptive `LocationName`; place one near every arrival/hub gate. |
| Door / Floor Switch | Set the switch `TargetDoorPath` after both nodes are placed. |
| Key Door | Set the existing `RequiredFlag` and its locked message. Do not invent progression flags without a design decision. |
| Teleport Pad | Add a pair and set each `TargetPadPath`. |
| Timed hazards | Keep an escape line; test timing with the player. |
| Cutscene Trigger | Use `DialogLines` for simple dialog or a `CutsceneResource` for sequencing. `ChoiceOptions` plus matching `ChoiceResponses` add flavor-only replies without flags. |

## 4. Wire transitions as a graph

- Prefer explicit `res://` scene paths over manually written `uid://` paths.
- Every source transition needs a loadable `Level` and a destination root child with the exact `TargetTransitionName`.
- Every hub destination must expose `HubArrival`, which returns to the matching Overworld gate.
- Keep a save point near each hub arrival and at the central Overworld hub.
- Avoid placing a destination inside another transition's collision area; the player is placed just beyond the destination according to its `Side`.

## 5. Add interaction without accidental progression

Reuse existing puzzle and encounter scenes. Add only behavior that has a defined purpose:

- **Puzzle shortcut:** creates an alternate route, not a dead-end gate.
- **Optional encounter:** use an existing arena and keep it outside the main path.
- **Hazard:** use existing spikes, conveyors, teleport pads, or moving platforms; do not add difficulty tuning without a decision.
- **Reward:** use only approved existing items. Consumable and equipment design remains a separate decision.

## 6. Validate before committing

1. Open the scene in Godot and inspect the TileMap used rect.
2. Run the scene from the editor. Confirm the `BaseLevel` loads with no missing-resource errors.
3. Traverse each arrival to every transition, save point, puzzle control, NPC, encounter, and exit.
4. Follow every transition both directions. Confirm the target scene loads and places the player outside the destination trigger.
5. Run `dotnet build` from the repository root.

For project-wide link changes, audit every `LevelTransition`: all `Level` values must load and all `TargetTransitionName` values must resolve to direct children of their target level root.
