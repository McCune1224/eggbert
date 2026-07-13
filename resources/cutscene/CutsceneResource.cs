using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CutsceneResource : Resource
{
    [Export] public Array<CutsceneStep> Steps { get; set; } = new();
}
