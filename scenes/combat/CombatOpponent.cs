using Godot;
using System;

public partial class CombatOpponent : Node2D
{
    private Node2D _bulletScene;
    private double _reloadTime = 0.08;
    private bool _readyToFire = true;
    private int _direction = 1;
    private Timer _flipTimer;
    [Export]
    private Sprite2D _Sprite;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _bulletScene = (Node2D)GD.Load<PackedScene>("res://scenes/combat/bullets/bullet.tscn").Instantiate();
        _flipTimer = new Timer();
        _flipTimer.Timeout += () => { _direction = _direction * -1; };
        _flipTimer.OneShot = false;
        AddChild(_flipTimer);
        _flipTimer.Start(4);

    }
    public override void _Process(double delta)
    {
        //NOTE: UNIT CIRCLE MY BELOVED (Tau = 2Pi)
        Rotate((float)Math.Tau / 4 * (float)delta * _direction);
        Fire();
    }

    public void Fire()
    {
        if (_readyToFire)
        {
            _readyToFire = false;
            Node2D bullet = (Node2D)_bulletScene.Duplicate();
            bullet.GlobalPosition = GlobalPosition; // Or use Position for local coordinates
            bullet.Rotation = this.Rotation;
            GetTree().Root.AddChild(bullet); // Or use GetParent().AddChild(bullet) to add to current parent
            SceneTreeTimer reloadTimer = GetTree().CreateTimer(_reloadTime);
            reloadTimer.Timeout += () => { _readyToFire = true; };
        }
    }
}
