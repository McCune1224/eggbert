using Godot;
using System;
using System.Threading.Tasks;

// Headless verifier for the one-shot combat trigger pattern.
//
// Each trigger type (CerealEncounterTrigger, FinalBossTrigger, MercyEncounterTrigger)
// must self-free in _Ready when its OnceFlag is set in WorldFlags, and must NOT
// self-free when OnceFlag is empty or the flag is unset. The verifier exercises
// the actual scripts and the actual WorldFlags autoload, then parses each level
// scene to confirm OnceFlag is wired on the expected nodes.
//
// "Self-frees" is observed by adding the trigger to the root, awaiting one frame
// for QueueFree to drain, then checking GodotObject.IsInstanceValid — by the time
// the frame ends the C# wrapper is disposed, so accessing instance methods throws.
// IsInstanceValid is the safe predicate.
//
// Run with: godot --headless --path . --script res://tests/VerifyCombatOnceFlag.cs

public partial class VerifyCombatOnceFlag : SceneTree
{
    private int _failures;

    public override async void _Initialize()
    {
        await ToSignal(Root, Window.SignalName.Ready);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (WorldFlags.Instance == null)
        {
            GD.PushError("WorldFlags autoload missing");
            Quit(1);
            return;
        }

        if (!await ExpectSelfFree("Cereal",
                () => new CerealEncounterTrigger { OnceFlag = "test_cereal_resolved", Name = "T_Cereal_Resolved" }))
            return;
        if (!await ExpectStays("Cereal",
                () => new CerealEncounterTrigger { OnceFlag = "test_cereal_unset", Name = "T_Cereal_Unset" }))
            return;
        if (!await ExpectStays("Cereal",
                () => new CerealEncounterTrigger { OnceFlag = "", Name = "T_Cereal_Empty" }))
            return;

        if (!await ExpectSelfFree("FinalBoss",
                () => new FinalBossTrigger { OnceFlag = "test_leader_resolved", Name = "T_Final_Resolved" }))
            return;
        if (!await ExpectStays("FinalBoss",
                () => new FinalBossTrigger { OnceFlag = "test_leader_unset", Name = "T_Final_Unset" }))
            return;

        if (!await ExpectSelfFree("Mercy",
                () => new MercyEncounterTrigger { OnceFlag = "test_mercy_resolved", Name = "T_Mercy_Resolved" }))
            return;
        if (!await ExpectStays("Mercy",
                () => new MercyEncounterTrigger { OnceFlag = "test_mercy_unset", Name = "T_Mercy_Unset" }))
            return;

        if (!CheckSceneWiring(
                "res://levels/prison/maps/PrisonBlockC.tscn",
                "CerealEncounter",
                "OnceFlag = \"resolved_prison_cereal\"")) return;
        if (!CheckSceneWiring(
                "res://levels/overworld/maps/TheGreatBeyond.tscn",
                "FinalBossTrigger",
                "OnceFlag = \"beat_leader\"")) return;
        if (!CheckSceneWiring(
                "res://levels/overworld/maps/TheGreatBeyond.tscn",
                "HubOptionalEncounter",
                "OnceFlag = \"resolved_gtg_hub\"")) return;
        if (!CheckSceneWiring(
                "res://levels/factory/maps/Zone1.tscn",
                "HubOptionalEncounter",
                "OnceFlag = \"resolved_factory_hub\"")) return;

        if (_failures == 0)
        {
            GD.Print("[combat-once] ALL OK");
            Quit(0);
        }
        else
        {
            GD.PushError($"[combat-once] {_failures} failure(s)");
            Quit(1);
        }
    }

    private async Task<bool> ExpectSelfFree(string label, Func<Node> factory)
    {
        WorldFlags.Instance.ClearAll();
        // The flag is named to match what the factory puts on OnceFlag.
        var trigger = factory();
        var flag = trigger.Get("OnceFlag").AsString();
        WorldFlags.Instance.SetFlag(flag, true);

        Root.AddChild(trigger);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (GodotObject.IsInstanceValid(trigger))
        {
            Fail($"{label}: trigger with OnceFlag='{flag}' (set) should have self-freed");
            trigger.QueueFree();
            return false;
        }

        GD.Print($"[combat-once] {label}: self-frees when OnceFlag='{flag}' is set");
        return true;
    }

    private async Task<bool> ExpectStays(string label, Func<Node> factory)
    {
        WorldFlags.Instance.ClearAll();
        var trigger = factory();
        var flag = trigger.Get("OnceFlag").AsString();

        Root.AddChild(trigger);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        if (!GodotObject.IsInstanceValid(trigger))
        {
            Fail($"{label}: trigger with OnceFlag='{flag}' (empty/unset) should NOT have self-freed");
            return false;
        }

        GD.Print($"[combat-once] {label}: stays when OnceFlag='{flag}' is empty or unset");
        trigger.QueueFree();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        return true;
    }

    private bool CheckSceneWiring(string scenePath, string nodeName, string expectedLine)
    {
        var text = ReadAllText(scenePath);
        if (string.IsNullOrEmpty(text))
        {
            Fail($"Scene unreadable: {scenePath}");
            return false;
        }

        var lines = text.Split('\n');
        bool inNode = false;
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith($"[node name=\"{nodeName}\""))
            {
                inNode = true;
                continue;
            }
            if (inNode)
            {
                if (line.StartsWith("[node ") || line.StartsWith("["))
                {
                    inNode = false;
                }
                else if (line == expectedLine)
                {
                    GD.Print($"[combat-once] {System.IO.Path.GetFileName(scenePath)}: {nodeName} has {expectedLine}");
                    return true;
                }
            }
        }

        Fail($"{scenePath}: node '{nodeName}' missing line: {expectedLine}");
        return false;
    }

    private static string ReadAllText(string godotPath)
    {
        var fsPath = ProjectSettings.GlobalizePath(godotPath);
        if (!System.IO.File.Exists(fsPath)) return null;
        return System.IO.File.ReadAllText(fsPath);
    }

    private void Fail(string msg)
    {
        _failures++;
        GD.PushError($"[combat-once] {msg}");
    }
}
