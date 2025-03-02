using Godot;

public partial class GameInit : Node
{
    public override void _Ready()
    {
        GD.Print("HIT");

        // Get the singleton instance
        var overworldManager = OverworldManager.Instance;

        if (overworldManager == null)
        {
            GD.PrintErr("OverworldManager instance is null. Make sure it's properly initialized as an autoload.");
            return;
        }

        // Load the overworld map
        overworldManager.LoadMap("res://scenes/overworld/maps/Overworld.tscn");

        // Optionally set initial player position
        overworldManager.SetPlayerPosition(new Vector2(100, 100));
    }
}
