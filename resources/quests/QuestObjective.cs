using Godot;

[GlobalClass]
public partial class QuestObjective : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public string CompletionFlag { get; set; } = "";
}
