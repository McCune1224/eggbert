using Godot;
using System;

public partial class DefaultBullet : Area2D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("Whats good Bullet exist");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    // Check for collision with the player
    public void _on_Bullet_body_entered(Node body)
    {
        if (body is CombatPlayer)
        {
            CombatPlayer player = (CombatPlayer)body;
            QueueFree();
        }
    }
}
