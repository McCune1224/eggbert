using Godot;
using System;

public partial class CombatOpponent : Node2D
{
    private Node2D _bulletScene;
    private Node2D[] _bulletPool = new Node2D[1000];
    [Export]
    private Sprite2D _Sprite;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _bulletScene = (Node2D)GD.Load<PackedScene>("res://scenes/combat/bullets/bullet.tscn").Instantiate();
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // 2π (full rotation) per second
        float rotationSpeed = Mathf.Tau;  // Tau is 2π in Godot
        Rotate(rotationSpeed * (float)delta);
        Fire();
    }

    public void Fire()
    {
        Node2D bullet = (Node2D)_bulletScene.Duplicate();
        bullet.GlobalPosition = GlobalPosition; // Or use Position for local coordinates
        GetTree().Root.AddChild(bullet); // Or use GetParent().AddChild(bullet) to add to current parent
    }
}
