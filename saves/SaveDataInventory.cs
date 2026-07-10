using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SaveDataInventory : Resource
{
    [Export]
    public Dictionary<string, Variant> Stacks { get; set; } = new();
}