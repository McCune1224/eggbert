using System;
using Godot;

/// <summary>
/// Headless verifier for the rebuilt Eggs Isle "First Night" exile (issues #175–#181):
/// three chained maps — The Dock, The Gatehouse, The Overflow wing — with the
/// AnimationPlayer cutscene system, the fresh cast, and the Overflow quest.
/// Run with:
///   godot --headless --path . --script res://tests/VerifyEggsileFirstNight.cs
/// </summary>
public partial class VerifyEggsileFirstNight : SceneTree
{
    private const string DockPath = "res://levels/eggsile/maps/EggsIsle.tscn";
    private const string GatehousePath = "res://levels/eggsile/maps/EggsIsleGatehouse.tscn";
    private const string BlockPath = "res://levels/eggsile/maps/EggsIsleBlock.tscn";
    private int _failures = 0;

    public override void _Initialize()
    {
        VerifyDock();
        VerifyGatehouse();
        VerifyBlock();
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

    private Node2D LoadLevel(string path, string expectName)
    {
        Check(FileAccess.FileExists(path), $"{path} exists");
        var scene = ResourceLoader.Load<PackedScene>(path);
        Check(scene != null, $"{path} loads");
        if (scene == null) return null;
        var root = scene.Instantiate() as Node2D;
        Check(root != null, $"{path} root is Node2D");
        if (root == null) return null;
        Root.AddChild(root);
        Check(root is BaseLevel, $"{path} root is BaseLevel");
        var level = root as BaseLevel;
        Check(level != null && level.LevelName == expectName, $"{path} LevelName = '{expectName}'");
        TileMapLayer tiles = null;
        foreach (var child in root.GetChildren())
            if (child is TileMapLayer tml && tml.GetUsedRect().Size != Vector2I.Zero)
                tiles = tml;
        Check(tiles != null, $"{path} has a painted tilemap");
        if (tiles != null)
        {
            var rect = tiles.GetUsedRect();
            Check(rect.Size.X >= 100 && rect.Size.Y >= 50, $"{path} used rect ({rect.Size.X}x{rect.Size.Y})");
        }
        return root;
    }

    private void VerifyDock()
    {
        GD.Print("== Dock ==");
        var root = LoadLevel(DockPath, "Eggs Isle — The Dock");
        if (root == null) return;

        var hub = root.GetNodeOrNull<LevelTransition>("HubArrival");
        Check(hub != null, "HubArrival present (arrest-handoff anchor)");
        if (hub != null)
        {
            Check(hub.Level == DockPath, "HubArrival points at the dock scene");
            Check(hub.TargetTransitionName == "HubArrival", "HubArrival targets itself");
            Check(hub.RequiredFlag == "hub_arrival_inactive", "HubArrival is anchor-only (no reload loop)");
        }
        Check(root.GetNodeOrNull("DockSavePoint") != null, "DockSavePoint present");
        var warp = root.GetNodeOrNull<Area2D>("WarpPoint");
        Check(warp != null && (string)warp.Get("WarpId") == "eggsile_area1", "WarpPoint eggsile_area1 present");

        var arrival = root.GetNodeOrNull<CutsceneTrigger>("ArrivalCutscene");
        Check(arrival != null, "ArrivalCutscene trigger present");
        if (arrival != null)
        {
            Check(arrival.Mode == TriggerMode.OnEnter && arrival.Once, "ArrivalCutscene OnEnter one-shot");
            Check(arrival.CutsceneId == "eggsile_arrival", "ArrivalCutscene CutsceneId correct");
            Check(arrival.CutsceneScene != null, "ArrivalCutscene uses an AnimationPlayer cutscene scene");
        }
        Check(root.GetNodeOrNull("OfficerBacon") != null, "OfficerBacon present on the pier");

        var gate = root.GetNodeOrNull<LevelTransition>("DockGate");
        Check(gate != null, "DockGate transition present");
        if (gate != null)
        {
            Check(gate.Level == GatehousePath, "DockGate → Gatehouse");
            Check(gate.TargetTransitionName == "DockArrival", "DockGate targets Gatehouse/DockArrival");
        }

        root.QueueFree();
    }

    private void VerifyGatehouse()
    {
        GD.Print("== Gatehouse ==");
        var root = LoadLevel(GatehousePath, "Eggs Isle — Gatehouse");
        if (root == null) return;

        var back = root.GetNodeOrNull<LevelTransition>("DockArrival");
        Check(back != null, "DockArrival return transition present");
        if (back != null)
        {
            Check(back.Level == DockPath && back.TargetTransitionName == "DockGate", "DockArrival → Dock");
        }
        var exit = root.GetNodeOrNull<LevelTransition>("GatehouseExit");
        Check(exit != null, "GatehouseExit transition present");
        if (exit != null)
        {
            Check(exit.Level == BlockPath && exit.TargetTransitionName == "GatehouseArrival", "GatehouseExit → Overflow wing");
            Check(exit.RequiredFlag == "met_tea", "GatehouseExit gated on met_tea");
        }

        Check(root.GetNodeOrNull("MrTea") != null, "MrTea present (booking desk)");
        Check(root.GetNodeOrNull("Ledger") != null, "Ledger readable present");
        Check(root.GetNodeOrNull("RulesBoard") != null, "RulesBoard readable present");
        Check(root.GetNodeOrNull("IntakeStamp") != null, "IntakeStamp readable present");
        Check(root.GetNodeOrNull("GatehouseSavePoint") != null, "GatehouseSavePoint present");

        var checkIn = root.GetNodeOrNull<CutsceneTrigger>("CheckInCutscene");
        Check(checkIn != null, "CheckInCutscene trigger present");
        if (checkIn != null)
        {
            Check(checkIn.Once && checkIn.CutsceneId == "gatehouse_checkin", "CheckInCutscene one-shot");
            Check(checkIn.CutsceneScene != null, "CheckInCutscene uses an AnimationPlayer cutscene scene");
        }

        root.QueueFree();
    }

    private void VerifyBlock()
    {
        GD.Print("== Overflow Wing ==");
        var root = LoadLevel(BlockPath, "Eggs Isle — The Overflow");
        if (root == null) return;

        var arrival = root.GetNodeOrNull<LevelTransition>("GatehouseArrival");
        Check(arrival != null, "GatehouseArrival transition present");
        if (arrival != null)
            Check(arrival.Level == GatehousePath && arrival.TargetTransitionName == "GatehouseExit", "GatehouseArrival → Gatehouse");

        var placement = root.GetNodeOrNull<CutsceneTrigger>("CellPlacement");
        Check(placement != null, "CellPlacement trigger present");
        if (placement != null)
        {
            Check(placement.Once && placement.CutsceneId == "cell_placement", "CellPlacement one-shot");
            Check(placement.CutsceneScene != null, "CellPlacement uses an AnimationPlayer cutscene scene");
        }

        // The cell
        Check(root.GetNodeOrNull("Frank") != null, "Frank present (cell niche)");
        var frank = root.GetNodeOrNull<Node>("Frank");
        var frankTrigger = frank?.GetNodeOrNull<CutsceneTrigger>("CutsceneTrigger");
        Check(frankTrigger != null, "Frank has a CutsceneTrigger");
        if (frankTrigger != null)
        {
            Check(frankTrigger.Cutscene != null, "Frank has conditional dialog (FrankIntake.tres)");
            Check(frankTrigger.SetFlagsOnFire != null &&
                  Array.IndexOf(frankTrigger.SetFlagsOnFire, "met_frank") >= 0, "Frank sets met_frank");
        }
        Check(root.GetNodeOrNull("BunkSavePoint") != null, "BunkSavePoint present (cell)");
        Check(root.GetNodeOrNull("WallScratch") != null, "WallScratch readable present");
        Check(root.GetNodeOrNull("TinCup") != null, "TinCup readable present");
        var keyPickup = root.GetNodeOrNull<PickupItem>("TunnelKeyPickup");
        Check(keyPickup != null && keyPickup.ItemId == "tunnel_key", "TunnelKeyPickup grants tunnel_key");
        if (keyPickup != null)
        {
            bool hasFlag = keyPickup.SetFlag != null && Array.IndexOf(keyPickup.SetFlag, "found_tunnel_key") >= 0;
            Check(hasFlag, "TunnelKeyPickup sets found_tunnel_key");
        }

        // Boiler puzzle
        Check(root.GetNodeOrNull("BoilerCrate") != null, "BoilerCrate present");
        var plate = root.GetNodeOrNull<Area2D>("BoilerPlate");
        Check(plate != null, "BoilerPlate present");
        Check(root.GetNodeOrNull("BoilerDoor") != null, "BoilerDoor present");
        if (plate != null)
        {
            Check(root.GetNodeOrNull((NodePath)plate.Get("TargetDoorPath")) != null, "BoilerPlate TargetDoorPath resolves");
            Check((string)plate.Get("PushablePressedFlag") == "boiler_gate_open", "BoilerPlate flag = boiler_gate_open");
        }
        Check(root.GetNodeOrNull("Mikan") != null, "Mikan present (boiler room)");
        Check(root.GetNodeOrNull("BoilerRewardPickup") != null, "BoilerRewardPickup present");

        // Tank + gallery
        Check(root.GetNodeOrNull("Scrambles") != null, "Scrambles present (the Tank)");
        Check(root.GetNodeOrNull("TidePoolRewardPickup") != null, "TidePoolRewardPickup present");
        Check(root.GetNodeOrNull("GallerySign") != null, "GallerySign readable present");
        Check(root.GetNodeOrNull("WienerCop") != null, "WienerCop present (corridor)");

        // The count
        var count = root.GetNodeOrNull<CutsceneTrigger>("CountTrigger");
        Check(count != null, "CountTrigger present");
        if (count != null)
        {
            Check(count.Once && count.CutsceneId == "count", "CountTrigger one-shot");
            Check(count.CutsceneScene != null, "CountTrigger uses an AnimationPlayer cutscene scene");
            Check(count.SetFlagsOnFire != null &&
                  Array.IndexOf(count.SetFlagsOnFire, "eggsile_count_survived") >= 0, "CountTrigger sets eggsile_count_survived");
        }

        // The hatch payoff
        var hatch = root.GetNodeOrNull<KeyDoor>("TunnelHatch");
        Check(hatch != null, "TunnelHatch KeyDoor present");
        if (hatch != null)
            Check(hatch.RequiredFlag == "found_tunnel_key", "TunnelHatch requires found_tunnel_key");
        var tunnelReward = root.GetNodeOrNull<PickupItem>("TunnelRewardPickup");
        Check(tunnelReward != null && tunnelReward.ItemId == "lucky_yolk", "TunnelRewardPickup grants lucky_yolk");
        if (tunnelReward != null)
        {
            bool hasFlag = tunnelReward.SetFlag != null && Array.IndexOf(tunnelReward.SetFlag, "tunnel_opened") >= 0;
            Check(hasFlag, "TunnelRewardPickup sets tunnel_opened");
        }
        Check(root.GetNodeOrNull("HatchNote") != null, "HatchNote readable present");

        // Collision walls
        int wallBodies = 0;
        foreach (var child in root.GetChildren())
            if (child is StaticBody2D sb && sb.Name.ToString().StartsWith("Wall_"))
                wallBodies++;
        Check(wallBodies >= 10, $"{wallBodies} StaticBody2D wall strips present");

        // Items
        Check(ItemDatabase.Get("tunnel_key") != null, "tunnel_key in ItemDatabase");
        Check(ItemDatabase.Get("lucky_yolk") != null, "lucky_yolk in ItemDatabase");
        Check(ItemDatabase.Get("hardboiled_egg") != null, "hardboiled_egg in ItemDatabase");
        Check(ItemDatabase.Get("deviled_egg") != null, "deviled_egg in ItemDatabase");

        root.QueueFree();
    }

    private void VerifyQuest()
    {
        GD.Print("== Quest ==");
        var questManagerScene = ResourceLoader.Load<PackedScene>("res://autoload/QuestManager.tscn");
        Check(questManagerScene != null, "QuestManager.tscn loads");
        if (questManagerScene == null) return;

        var qm = questManagerScene.Instantiate<QuestManager>();
        Root.AddChild(qm);

        var quest = qm.GetQuest("eggs_isle_overflow");
        Check(quest != null, "eggs_isle_overflow registered in QuestManager");
        if (quest != null)
        {
            Check(quest.Title == "The Overflow", "Quest title correct");
            Check(quest.StartFlag == "arrested", "Quest StartFlag = arrested");
            Check(quest.Objectives != null && quest.Objectives.Count == 5, "Quest has 5 objectives");
            if (quest.Objectives != null && quest.Objectives.Count == 5)
            {
                Check(quest.Objectives[0].CompletionFlag == "met_tea", "Objective 1: met_tea");
                Check(quest.Objectives[1].CompletionFlag == "met_frank", "Objective 2: met_frank");
                Check(quest.Objectives[2].CompletionFlag == "eggsile_count_survived", "Objective 3: eggsile_count_survived");
                Check(quest.Objectives[3].CompletionFlag == "found_tunnel_key", "Objective 4: found_tunnel_key");
                Check(quest.Objectives[4].CompletionFlag == "tunnel_opened", "Objective 5: tunnel_opened (optional)");
            }
        }

        qm.QueueFree();
    }
}
