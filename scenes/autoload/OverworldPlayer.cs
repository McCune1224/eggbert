using Godot;
using System;

public partial class OverworldPlayer : CharacterBody2D
{
    // Singleton instance
    private static OverworldPlayer _instance;
    public static OverworldPlayer Instance => _instance;

    // Movement properties
    [Export]
    private float PLAYER_SPEED = 150.0f;
    private AnimatedSprite2D _sprite;
    private CollisionShape2D _collisionShape;
    private string facedDirection = "down";

    // Current state
    private bool _inInteraction = false;
    public bool InInteraction
    {
        get => _inInteraction;
        set => _inInteraction = value;
    }

    public override void _Ready()
    {
        AddToGroup("player");
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of OverworldPlayer detected!");
        }

        // Get references to child nodes
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

        // Set default animation
        _sprite.Play("idle forward");
    }

    public override void _Process(double delta)
    {
        if (!_inInteraction)
        {
            HandleMovement(delta);
        }
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

        // Update position in manager
        if (OverworldManager.Instance != null)
        {
            OverworldManager.Instance.SetPlayerPosition(Position);
        }
    }

    private void UpdateAnimation(Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            // GD.Print("NO DIRECTION");
            // If we're not moving, use idle animation based on current animation
            string currentAnim = _sprite.Animation;
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

    // Method to teleport player to a specific position
    public void SetInitialPosition(Vector2 position)
    {
        Position = position;
        if (OverworldManager.Instance != null)
        {
            OverworldManager.Instance.SetPlayerPosition(position);
        }
    }

    // Method to start an interaction (disable movement)
    public void StartInteraction()
    {
        _inInteraction = true;
    }

    // Method to end an interaction (enable movement)
    public void EndInteraction()
    {
        _inInteraction = false;
    }
}
