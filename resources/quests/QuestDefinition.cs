using Godot;
using Godot.Collections;

[GlobalClass]
public partial class QuestDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Title { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public string StartFlag { get; set; } = "";
    [Export] public Array<QuestObjective> Objectives { get; set; } = new();
}
