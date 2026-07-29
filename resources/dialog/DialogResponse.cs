using Godot;

/// <summary>
/// A single response option in a dialog node. Selecting it displays the choice text,
/// optionally sets a world flag, and branches to the next dialog node.
/// </summary>
[GlobalClass]
public partial class DialogResponse : Resource
{
	/// <summary>
	/// The text displayed as the player's choice in the dialog option menu.
	/// </summary>
	[Export] public string Text { get; set; } = "";
	/// <summary>
	/// The id of the next DialogNode to show after this response is chosen.
	/// An empty string ends the dialog branch at this response.
	/// </summary>
	[Export] public string NextNodeId { get; set; } = "";
	/// <summary>
	/// The world flag set to true when the player chooses this response.
	/// </summary>
	[Export] public string SetFlagOnSelect { get; set; } = "";
	/// <summary>
	/// An optional condition that gates whether this response is available.
	/// </summary>
	[Export] public CutsceneCondition Condition { get; set; }
}
