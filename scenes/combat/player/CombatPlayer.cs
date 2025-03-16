using Godot;
using Godot.Collections;
using System;

public partial class CombatPlayer : CharacterBody2D
{
    // Movement properties
    [Export]
    private float PLAYER_SPEED = 150.0f;
    private AnimationPlayer _sprite;
    private CollisionShape2D _collisionShape;
    private string facedDirection = "down";

    public Array<Node2D> GetCollidingBodies()
    {
        var colliders = new Array<Node2D>();

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            colliders.Add((Node2D)collision.GetCollider());
        }

        return colliders;
    }
    // public void PrintCurrentCollisions()
    // {
    //     Array<Node2D> collision = GetCollidingBodies();
    //     foreach (var col in collision)
    //     {
    //         GD.Print(col);
    //     }
    // }

    public override void _Ready()
    {
        AddToGroup("player");
        // Get references to child nodes
        _sprite = GetNode<AnimationPlayer>("AnimatedSprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

        // Set default animation
        _sprite.Play("idle forward");
    }

    public override void _Process(double delta)
    {
        HandleMovement(delta);
    }

    private void HandleMovement(double delta)
    {
        // Get input direction
        Vector2 direction = Input.GetVector("player_left", "player_right", "player_up", "player_down");

        // Normalize the vector to ensure consistent movement speed in all directions
        if (direction.Length() > 1.0f)
        {
            direction = direction.Normalized();
        }

        // Set the velocity and move the player
        Velocity = direction * PLAYER_SPEED;
        MoveAndSlide();

        // Update animation based on movement
        UpdateAnimation(direction);

    }

    private void UpdateAnimation(Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            // GD.Print("NO DIRECTION");
            // If we're not moving, use idle animation based on current animation
            string currentAnim = _sprite.CurrentAnimation;
            if (currentAnim.StartsWith("walk"))
            {
                string left = currentAnim.Split(" ")[1];
                _sprite.Play("idle " + currentAnim.Substring(5)); // Remove "walk " prefix
            }
        }
        else
        {
            // GD.Print("DIRECTION");
            // Determine direction based on input
            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
            {
                // Horizontal movement is dominant
                _sprite.Play(direction.X < 0 ? "walk left" : "walk right");
            }
            else
            {
                // Vertical movement is dominant
                _sprite.Play(direction.Y < 0 ? "walk back" : "walk forward");
            }
        }
    }

}
