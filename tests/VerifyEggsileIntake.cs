using System;
using Godot;

/// <summary>
/// Headless verifier for the Eggs Isle arrival/intake level (issue #130).
/// Asserts the new EggsIsle.tscn structure, flag wiring, quest registration,
/// Kitchen gate, and the #127 sewers link. Run with:
///   godot --headless --path . --script res://tests/VerifyEggsileIntake.cs
/// </summary>
public partial class VerifyEggsileIntake : SceneTree
{
    private const string ScenePath = "res://levels/eggsile/maps/EggsIsle.tscn";
    private int _failures = 0;

    public override void _Initialize()
    {
        VerifyEggsIsle();
        VerifyKitchenEntry();
        VerifySewersLink();
        VerifyQuest();
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

    private void VerifyEggsIsle()
    {
        Check(FileAccess.FileExists(ScenePath), "EggsIsle.tscn exists");
        var scene = ResourceLoader.Load<PackedScene>(ScenePath);
        Check(scene != null, "EggsIsle.tscn loads");
        if (scene == null) return;

        var root = scene.Instantiate() as Node2D;
        Check(root != null, "EggsIsle root is Node2D");
        if (root == null) return;
        Root.AddChild(root);

        Check(root is BaseLevel, "EggsIsle root is BaseLevel");

        // Tilemap with painted bounds
        TileMapLayer tiles = null;
        foreach (var child in root.GetChildren())
            if (child is TileMapLayer tml && tml.GetUsedRect().Size != Vector2I.Zero)
                tiles = tml;
        Check(tiles != null, "EggsIsle has a painted tilemap layer");

        // Arrival / save / warp
        Check(root.GetNodeOrNull("HubArrival") != null, "HubArrival present");
        Check(root.GetNodeOrNull("HubSavePoint") != null, "HubSavePoint present");
        Check(root.GetNodeOrNull("WarpPoint") != null, "WarpPoint present");
        Check(root.GetNodeOrNull("IntakeTowels") != null, "IntakeTowels present");
        Check(root.GetNodeOrNull("CellKeyPickup") != null, "CellKeyPickup present");

        var warp = root.GetNodeOrNull<Area2D>("WarpPoint");
        if (warp != null)
            Check((string)warp.Get("WarpId") == "eggsile_area1", "WarpPoint id is eggsile_area1");

        // Intro cutscene trigger
        var arrival = root.GetNodeOrNull<CutsceneTrigger>("ArrivalCutscene");
        Check(arrival != null, "ArrivalCutscene present");
        if (arrival != null)
        {
            Check(arrival.Mode == TriggerMode.OnEnter, "ArrivalCutscene is OnEnter");
            Check(arrival.Once, "ArrivalCutscene is one-shot");
            Check(arrival.CutsceneId == "eggsile_arrival", "ArrivalCutscene CutsceneId correct");
            Check(arrival.Cutscene != null, "ArrivalCutscene has a Cutscene resource");
        }

        // Intake NPCs + history book
        Check(root.GetNodeOrNull("Joe") != null, "Joe present");
        Check(root.GetNodeOrNull("Frank") != null, "Frank present");
        Check(root.GetNodeOrNull("OfficerBacon") != null, "OfficerBacon present");
        Check(root.GetNodeOrNull("HistoryBook") != null, "HistoryBook present");

        var joe = root.GetNodeOrNull<Node>("Joe");
        var joeTrigger = joe?.GetNodeOrNull<CutsceneTrigger>("CutsceneTrigger");
        Check(joeTrigger != null, "Joe has a CutsceneTrigger");
        if (joeTrigger != null)
            Check(joeTrigger.SetFlagsOnFire != null &&
                  Array.IndexOf(joeTrigger.SetFlagsOnFire, "met_joe") >= 0,
                  "Joe trigger sets met_joe");

        var frank = root.GetNodeOrNull<Node>("Frank");
        var frankTrigger = frank?.GetNodeOrNull<CutsceneTrigger>("CutsceneTrigger");
        Check(frankTrigger != null, "Frank has a CutsceneTrigger");
        if (frankTrigger != null)
        {
            Check(frankTrigger.SetFlagsOnFire != null &&
                  Array.IndexOf(frankTrigger.SetFlagsOnFire, "met_frank") >= 0,
                  "Frank trigger sets met_frank");
            Check(frankTrigger.Cutscene != null, "Frank has a Cutscene resource (conditional handoff)");
        }

        var book = root.GetNodeOrNull<ReadableObject>("HistoryBook");
        Check(book != null, "HistoryBook is ReadableObject");
        if (book != null)
            Check(book.GateFlag == "history_book", "HistoryBook GateFlag = history_book (read_history_book)");

        // Towels gated one-shots
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

        // Cell key item + flag
        Check(ItemDatabase.Get("cell_key") != null, "cell_key exists in ItemDatabase");
        var cellPickup = root.GetNodeOrNull<PickupItem>("CellKeyPickup");
        Check(cellPickup != null && cellPickup.ItemId == "cell_key", "CellKeyPickup grants cell_key");
        if (cellPickup != null)
        {
            bool hasFoundFlag = cellPickup.SetFlag != null &&
                                Array.IndexOf(cellPickup.SetFlag, "found_cell_key") >= 0;
            Check(hasFoundFlag, "CellKeyPickup sets found_cell_key");
        }

        // Kitchen gate
        var gate = root.GetNodeOrNull<LevelTransition>("KitchenTransition");
        Check(gate != null, "KitchenTransition present");
        if (gate != null)
        {
            Check(gate.Level == "res://levels/kitchen/maps/Kitchen.tscn", "KitchenTransition targets Kitchen.tscn");
            Check(gate.TargetTransitionName == "IntakeArrival", "KitchenTransition targets IntakeArrival");
            Check(gate.RequiredFlag == "intake_settled", "KitchenTransition gated on intake_settled");
            bool setsVisited = gate.SetFlagsOnFire != null &&
                               Array.IndexOf(gate.SetFlagsOnFire, "visited_kitchen") >= 0;
            Check(setsVisited, "KitchenTransition sets visited_kitchen");
        }

        // #127: sewers link present
        var sewers = root.GetNodeOrNull<LevelTransition>("SewersEntrance");
        Check(sewers != null, "SewersEntrance present (#127)");
        if (sewers != null)
        {
            Check(sewers.Level == "res://levels/eggsile/maps/EggsileSewers.tscn", "SewersEntrance targets EggsileSewers.tscn");
            Check(sewers.TargetTransitionName == "Area1Exit", "SewersEntrance targets Area1Exit");
        }

        // Hub links preserved for overworld/sandbox gates
        Check(root.GetNodeOrNull("LeftHallwayTransition") != null, "LeftHallwayTransition present");
        Check(root.GetNodeOrNull("DummyUp") != null, "DummyUp present");
        Check(root.GetNodeOrNull("SandboxArrival") != null, "SandboxArrival present");

        // area1 deleted
        Check(!FileAccess.FileExists("res://levels/eggsile/maps/area1.tscn"), "area1.tscn deleted");

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
            Check(arrival.Level == ScenePath, "IntakeArrival targets EggsIsle.tscn");
            Check(arrival.TargetTransitionName == "KitchenTransition", "IntakeArrival targets KitchenTransition");
        }

        root.QueueFree();
    }

    private void VerifySewersLink()
    {
        const string path = "res://levels/eggsile/maps/EggsileSewers.tscn";
        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene == null) { Fail("EggsileSewers.tscn loads"); return; }

        var root = scene.Instantiate() as Node2D;
        if (root == null) { Fail("Sewers root is Node2D"); return; }
        Root.AddChild(root);

        var exit = root.GetNodeOrNull<LevelTransition>("Area1Exit");
        Check(exit != null, "Sewers Area1Exit present");
        if (exit != null)
        {
            Check(exit.Level == ScenePath, "Sewers Area1Exit targets EggsIsle.tscn (#127)");
            Check(exit.TargetTransitionName == "SewersEntrance", "Sewers Area1Exit targets SewersEntrance");
        }

        root.QueueFree();
    }

    private void VerifyQuest()
    {
        var questManagerScene = ResourceLoader.Load<PackedScene>("res://autoload/QuestManager.tscn");
        Check(questManagerScene != null, "QuestManager.tscn loads");
        if (questManagerScene == null) return;

        var qm = questManagerScene.Instantiate<QuestManager>();
        Root.AddChild(qm);

        var quest = qm.GetQuest("eggs_isle_first_night");
        Check(quest != null, "eggs_isle_first_night registered in QuestManager");
        if (quest != null)
        {
            Check(quest.Title == "First Night on Eggs Isle", "Quest title correct");
            Check(quest.StartFlag == "arrested", "Quest StartFlag = arrested");
            Check(quest.Objectives != null && quest.Objectives.Count == 5,
                  "Quest has 5 objectives");
            if (quest.Objectives != null && quest.Objectives.Count == 5)
            {
                Check(quest.Objectives[0].CompletionFlag == "met_joe", "Objective 1: met_joe");
                Check(quest.Objectives[1].CompletionFlag == "met_frank", "Objective 2: met_frank");
                Check(quest.Objectives[2].CompletionFlag == "intake_settled", "Objective 3: intake_settled");
                Check(quest.Objectives[3].CompletionFlag == "found_cell_key", "Objective 4: found_cell_key");
                Check(quest.Objectives[4].CompletionFlag == "visited_kitchen", "Objective 5: visited_kitchen");
            }
        }

        qm.QueueFree();
    }
}
