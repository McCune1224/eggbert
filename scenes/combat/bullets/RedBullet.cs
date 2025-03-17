using Godot;
using System;

public partial class RedBullet : Area2D
{
    // Properties for bullet behavior
    [Export] private float speed = 200.0f; // Bullet speed in pixels per second
    [Export] private float lifetime = 3.0f; // How long the bullet exists before auto-destroying
    [Export] private Vector2 direction = Vector2.Right; // Default direction is right

    private float aliveTime = 0.0f;

    // Called when the node enters the scene tree for the first time
    public override void _Ready()
    {
        // You could initialize anything specific here if needed
    }

    // Called every frame
    public override void _Process(double delta)
    {
        // Convert delta to float for easier math
        float deltaFloat = (float)delta;

        // Move the bullet in the specified direction
        Position += direction.Normalized() * speed * deltaFloat;

        // Update the rotation to match the movement direction
        Rotation = Mathf.Atan2(direction.Y, direction.X);

        // Track lifetime of the bullet
        aliveTime += deltaFloat;
        if (aliveTime >= lifetime)
        {
            QueueFree(); // Destroy the bullet when lifetime expires
        }
    }

    // Set the direction and optionally speed of the bullet
    public void SetDirection(Vector2 newDirection, float? newSpeed = null)
    {
        direction = newDirection.Normalized();
        if (newSpeed.HasValue)
        {
            speed = newSpeed.Value;
        }
    }

    // Reset the lifetime of the bullet (for reuse from pool)
    public void ResetLifetime()
    {
        aliveTime = 0.0f;
    }

    // What happens when bullet hits something
    private void OnAreaEntered(Area2D area)
    {
        // Here you can add logic for what happens when the bullet hits something
        // For example, deal damage, play effects, etc.

        // Currently, we'll just destroy the bullet when it hits another Area2D
        QueueFree();
    }

    // Called when the bullet enters a body (solid object)
    private void OnBodyEntered(Node2D body)
    {
        // Destroy the bullet when it hits a solid object
        QueueFree();
    }
}
