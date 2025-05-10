using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    // Singleton instance
    private static Player _instance;
    public static Player Instance => _instance;
    // Movement properties
    public readonly float PlayerSpeed = 150.0f;
    private AnimationPlayer _animationPlayer;
    private CollisionShape2D _collisionShape;
    public PlayerCamera Camera { get; private set; }
    private string _facedDirection = "down";

    // Current state
    private bool _inInteraction = false;
    public bool InInteraction
    {
        get => _inInteraction;
        set => _inInteraction = value;
    }

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
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of OverworldPlayer detected!");
        }

        // Get references to child nodes
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        Camera = GetNode<PlayerCamera>("PlayerCamera");

        // Set default animation
        _animationPlayer.Play("idle forward");
    }

    public override void _Process(double delta)
    {
        if (!_inInteraction)
        {
            HandleMovement(delta);
        }
        // PrintCurrentCollisions();
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
        Velocity = direction * PlayerSpeed;
        MoveAndSlide();

        // Update animation based on movement
        UpdateAnimation(direction);

        // Update position in manager
        // if (GameController.Instance != null)
        // {
        //     GameController.Instance.SetPlayerPosition(Position);
        // }
    }

    private void UpdateAnimation(Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            // GD.Print("NO DIRECTION");
            // If we're not moving, use idle animation based on current animation
            string currentAnim = _animationPlayer.CurrentAnimation;
            if (currentAnim.StartsWith("walk"))
            {
                string left = currentAnim.Split(" ")[1];
                _animationPlayer.Play("idle " + currentAnim.Substring(5)); // Remove "walk " prefix
            }
        }
        else
        {
            // GD.Print("DIRECTION");
            // Determine direction based on input
            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
            {
                // Horizontal movement is dominant
                _animationPlayer.Play(direction.X < 0 ? "walk left" : "walk right");
            }
            else
            {
                // Vertical movement is dominant
                _animationPlayer.Play(direction.Y < 0 ? "walk back" : "walk forward");
            }
        }
    }

    // Method to teleport player to a specific position
    public void SetInitialPosition(Vector2 position)
    {
        Position = position;
        // if (GameController.Instance != null)
        // {
        //     GameController.Instance.SetPlayerPosition(position);
        // }
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

    public void OnSaveGame(Godot.Collections.Array<SavedData> saveDataList)
    {
        SavedData newData = new();
        newData.ScenePath = SceneFilePath;
        saveDataList.Add(newData);
    }
}

