using Godot;
using System;

public partial class Gun : Node2D
{
    [Export]
    public int _ammo;
    [Export]
    //Total time (in ms) to do reload for
    public int _reloadTimeDurationMS = 100;
    [Export]
    private bool _fireReady = true;
    [Export]
    private Node2D _BulletType;
    private bool _isReloading = false;

    // Called when the node enters the scene tree for the first time.
    public Vector2 GetBulletDirection()
    {
        // Get mouse position
        var mousePosition = GetGlobalMousePosition();
        GD.Print(mousePosition);
        var playerPosition = GetGlobalPosition();
        var target = mousePosition - playerPosition;

        GD.Print(target.Normalized());
        return target.Normalized();
    }


    //FIXME:THIS SHIT IS BROKEN!!!
    // For some god forsaken reason this dumbass function keeps firing ALL BULLETS AT ONCE????
    // Bleh
    public void Fire()
    {
        if (_ammo == 0)
        {
            Reload();
        }
        else
        {
            Vector2 mouse = GetBulletDirection();
            GD.Print("FIRED ${_ammo} bullets left");
            _ammo -= 1;
        }
    }

    public void SpawnBullet(Vector2 direction)
    {
        PackedScene bullet = GD.Load<PackedScene>("res://combat/bullets/Bullet.tscn");
    }

    public void Reload()
    {
        if (_isReloading)
            return;

        _isReloading = true;
        _fireReady = false;

        SceneTreeTimer reloadTimer = GetTree().CreateTimer(_reloadTimeDurationMS / 1000f);
        reloadTimer.Timeout += OnReloadComplete;
    }

    private void OnReloadComplete()
    {
        _fireReady = true;
        _isReloading = false;
        GD.Print("Reload complete - Ready to fire");
    }


    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("gun_fire") && _fireReady == true)
        {
            Fire();
        }
        if (Input.IsActionPressed("gun_reload"))
        {
            Reload();
        }

    }
}
