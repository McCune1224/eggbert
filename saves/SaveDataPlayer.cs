using Godot;

[GlobalClass]
public partial class SaveDataPlayer : Resource
{
    [Export] public Vector2 Position { get; set; }
    [Export] public int Health { get; set; }
    [Export] public string LevelScenePath { get; set; }
}
