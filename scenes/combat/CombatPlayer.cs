using Godot;
using System;

public partial class CombatPlayer : CharacterBody2D
{
    private int _playerSpeed = 800;
    public const float Speed = 300.0f;
    private AnimationPlayer _ap;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 direction = GetPlayerInput();
        Velocity = direction * _playerSpeed;
        MoveAndSlide();
        return;
    }

    public Vector2 GetPlayerInput()
    {
        Vector2 direction = Vector2.Zero;
        if (Input.IsActionPressed("player_right")) { direction.X += 1; }
        if (Input.IsActionPressed("player_left")) { direction.X -= 1; }
        if (Input.IsActionPressed("player_up")) { direction.Y -= 1; }
        if (Input.IsActionPressed("player_down")) { direction.Y += 1; }

        return direction.Normalized();
    }
}
