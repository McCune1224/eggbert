using Godot;

// One-off dev utility: authors the DialogBranch .tres resources for the five
// optional Factory tutorial NPCs (nested sub-resources must NOT be hand-authored).
// Run:
//   godot --headless --path . --script res://tests/GenerateDialogBranches.cs
// Re-running is safe (overwrites the same files).

public partial class GenerateDialogBranches : SceneTree
{
    public override void _Initialize()
    {
        Create("GreeterDialog", "Greeter", "met_greeter",
            new[] { "Oh — hey. You the new egg? Welcome to the night shift.", "Clock out at the TimeClock over there, then head through the door." },
            ("How do I play?", new[] { "WASD to move. Shift to sprint, Space to dash. E to talk, F to check stuff.", "You'll figure it out. Everyone does." }),
            ("Egg-costume rumors?", new[] { "*lowers voice* You heard about that too, huh?", "Some egg-costumed loon running around the plant. Management's losing it. Don't ask me." }));

        Create("PipDialog", "Pip", "met_pip",
            new[] { "Careful with that crate — it's gotta go on the pressure plate to open the gate.", "Push it with your body. Old-school, but it works." },
            ("Any tips?", new[] { "Jamitor's the one who'll walk you through the basics. I just sort boxes.", "Push the crate onto the plate. Gate opens. Simple." }),
            ("Egg phantom?", new[] { "*Pip snorts* Yeah, the 'Factory Phantom'. HR's been weird about it.", "Just do your job and clock out, egg. That's my advice." }));

        Create("BoltDialog", "Bolt", "met_bolt",
            new[] { "Conveyors'll carry you west if you ride 'em, push back to fight 'em.", "And sign the shutdown checklist on the clipboard before you move on — the console gets fussy." },
            ("How do conveyors work?", new[] { "Step on one and you'll slide. Sprint to push against the flow.", "Don't stand on the belt if you need to stop fast." }),
            ("Egg phantom again?", new[] { "*Bolt squints* Seen footprints shaped like eggs. Weirdest thing.", "Management says ignore it. I say keep your head down." }));

        Create("VexDialog", "Vex", "met_vex",
            new[] { "Inspection time. Step on the plates in order — A, B, C — and the door opens.", "Don't mess up the sequence or you start over. Standard." },
            ("After inspection?", new[] { "Once approved, the Loading Bay's yours. Then you're done for the night.", "Try not to get arrested on the way out. Long story." }),
            ("The 'egg' situation?", new[] { "*Vex sighs* Everyone's obsessed with that costume. Focus on your shift.", "If Bacon stops you, just cooperate. Trust me." }));

        Create("CraneDialog", "Crane", "met_crane",
            new[] { "Loading Bay. Two timed switches open the gate — hit either, you've got five seconds.", "Then Officer Bacon'll see you out. ...Probably." },
            ("Bacon seems intense.", new[] { "*Crane glances around* Bacon's by the book. Just do what they say.", "Tonight's got a weird energy. Stay chill." }),
            ("Egg phantom?", new[] { "*Crane laughs* You too? Whole plant's talking about it.", "If you see an egg-shaped shadow, pretend you didn't. For both our sakes." }));

        GD.Print("[gen-dialog] done");
        Quit(0);
    }

    private static void Create(string fileName, string speaker, string flag, string[] startLines,
        (string, string[]) opt0, (string, string[]) opt1)
    {
        var branch = new DialogBranch();
        branch.Nodes.Add(new DialogNode
        {
            Id = "start",
            SpeakerName = speaker,
            Lines = startLines,
            SetFlagsOnEnter = new[] { flag },
            Responses = new DialogResponse[]
            {
                new DialogResponse { Text = opt0.Item1, NextNodeId = "opt0" },
                new DialogResponse { Text = opt1.Item1, NextNodeId = "opt1" },
                new DialogResponse { Text = "(Leave)", NextNodeId = "" },
            }
        });
        branch.Nodes.Add(OptNode("opt0", speaker, opt0.Item2));
        branch.Nodes.Add(OptNode("opt1", speaker, opt1.Item2));

        string path = $"res://levels/factory/npcs/{fileName}.tres";
        Error err = ResourceSaver.Save(branch, path);
        GD.Print($"[gen-dialog] save {path} -> {err}");

        var loaded = ResourceLoader.Load<DialogBranch>(path);
        if (loaded == null)
            GD.PrintErr($"[gen-dialog] FAIL: could not reload {path}");
        else
            GD.Print($"[gen-dialog] reload {fileName} nodes={loaded.Nodes.Count} OK");
    }

    private static DialogNode OptNode(string id, string speaker, string[] lines) => new DialogNode
    {
        Id = id,
        SpeakerName = speaker,
        Lines = lines,
        Responses = new DialogResponse[] { new DialogResponse { Text = "(Leave)", NextNodeId = "" } }
    };
}
