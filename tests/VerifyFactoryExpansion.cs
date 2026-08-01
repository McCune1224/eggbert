using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// Headless verifier for the factory demo expansion.
//
// Asserts that AssemblyLine.tscn and ControlRoom.tscn exist, have correct
// root types, tilemaps, direct-root component names, transition wiring,
// flag connections, paired teleport pads, sequence puzzle behavior, and
// the optional eggdrop_soup pickup.
//
// Run with: godot --headless --path . --script res://tests/VerifyFactoryExpansion.cs

public partial class VerifyFactoryExpansion : SceneTree
{
    private int _failures;

    public override async void _Initialize()
    {
        GD.Print("[factory-expansion] Starting verification...");

        await VerifyAssemblyLine();
        await VerifyControlRoom();
        await VerifyTransitionWiring();
        await VerifySequencePuzzleBehavior();
        await VerifyEggdropSoupPickup();
        await VerifyRouteProducersAndSave();

        if (_failures == 0)
            GD.Print("[factory-expansion] ALL CHECKS PASSED");
        else
            GD.PrintErr($"[factory-expansion] {_failures} FAILURE(S)");

        System.Environment.Exit(_failures == 0 ? 0 : 1);
    }

    // -----------------------------------------------------------------------
    // AssemblyLine
    // -----------------------------------------------------------------------
    private async Task VerifyAssemblyLine()
    {
        const string scenePath = "res://levels/factory/maps/AssemblyLine.tscn";

        if (!FileAccess.FileExists(scenePath))
        {
            Fail($"[AssemblyLine] Scene file not found at {scenePath}");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            Fail($"[AssemblyLine] Could not load PackedScene from {scenePath}");
            return;
        }

        var root = scene.Instantiate() as Node2D;
        if (root == null)
        {
            Fail($"[AssemblyLine] Instantiated root is not Node2D");
            return;
        }

        // Root must be BaseLevel
        if (root is not BaseLevel)
            Fail($"[AssemblyLine] Root '{root.Name}' is not BaseLevel");

        GD.Print($"[AssemblyLine] Root type OK: {root.GetType().Name}");

        // Direct-root CoreTilemapLayer of type LevelTileMapLayer with non-empty used rect
        var tilemap = root.GetNodeOrNull<LevelTileMapLayer>("CoreTilemapLayer");
        if (tilemap == null)
            Fail($"[AssemblyLine] Missing direct-root CoreTilemapLayer of type LevelTileMapLayer");
        else
        {
            var usedRect = tilemap.GetUsedRect();
            if (usedRect.Size == Vector2I.Zero)
                Fail($"[AssemblyLine] CoreTilemapLayer has empty used rect");
            else
                GD.Print($"[AssemblyLine] CoreTilemapLayer used rect: {usedRect}");
        }

        // Direct-root arrivals
        AssertNode<LevelTransition>(root, "SortingFloorArrival", "[AssemblyLine]");
        AssertNode<LevelTransition>(root, "AssemblyLineExit", "[AssemblyLine]");

        AssertNode<StaticBody2D>(root, "ConveyorNorthWall", "[AssemblyLine]");
        AssertNode<StaticBody2D>(root, "ConveyorSouthWall", "[AssemblyLine]");
        AssertNode<StaticBody2D>(root, "TransferLaneBarrier", "[AssemblyLine]");
        // Save point
        AssertNode<SavePoint>(root, "AssemblyLineSavePoint", "[AssemblyLine]");

        // Cutscene trigger
        var checklist = root.GetNodeOrNull<CutsceneTrigger>("ShutdownChecklist");
        if (checklist != null)
        {
            if (checklist.Mode != TriggerMode.OnInteract || !checklist.Once || checklist.CutsceneId != "shutdown_checklist")
                Fail("[AssemblyLine] ShutdownChecklist trigger mode/once/id is incorrect");
            AssertExactStrings(checklist.SetFlagsOnFire, new[] { "factory_shutdown_checklist_signed" }, "[AssemblyLine] ShutdownChecklist flags");
            AssertExactStrings(checklist.DialogLines, new[]
            {
                "SHUTDOWN CHECKLIST: Clear the conveyor lane, inspect the controls, then report to Loading.",
                "Eggbert signs the checklist. The machines keep humming anyway."
            }, "[AssemblyLine] ShutdownChecklist dialog");
        }

        // Readables
        AssertNode<ReadableObject>(root, "ConveyorInstruction", "[AssemblyLine]");
        AssertNode<ReadableObject>(root, "MaintenanceTransferInstruction", "[AssemblyLine]");

        // Conveyor tiles
        for (int i = 1; i <= 12; i++)
        {
            var conveyor = AssertNode<ConveyorTile>(root, $"Conveyor{i:00}", "[AssemblyLine]");
            if (conveyor != null && (conveyor.ConveyorDirection != Vector2.Left || Math.Abs(conveyor.ConveyorSpeed - 80f) > 0.001f))
                Fail($"[AssemblyLine] Conveyor{i:00} direction/speed is {conveyor.ConveyorDirection}/{conveyor.ConveyorSpeed}, expected (-1,0)/80");
        }

        // Teleport pads
        var padWest = AssertNode<TeleportPad>(root, "MaintenancePadWest", "[AssemblyLine]");
        var padEast = AssertNode<TeleportPad>(root, "MaintenancePadEast", "[AssemblyLine]");

        if (padWest != null && padEast != null)
        {
            var westTarget = padWest.GetNodeOrNull<TeleportPad>(padWest.TargetPadPath);
            var eastTarget = padEast.GetNodeOrNull<TeleportPad>(padEast.TargetPadPath);
            if (westTarget != padEast)
                Fail("[AssemblyLine] MaintenancePadWest.TargetPadPath does not resolve to MaintenancePadEast");
            if (eastTarget != padWest)
                Fail("[AssemblyLine] MaintenancePadEast.TargetPadPath does not resolve to MaintenancePadWest");
            if (Math.Abs(padWest.CooldownSeconds - 0.5f) > 0.001f || Math.Abs(padEast.CooldownSeconds - 0.5f) > 0.001f)
                Fail("[AssemblyLine] TeleportPad cooldown must be 0.5 seconds on both pads");
            if (westTarget == padEast && eastTarget == padWest)
                GD.Print("[AssemblyLine] TeleportPad pair resolves bidirectionally");
        }

        // Clean up
        root.Free();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }

    // -----------------------------------------------------------------------
    // ControlRoom
    // -----------------------------------------------------------------------
    private async Task VerifyControlRoom()
    {
        const string scenePath = "res://levels/factory/maps/ControlRoom.tscn";

        if (!FileAccess.FileExists(scenePath))
        {
            Fail($"[ControlRoom] Scene file not found at {scenePath}");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            Fail($"[ControlRoom] Could not load PackedScene from {scenePath}");
            return;
        }

        var root = scene.Instantiate() as Node2D;
        if (root == null)
        {
            Fail($"[ControlRoom] Instantiated root is not Node2D");
            return;
        }

        if (root is not BaseLevel)
            Fail($"[ControlRoom] Root '{root.Name}' is not BaseLevel");

        GD.Print($"[ControlRoom] Root type OK: {root.GetType().Name}");

        var tilemap = root.GetNodeOrNull<LevelTileMapLayer>("CoreTilemapLayer");
        if (tilemap == null)
            Fail($"[ControlRoom] Missing direct-root CoreTilemapLayer of type LevelTileMapLayer");
        else
        {
            var usedRect = tilemap.GetUsedRect();
            if (usedRect.Size == Vector2I.Zero)
                Fail($"[ControlRoom] CoreTilemapLayer has empty used rect");
            else
                GD.Print($"[ControlRoom] CoreTilemapLayer used rect: {usedRect}");
        }

        // Direct-root arrivals
        AssertNode<LevelTransition>(root, "AssemblyLineArrival", "[ControlRoom]");
        AssertNode<LevelTransition>(root, "LoadingBayEntrance", "[ControlRoom]");

        // Save point
        AssertNode<SavePoint>(root, "ControlRoomSavePoint", "[ControlRoom]");

        // Inspection door
        AssertNode<Door>(root, "InspectionDoor", "[ControlRoom]");
        AssertNode<StaticBody2D>(root, "ControlCorridorNorthWall", "[ControlRoom]");
        AssertNode<StaticBody2D>(root, "ControlCorridorSouthWall", "[ControlRoom]");
        AssertNode<StaticBody2D>(root, "InspectionDoorNorthWall", "[ControlRoom]");
        AssertNode<StaticBody2D>(root, "ControlCorridorSouthEastWall", "[ControlRoom]");
        AssertNode<StaticBody2D>(root, "SoupAlcoveSouthWall", "[ControlRoom]");
        AssertNode<StaticBody2D>(root, "SoupAlcoveEastWall", "[ControlRoom]");
        AssertNode<StaticBody2D>(root, "InspectionDoorSouthWall", "[ControlRoom]");

        // Sequence plates
        var plateA = AssertNode<SequencePressurePlate>(root, "SequencePlateA", "[ControlRoom]");
        var plateB = AssertNode<SequencePressurePlate>(root, "SequencePlateB", "[ControlRoom]");
        var plateC = AssertNode<SequencePressurePlate>(root, "SequencePlateC", "[ControlRoom]");

        if (plateA != null && plateB != null && plateC != null)
        {
            if (plateA.SequenceIndex != 0)
                Fail($"[ControlRoom] SequencePlateA.SequenceIndex is {plateA.SequenceIndex}, expected 0");
            if (plateB.SequenceIndex != 1)
                Fail($"[ControlRoom] SequencePlateB.SequenceIndex is {plateB.SequenceIndex}, expected 1");
            if (plateC.SequenceIndex != 2)
                Fail($"[ControlRoom] SequencePlateC.SequenceIndex is {plateC.SequenceIndex}, expected 2");
            else
                GD.Print("[ControlRoom] SequencePlate indices OK: A=0, B=1, C=2");
        }

        // Sequence controller
        var controller = root.GetNodeOrNull<SequencePuzzleController>("SequenceController");
        if (controller == null)
            Fail($"[ControlRoom] Missing direct-root SequencePuzzleController");
        else
        {
            GD.Print($"[ControlRoom] SequenceController found");
            if (controller.PlatePaths == null || controller.PlatePaths.Length != 3)
                Fail($"[ControlRoom] SequencePuzzleController.PlatePaths length is {controller.PlatePaths?.Length ?? 0}, expected 3");
            else
            {
                if (controller.PlatePaths[0].ToString() != "../SequencePlateA")
                    Fail($"[ControlRoom] PlatePaths[0] is '{controller.PlatePaths[0]}', expected '../SequencePlateA'");
                if (controller.PlatePaths[1].ToString() != "../SequencePlateB")
                    Fail($"[ControlRoom] PlatePaths[1] is '{controller.PlatePaths[1]}', expected '../SequencePlateB'");
                if (controller.PlatePaths[2].ToString() != "../SequencePlateC")
                    Fail($"[ControlRoom] PlatePaths[2] is '{controller.PlatePaths[2]}', expected '../SequencePlateC'");
                else
                    GD.Print("[ControlRoom] PlatePaths order OK");
            }

            if (controller.TargetDoorPath == null || controller.TargetDoorPath.ToString() != "../InspectionDoor")
                Fail($"[ControlRoom] TargetDoorPath is '{controller.TargetDoorPath}', expected '../InspectionDoor'");
            else
                GD.Print("[ControlRoom] TargetDoorPath OK");

            if (Math.Abs(controller.TimeWindow - 5.0f) > 0.001f)
                Fail($"[ControlRoom] TimeWindow is {controller.TimeWindow}, expected 5.0");
            else
                GD.Print("[ControlRoom] TimeWindow OK");
        }

        // Readable
        AssertNode<ReadableObject>(root, "SequenceInstruction", "[ControlRoom]");

        var inspectionApproved = root.GetNodeOrNull<CutsceneTrigger>("InspectionApproved");
        if (inspectionApproved != null)
        {
            if (inspectionApproved.Mode != TriggerMode.OnEnter || !inspectionApproved.Once || inspectionApproved.CutsceneId != "shutdown_inspection_complete")
                Fail("[ControlRoom] InspectionApproved trigger mode/once/id is incorrect");
            AssertExactStrings(inspectionApproved.SetFlagsOnFire, new[] { "factory_shutdown_inspection_complete" }, "[ControlRoom] InspectionApproved flags");
            AssertExactStrings(inspectionApproved.DialogLines, new[]
            {
                "CONTROL PANEL: Safety sequence accepted.",
                "LOADING BAY: Late-shipment alert received."
            }, "[ControlRoom] InspectionApproved dialog");
        }
        AssertNode<CutsceneTrigger>(root, "InspectionApproved", "[ControlRoom]");

        // Optional eggdrop_soup pickup
        var soup = AssertNode<PickupItem>(root, "EggdropSoupPickup", "[ControlRoom]");
        if (soup != null)
        {
            if (soup.ItemId != "eggdrop_soup" || soup.Count != 1)
                Fail("[ControlRoom] EggdropSoupPickup item id/count is incorrect");
            AssertExactStrings(soup.SetFlag, Array.Empty<string>(), "[ControlRoom] EggdropSoupPickup flags");
            AssertExactStrings(soup.DialogLines, new[] { "Found Egg Drop Soup. Restores 25 HP." }, "[ControlRoom] EggdropSoupPickup dialog");
        }

        // Clean up
        root.Free();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }

    // -----------------------------------------------------------------------
    // Transition wiring across all scenes
    // -----------------------------------------------------------------------
    private async Task VerifyTransitionWiring()
    {
        AssertSceneNode<SavePoint>("res://levels/factory/maps/SortingFloor.tscn", "SortingFloorSavePoint", "Factory — Sorting Floor");
        AssertSceneNode<SavePoint>("res://levels/factory/maps/LoadingBay.tscn", "LoadingBaySavePoint", "Factory — Loading Bay");
        // SortingFloor → AssemblyLine (renamed from LoadingBayEntrance)
        await VerifyTransitionTarget(
            "res://levels/factory/maps/SortingFloor.tscn",
            "AssemblyLineEntrance",
            "res://levels/factory/maps/AssemblyLine.tscn",
            "SortingFloorArrival",
            "tutorial_crate_gate_open",
            "[SortingFloor→AssemblyLine]"
        );

        // LoadingBay → ControlRoom (renamed from SortingFloorReturn)
        await VerifyTransitionTarget(
            "res://levels/factory/maps/LoadingBay.tscn",
            "ControlRoomReturn",
            "res://levels/factory/maps/ControlRoom.tscn",
            "LoadingBayEntrance",
            "",
            "[LoadingBay→ControlRoom]"
        );

        // AssemblyLine → ControlRoom
        await VerifyTransitionTarget(
            "res://levels/factory/maps/AssemblyLine.tscn",
            "AssemblyLineExit",
            "res://levels/factory/maps/ControlRoom.tscn",
            "AssemblyLineArrival",
            "factory_shutdown_checklist_signed",
            "[AssemblyLine→ControlRoom]"
        );

        // ControlRoom → LoadingBay
        await VerifyTransitionTarget(
            "res://levels/factory/maps/ControlRoom.tscn",
            "LoadingBayEntrance",
            "res://levels/factory/maps/LoadingBay.tscn",
            "ControlRoomReturn",
            "factory_shutdown_inspection_complete",
            "[ControlRoom→LoadingBay]"
        );

        // OpeningZone → SortingFloor (unchanged)
        await VerifyTransitionTarget(
            "res://levels/factory/maps/OpeningZone.tscn",
            "SortingFloorEntrance",
            "res://levels/factory/maps/SortingFloor.tscn",
            "ClockOutReturn",
            "tutorial_clocked_out",
            "[OpeningZone→SortingFloor]"
        );
        await VerifyEveryLevelTransition();
    }
    private async Task VerifyEveryLevelTransition()
    {
        string[] scenePaths =
        {
            "res://levels/factory/maps/OpeningZone.tscn",
            "res://levels/factory/maps/SortingFloor.tscn",
            "res://levels/factory/maps/AssemblyLine.tscn",
            "res://levels/factory/maps/ControlRoom.tscn",
            "res://levels/factory/maps/LoadingBay.tscn"
        };

        foreach (string sourcePath in scenePaths)
        {
            if (!FileAccess.FileExists(sourcePath))
                continue;

            var packed = ResourceLoader.Load<PackedScene>(sourcePath);
            var root = packed?.Instantiate() as Node2D;
            if (root == null)
            {
                Fail($"[Transitions] Could not instantiate '{sourcePath}'");
                continue;
            }

            foreach (Node child in root.GetChildren())
            {
                if (child is not LevelTransition transition)
                    continue;
                if (transition.Size != 7)
                    Fail($"[Transitions] {sourcePath}:{transition.Name}.Size is {transition.Size}, expected 7");

                if (string.IsNullOrEmpty(transition.Level) || !FileAccess.FileExists(transition.Level))
                {
                    Fail($"[Transitions] {sourcePath}:{transition.Name} has non-loadable Level '{transition.Level}'");
                    continue;
                }

                var targetPacked = ResourceLoader.Load<PackedScene>(transition.Level);
                var targetRoot = targetPacked?.Instantiate() as Node2D;
                if (targetRoot == null)
                {
                    Fail($"[Transitions] {sourcePath}:{transition.Name} target '{transition.Level}' cannot instantiate");
                    continue;
                }

                var target = targetRoot.GetNodeOrNull<LevelTransition>(transition.TargetTransitionName);
                if (target == null)
                    Fail($"[Transitions] {sourcePath}:{transition.Name} target '{transition.Level}' lacks direct-root '{transition.TargetTransitionName}'");
                else
                    GD.Print($"[Transitions] {sourcePath}:{transition.Name} → {transition.Level}/{transition.TargetTransitionName} OK");

                targetRoot.Free();
            }

            root.Free();
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        }
    }

    private async Task VerifyTransitionTarget(
        string sourceScene, string transitionName,
        string expectedLevel, string expectedTarget,
        string expectedFlag, string label)
    {
        if (!FileAccess.FileExists(sourceScene))
        {
            Fail($"{label} Source scene not found: {sourceScene}");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(sourceScene);
        if (scene == null)
        {
            Fail($"{label} Could not load {sourceScene}");
            return;
        }

        var root = scene.Instantiate() as Node2D;
        if (root == null)
        {
            Fail($"{label} Could not instantiate {sourceScene}");
            return;
        }

        var transition = root.GetNodeOrNull<LevelTransition>(transitionName);
        if (transition == null)
        {
            Fail($"{label} Missing direct-root LevelTransition '{transitionName}'");
            root.Free();
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            return;
        }

        if (transition.Level != expectedLevel)
            Fail($"{label} {transitionName}.Level is '{transition.Level}', expected '{expectedLevel}'");

        if (transition.TargetTransitionName != expectedTarget)
            Fail($"{label} {transitionName}.TargetTransitionName is '{transition.TargetTransitionName}', expected '{expectedTarget}'");

        if (transition.RequiredFlag != expectedFlag)
            Fail($"{label} {transitionName}.RequiredFlag is '{transition.RequiredFlag}', expected '{expectedFlag}'");

        // Verify target transition exists in the target scene
        if (!string.IsNullOrEmpty(expectedLevel) && FileAccess.FileExists(expectedLevel))
        {
            var targetScene = ResourceLoader.Load<PackedScene>(expectedLevel);
            if (targetScene != null)
            {
                var targetRoot = targetScene.Instantiate() as Node2D;
                if (targetRoot != null)
                {
                    var targetTransition = targetRoot.GetNodeOrNull<LevelTransition>(expectedTarget);
                    if (targetTransition == null)
                        Fail($"{label} Target scene '{expectedLevel}' missing direct-root LevelTransition '{expectedTarget}'");
                    targetRoot.Free();
                }
            }
        }

        GD.Print($"{label} {transitionName} → {expectedLevel} / {expectedTarget} (flag='{expectedFlag}') OK");

        root.Free();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }

    // -----------------------------------------------------------------------
    // Sequence puzzle behavior
    // -----------------------------------------------------------------------
    private async Task VerifySequencePuzzleBehavior()
    {
        const string scenePath = "res://levels/factory/maps/ControlRoom.tscn";

        if (!FileAccess.FileExists(scenePath))
        {
            Fail($"[SequencePuzzle] ControlRoom scene not found — cannot test behavior");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            Fail($"[SequencePuzzle] Could not load ControlRoom scene");
            return;
        }

        var root = scene.Instantiate() as Node2D;
        if (root == null)
        {
            Fail($"[SequencePuzzle] Could not instantiate ControlRoom");
            return;
        }

        var testPlayer = Player.Instance;
        if (testPlayer != null)
            testPlayer.Position = new Vector2(-512, 256);
        foreach (string plateName in new[] { "SequencePlateA", "SequencePlateB", "SequencePlateC" })
            root.GetNode<SequencePressurePlate>(plateName).Monitoring = false;

        // Add to scene tree so _Ready runs
        Root.AddChild(root);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var door = root.GetNodeOrNull<Door>("InspectionDoor");
        var controller = root.GetNodeOrNull<SequencePuzzleController>("SequenceController");

        if (door == null)
        {
            Fail($"[SequencePuzzle] InspectionDoor not found in instantiated ControlRoom");
            root.Free();
            return;
        }

        if (controller == null)
        {
            Fail($"[SequencePuzzle] SequenceController not found in instantiated ControlRoom");
            root.Free();
            return;
        }

        // Door should start closed
        if (door.IsOpen)
            Fail($"[SequencePuzzle] InspectionDoor is open before any plate press (should be closed)");
        else
            GD.Print("[SequencePuzzle] InspectionDoor starts closed OK");

        // Step 1: press plate index 2 (wrong — should NOT open door)
        controller.StepPressed(2);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (door.IsOpen)
            Fail($"[SequencePuzzle] InspectionDoor opened after pressing plate 2 first (wrong order)");
        else
            GD.Print("[SequencePuzzle] Door stays closed after wrong plate (index 2) — OK");

        // Step 2: press 0, 1, 2 in order within time window
        controller.StepPressed(0);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        controller.StepPressed(1);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        controller.StepPressed(2);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        // Wait one more frame for the door's deferred collision update
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (!door.IsOpen)
            Fail($"[SequencePuzzle] InspectionDoor did not open after correct sequence A→B→C");
        else
            GD.Print("[SequencePuzzle] InspectionDoor opens after correct sequence — OK");

        root.Free();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }

    // -----------------------------------------------------------------------
    // Eggdrop soup pickup
    // -----------------------------------------------------------------------
    private async Task VerifyEggdropSoupPickup()
    {
        const string scenePath = "res://levels/factory/maps/ControlRoom.tscn";

        if (!FileAccess.FileExists(scenePath))
        {
            Fail($"[EggdropSoup] ControlRoom scene not found — cannot verify pickup");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            Fail($"[EggdropSoup] Could not load ControlRoom scene");
            return;
        }

        var root = scene.Instantiate() as Node2D;
        if (root == null)
        {
            Fail($"[EggdropSoup] Could not instantiate ControlRoom");
            return;
        }

        var pickup = root.GetNodeOrNull<PickupItem>("EggdropSoupPickup");
        if (pickup == null)
        {
            Fail($"[EggdropSoup] Missing direct-root PickupItem 'EggdropSoupPickup'");
            root.Free();
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            return;
        }

        if (pickup.ItemId != "eggdrop_soup")
            Fail($"[EggdropSoup] EggdropSoupPickup.ItemId is '{pickup.ItemId}', expected 'eggdrop_soup'");
        else
            GD.Print("[EggdropSoup] ItemId OK: eggdrop_soup");

        if (pickup.Count != 1)
            Fail($"[EggdropSoup] EggdropSoupPickup.Count is {pickup.Count}, expected 1");
        else
            GD.Print("[EggdropSoup] Count OK: 1");
        var pickupShape = pickup.GetNodeOrNull<CollisionShape2D>("CollisionShape2D")?.Shape as CircleShape2D;
        if (pickupShape == null || Math.Abs(pickupShape.Radius - 16f) > 0.001f)
            Fail("[EggdropSoup] CollisionShape2D must be a 16px-radius CircleShape2D");
        var pickupSprite = pickup.GetNodeOrNull<Sprite2D>("Sprite2D");
        if (pickupSprite?.Texture?.ResourcePath != "res://assets/items/sprites/item_sprite_0020.png")
            Fail($"[EggdropSoup] Sprite2D texture is '{pickupSprite?.Texture?.ResourcePath}', expected item_sprite_0020.png");

        root.Free();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }
    private async Task VerifyRouteProducersAndSave()
    {
        var previousFlags = WorldFlags.Instance.GetAllFlags();
        byte[] previousSave = null;
        if (FileAccess.FileExists("user://savegame.tres"))
        {
            var previousFile = FileAccess.Open("user://savegame.tres", FileAccess.ModeFlags.Read);
            previousSave = previousFile?.GetBuffer((long)previousFile.GetLength());
            previousFile?.Dispose();
        }

        Node2D control = null;
        try
        {
            WorldFlags.Instance.ClearAll();

        var assemblyScene = ResourceLoader.Load<PackedScene>("res://levels/factory/maps/AssemblyLine.tscn");
        var assembly = assemblyScene?.Instantiate() as Node2D;
        if (assembly == null)
        {
            Fail("[Route] Could not instantiate AssemblyLine for trigger integration");
            return;
        }

        Root.AddChild(assembly);
        if (Player.Instance != null)
            Player.Instance.Position = new Vector2(-448, 0);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var checklist = assembly.GetNode<CutsceneTrigger>("ShutdownChecklist");
        InvokeTriggerCallback(checklist, "OnBodyEntered", Player.Instance);
        InvokeTriggerCallback(checklist, "OnInteract");
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (!WorldFlags.Instance.HasFlag("factory_shutdown_checklist_signed"))
            Fail("[Route] ShutdownChecklist did not set factory_shutdown_checklist_signed");
        if (!WorldFlags.Instance.HasFlag("cutscene_shutdown_checklist"))
            Fail("[Route] ShutdownChecklist did not set cutscene_shutdown_checklist");
        else
            GD.Print("[Route] ShutdownChecklist produced both required flags");

        CutsceneController.Instance.Stop();
        assembly.Free();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var controlScene = ResourceLoader.Load<PackedScene>("res://levels/factory/maps/ControlRoom.tscn");
        control = controlScene?.Instantiate() as Node2D;
        if (control == null)
        {
            Fail("[Route] Could not instantiate ControlRoom for trigger integration");
            return;
        }

        Root.AddChild(control);
        if (Player.Instance != null)
            Player.Instance.Position = new Vector2(352, 0);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var inspectionApproved = control.GetNode<CutsceneTrigger>("InspectionApproved");
        InvokeTriggerCallback(inspectionApproved, "OnBodyEntered", Player.Instance);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (!WorldFlags.Instance.HasFlag("factory_shutdown_inspection_complete"))
            Fail("[Route] InspectionApproved did not set factory_shutdown_inspection_complete");
        if (!WorldFlags.Instance.HasFlag("cutscene_shutdown_inspection_complete"))
            Fail("[Route] InspectionApproved did not set cutscene_shutdown_inspection_complete");
        else
            GD.Print("[Route] InspectionApproved produced both required flags");

        const string controlRoomPath = "res://levels/factory/maps/ControlRoom.tscn";
        if (SaveManager.Instance == null)
        {
            Fail("[Route] SaveManager autoload is unavailable");
        }
        else
        {
            SaveManager.Instance.SaveGame(controlRoomPath, new Vector2(448, 128), "Factory — Control Room");
            var save = ResourceLoader.Load<SaveFile>("user://savegame.tres");
            if (save == null)
            {
                Fail("[Route] SavePoint integration did not create user://savegame.tres");
            }
            else
            {
                if (save.SavePointScenePath != controlRoomPath)
                    Fail($"[Route] Save scene is '{save.SavePointScenePath}', expected '{controlRoomPath}'");
                if (save.LocationName != "Factory — Control Room")
                    Fail($"[Route] Save location is '{save.LocationName}', expected 'Factory — Control Room'");

                if (!save.ComponentData.TryGetValue("world_flags", out var worldFlagsVariant))
                {
                    Fail("[Route] Save file omitted world_flags component data");
                }
                else
                {
                    var worldFlagsData = worldFlagsVariant;
                    if (!worldFlagsData.TryGetValue("flags", out var flagsVariant))
                        Fail("[Route] Save file omitted world_flags.flags data");
                    else if (!flagsVariant.AsGodotDictionary().ContainsKey("factory_shutdown_inspection_complete"))
                        Fail("[Route] Save file omitted factory_shutdown_inspection_complete");
                    else
                        GD.Print("[Route] Save file preserves Control Room completion flag");
                }
            }

        }

        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(control))
                control.Free();
            SaveManager.Instance?.DeleteSave();
            if (previousSave != null)
            {
                var restoreFile = FileAccess.Open("user://savegame.tres", FileAccess.ModeFlags.Write);
                restoreFile?.StoreBuffer(previousSave);
                restoreFile?.Dispose();
            }
            WorldFlags.Instance.ClearAll();
            foreach (var entry in previousFlags)
                WorldFlags.Instance.SetFlag(entry.Key, entry.Value);
        }
    }


    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private static void InvokeTriggerCallback(CutsceneTrigger trigger, string methodName, params object[] args)
    {
        var method = typeof(CutsceneTrigger).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(typeof(CutsceneTrigger).Name, methodName);
        method.Invoke(trigger, args);
    }

    private T AssertNode<T>(Node parent, string name, string label) where T : Node
    {
        var node = parent.GetNodeOrNull<T>(name);
        if (node == null)
            Fail($"{label} Missing direct-root node '{name}' of type {typeof(T).Name}");
        else
            GD.Print($"{label} Found '{name}' ({typeof(T).Name}) OK");
        return node;
    }

    private void AssertSceneNode<T>(string scenePath, string nodeName, string expectedLocation) where T : Node
    {
        if (!FileAccess.FileExists(scenePath))
        {
            Fail($"[Scene] Missing scene '{scenePath}' while checking '{nodeName}'");
            return;
        }

        var packed = ResourceLoader.Load<PackedScene>(scenePath);
        var root = packed?.Instantiate() as Node2D;
        var node = root?.GetNodeOrNull<T>(nodeName);
        if (node == null)
        {
            Fail($"[Scene] '{scenePath}' missing direct-root '{nodeName}' ({typeof(T).Name})");
        }
        else if (node is SavePoint savePoint && savePoint.LocationName != expectedLocation)
        {
            Fail($"[Scene] '{scenePath}:{nodeName}'.LocationName is '{savePoint.LocationName}', expected '{expectedLocation}'");
        }

        root?.Free();
    }

    private void AssertExactStrings(string[] actual, string[] expected, string label)
    {
        if (actual == null || !actual.SequenceEqual(expected))
            Fail($"{label} is [{string.Join(" | ", actual ?? Array.Empty<string>())}], expected [{string.Join(" | ", expected)}]");
    }

    private void Fail(string msg)
    {
        _failures++;
        GD.PrintErr(msg);
    }
}
