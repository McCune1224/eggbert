using Godot;

[GlobalClass]
public partial class DialogBranch : Resource
{
	[Export] public Godot.Collections.Array<DialogNode> Nodes { get; set; } = new();
}
