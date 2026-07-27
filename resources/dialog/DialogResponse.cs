using Godot;

[GlobalClass]
public partial class DialogResponse : Resource
{
    [Export] public string Text { get; set; } = "";
    [Export] public string NextNodeId { get; set; } = "";
    [Export] public string SetFlagOnSelect { get; set; } = "";
    [Export] public CutsceneCondition Condition { get; set; }
}
