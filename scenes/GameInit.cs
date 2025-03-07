using Godot;

// This is the main entry point for the game
// TODO: Make a proper main menu scene and load it here instead of just going straight to the overworld
public partial class GameInit : Node
{
    public override void _Ready()
    {

        // Get the singleton instance
        var overworldManager = OverworldManager.Instance;

        if (overworldManager == null)
        {
            GD.PrintErr("OverworldManager instance is null. Make sure it's properly initialized as an autoload.");
            return;
        }

        // Load the overworld map
        overworldManager.LoadOverworldScene("res://scenes/overworld/maps/Overworld.tscn");

        // Optionally set initial player position
        overworldManager.SetPlayerPosition(new Vector2(100, 100));
    }
}
