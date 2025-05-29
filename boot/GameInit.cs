using Godot;

// This is the main entry point for the game
// TODO: Make a proper main menu scene and load it here instead of just going straight to the overworld
public partial class GameInit : Node
{
    public override void _Ready()
    {
        // Get the singleton instance
        var overworldManager = GameController.Instance;

        if (overworldManager == null)
        {
            GD.PrintErr("OverworldManager instance is null. Make sure it's properly initialized as an autoload.");
            return;
        }
        overworldManager.LoadLevel("res://levels/overworld/maps/Overworld.tscn", Vector2.Zero);



        // overworldManager.LoadLevel("res://levels/eggsile/sandbox/area1.tscn", Vector2.Zero);
        // // Load the overworld map
        // GameSaverLoader gsl = new(this.GetTree());
        // gsl.LoadGame();
        // // Optionally set initial player position
        // overworldManager.SetPlayerPosition(new Vector2(100, 100));
    }
}
