using Godot;
using System;

public partial class DefaultBullet : Area2D
{
    //Timer
    private SceneTreeTimer _timer;
    [Export]
    private float _timeToLive = 4.0f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var timer = GetTree().CreateTimer(_timeToLive);
        timer.Timeout += () => { QueueFree(); };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //Use the current rotation of the bullet to move it in the direction it is facing
        Vector2 direction = new Vector2(Mathf.Cos(Rotation), Mathf.Sin(Rotation));
        Position += direction * 500 * (float)delta;
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
