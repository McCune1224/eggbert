using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SaveDataWorldFlags : Resource
{
    [Export]
    public Dictionary<string, Variant> Flags { get; set; } = new();
}
