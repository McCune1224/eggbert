using Godot;

public partial class Checkpoint : Area2D
{
    [Export] public string SpawnLabel { get; set; } = "";

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;

        GameController.Instance.CheckpointLevelPath = GameController.Instance.CurrentLevel.SceneFilePath;
        GameController.Instance.CheckpointPosition = GlobalPosition;
    }
}
