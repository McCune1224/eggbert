using Godot;
using System;

public enum Direction { Up, Down, Left, Right }
public partial class Player : CharacterBody2D
{
    private int _playerSpeed = 150;
    private Direction _playerDirection = Direction.Down;
    private AnimationPlayer _ap;
    public override void _Ready()
    {
        _ap = GetNode<AnimationPlayer>("PlayerSprite/AnimationPlayer");
        return;
    }
    public override void _Process(double delta)
    {
        Vector2 dir = GetNextPayerDirection();
        SetPlayerAnmiation();
        return;
    }
    public override void _PhysicsProcess(double delta)
    {
        Vector2 direction = GetNextPayerDirection();
        Velocity = direction * _playerSpeed;
        MoveAndSlide();
        return;
    }

    public Vector2 GetNextPayerDirection()
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
            _ap.Play("walk back");
            _playerDirection = Direction.Up;
        }
        if (Input.IsActionPressed("player_down"))
        {
            _ap.Play("walk forward");
            _playerDirection = Direction.Down;
        }
        if (Input.IsActionPressed("player_left"))
        {
            _ap.Play("walk left");
            _playerDirection = Direction.Left;
        }
        if (Input.IsActionPressed("player_right"))
        {
            _ap.Play("walk right");
            _playerDirection = Direction.Right;
        }

        return;
    }
}

