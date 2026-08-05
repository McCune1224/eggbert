using Godot;

/// <summary>
/// A single quest objective tied to WorldFlags.
/// Completion is signaled by setting CompletionFlag to true in WorldFlags.
/// </summary>
[GlobalClass]
public partial class QuestObjective : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public string CompletionFlag { get; set; } = "";
}
