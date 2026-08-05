using Godot;

/// <summary>
/// A single quest objective tied to WorldFlags.
/// Completion is signaled by setting CompletionFlag to true in WorldFlags.
/// Optional <see cref="LocationLevel"/> + <see cref="LocationPosition"/> place a
/// marker on the overworld HUD map for the pinned quest's current objective.
/// </summary>
[GlobalClass]
public partial class QuestObjective : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public string CompletionFlag { get; set; } = "";
    /// <summary>res:// path of the level the objective takes place in. Empty = no map marker.</summary>
    [Export] public string LocationLevel { get; set; } = "";
    /// <summary>World position within <see cref="LocationLevel"/> where the map marker is drawn.</summary>
    [Export] public Vector2 LocationPosition { get; set; }
}
