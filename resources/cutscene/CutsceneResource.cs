using Godot;
using Godot.Collections;
/// <summary>
/// Container resource holding an ordered array of <see cref="CutsceneStep"/> resources.
/// Loaded from .tres via the Inspector and played by <see cref="CutsceneController"/>.
/// </summary>

[GlobalClass]
public partial class CutsceneResource : Resource
{
    [Export] public Array<CutsceneStep> Steps { get; set; } = new();
}
