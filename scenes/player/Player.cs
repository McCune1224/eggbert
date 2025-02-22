using Godot;
using System;

public enum Direction { Up, Down, Left, Right }
public partial class Player : CharacterBody2D
{
    private int _playerSpeed = 150;
    private AnimationPlayer _ap;
    public override void _Ready()
    {
        _ap = GetNode<AnimationPlayer>("PlayerSprite/AnimationPlayer");
        return;
    }
    public override void _Process(double delta)
    {
        Vector2 dir = GetPlayerInput();
        SetPlayerAnmiation();
        return;
    }
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
    public void SetPlayerAnmiation()
    {

        if (Input.IsActionPressed("player_up"))
        {
            _ap.Play("walking_up");
        }
        if (Input.IsActionPressed("player_down"))
        {
            _ap.Play("walking_down");
        }
        if (Input.IsActionPressed("player_left"))
        {
            _ap.Play("walking_left");
        }
        if (Input.IsActionPressed("player_right"))
        {
            _ap.Play("walking_right");
        }

        return;
    }
}

