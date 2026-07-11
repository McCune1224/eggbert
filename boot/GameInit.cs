using Godot;

public partial class GameInit : Node
{
    public override void _Ready()
    {
        var menuPacked = ResourceLoader.Load<PackedScene>("res://ui/MainMenu.tscn");
        if (menuPacked == null)
        {
            GD.PrintErr("Failed to load MainMenu.tscn");
            return;
        }
        // Deferring avoids 'Parent node is busy setting up children' error when
        // GameInit runs its _Ready during the root's _Ready cascade.
        Callable.From(() => GetTree().Root.AddChild(menuPacked.Instantiate())).CallDeferred();
    }
}
