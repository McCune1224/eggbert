using Godot;
/// <summary>
/// Root resource for a node-and-response dialog tree.
/// Contains an ordered array of <see cref="DialogNode"/> resources; each node
/// defines speaker lines and branching responses (each pointing to a
/// NextNodeId or empty string to end the dialog).
/// </summary>

[GlobalClass]
public partial class DialogBranch : Resource
{
	[Export] public Godot.Collections.Array<DialogNode> Nodes { get; set; } = new();
}
