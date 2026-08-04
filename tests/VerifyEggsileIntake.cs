using System;
using Godot;

/// <summary>
/// Headless verifier for the Eggs Isle intake beat (docs/eggsile-intake.md).
/// Asserts the intake scene structure, pruned orphans, gate flag wiring,
/// and the Kitchen entry transition. Run with:
///   godot --headless --path . --script res://tests/VerifyEggsileIntake.cs
/// </summary>
public partial class VerifyEggsileIntake : SceneTree
{
    private int _failures = 0;

    public override void _Initialize()
    {
        VerifyArea1();
        VerifyKitchenEntry();
        GD.Print(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} FAILURE(S)");
        System.Environment.Exit(_failures == 0 ? 0 : 1);
    }

    private void Fail(string msg)
    {
        _failures++;
        GD.Print("FAIL: " + msg);
    }

    private void Check(bool condition, string msg)
    {
        if (condition) GD.Print("OK: " + msg);
        else Fail(msg);
    }

    private void VerifyArea1()
    {
        const string path = "res://levels/eggsile/maps/area1.tscn";
        Check(FileAccess.FileExists(path), "area1.tscn exists");
        var scene = ResourceLoader.Load<PackedScene>(path);
        Check(scene != null, "area1.tscn loads");
        if (scene == null) return;

        var root = scene.Instantiate() as Node2D;
        Check(root != null, "area1 root is Node2D");
        if (root == null) return;
        Root.AddChild(root);

        Check(root is BaseLevel, "area1 root is BaseLevel");

        // Kept intake nodes
        Check(root.GetNodeOrNull("Frank") != null, "Frank present");
        Check(root.GetNodeOrNull("Joe") != null, "Joe present");
        Check(root.GetNodeOrNull("IntakeTowels") != null, "IntakeTowels present");
        Check(root.GetNodeOrNull("CellKeyPickup") != null, "CellKeyPickup present");
        Check(root.GetNodeOrNull("WarpPoint") != null, "WarpPoint present");
        Check(root.GetNodeOrNull("HubArrival") != null, "HubArrival present");

        // Towels present and gated
        for (int i = 1; i <= 3; i++)
        {
            var towel = root.GetNodeOrNull<CutsceneTrigger>($"Towel{i}");
            Check(towel != null, $"Towel{i} present");
            if (towel != null)
            {
                Check(towel.Mode == TriggerMode.OnEnter, $"Towel{i} is OnEnter");
                Check(towel.Once, $"Towel{i} is one-shot");
                Check(towel.CutsceneId == $"towel_{i}", $"Towel{i} CutsceneId correct");
            }
        }

        // Pruned orphans
        Check(root.GetNodeOrNull("SeqSwitch1") == null, "SeqSwitch1 pruned");
        Check(root.GetNodeOrNull("SeqSwitch2") == null, "SeqSwitch2 pruned");
        Check(root.GetNodeOrNull("SeqSwitch3") == null, "SeqSwitch3 pruned");
        Check(root.GetNodeOrNull("RewardDoor") == null, "RewardDoor pruned");
        Check(root.GetNodeOrNull("DrainMonsterSequence") == null, "DrainMonsterSequence pruned");
        Check(root.GetNodeOrNull("Chef") == null, "area1 Chef pruned");
        Check(root.GetNodeOrNull("ScrambledEggPickup") == null, "ScrambledEggPickup pruned");
        Check(root.GetNodeOrNull("SewersEntrance") == null, "SewersEntrance pruned");
        Check(root.GetNodeOrNull("SewersTopEntrance") == null, "SewersTopEntrance pruned");

        // Kitchen gate transition
        var gate = root.GetNodeOrNull<LevelTransition>("KitchenTransition");
        Check(gate != null, "KitchenTransition present");
        if (gate != null)
        {
            Check(gate.Level == "res://levels/kitchen/maps/Kitchen.tscn", "KitchenTransition targets Kitchen.tscn");
            Check(gate.TargetTransitionName == "IntakeArrival", "KitchenTransition targets IntakeArrival");
            Check(gate.RequiredFlag == "intake_settled", "KitchenTransition gated on intake_settled");
        }

        // Cell key item valid
        Check(ItemDatabase.Get("cell_key") != null, "cell_key exists in ItemDatabase");

        var cellPickup = root.GetNodeOrNull<PickupItem>("CellKeyPickup");
        Check(cellPickup != null && cellPickup.ItemId == "cell_key", "CellKeyPickup grants cell_key");

        // IntakeTowels script resolves
        var tracker = root.GetNodeOrNull<IntakeTowels>("IntakeTowels");
        Check(tracker != null, "IntakeTowels script attached");

        root.QueueFree();
    }

    private void VerifyKitchenEntry()
    {
        const string path = "res://levels/kitchen/maps/Kitchen.tscn";
        Check(FileAccess.FileExists(path), "Kitchen.tscn exists");
        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene == null) { Fail("Kitchen.tscn loads"); return; }

        var root = scene.Instantiate() as Node2D;
        if (root == null) { Fail("Kitchen root is Node2D"); return; }
        Root.AddChild(root);

        var arrival = root.GetNodeOrNull<LevelTransition>("IntakeArrival");
        Check(arrival != null, "Kitchen IntakeArrival present");
        if (arrival != null)
        {
            Check(arrival.Level == "res://levels/eggsile/maps/area1.tscn", "IntakeArrival targets area1.tscn");
            Check(arrival.TargetTransitionName == "KitchenTransition", "IntakeArrival targets KitchenTransition");
        }

        root.QueueFree();
    }
}