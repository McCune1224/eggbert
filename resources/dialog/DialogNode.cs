using Godot;

[GlobalClass]
public partial class DialogNode : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string SpeakerName { get; set; } = "";
    [Export] public DialogVoiceResource Voice { get; set; }
    [Export(PropertyHint.MultilineText)] public string[] Lines { get; set; } = System.Array.Empty<string>();
    [Export] public DialogResponse[] Responses { get; set; } = System.Array.Empty<DialogResponse>();
    [Export] public CutsceneCondition Condition { get; set; }
    [Export] public string[] SetFlagsOnEnter { get; set; } = System.Array.Empty<string>();
}
