using Godot;
using System;

public partial class CombatOpponent : Node2D
{
    private PackedScene _bullet;
    [Export]
    private Sprite2D _Sprite;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _bullet = GD.Load<PackedScene>("res://scenes/combat/bullets/bullet.tscn");
        _bullet.Instantiate();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // 2π (full rotation) per second
        float rotationSpeed = Mathf.Tau;  // Tau is 2π in Godot
        Rotate(rotationSpeed * (float)delta);
    }
}
