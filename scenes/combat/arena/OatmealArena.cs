using System;
using Godot;

public partial class OatmealArena : Node2D
{
    private Camera2D _combatCamera;

    public override void _Ready()
    {
        // Create or get the combat camera
        _combatCamera = GetNode<Camera2D>("Camera2D");

        // Make sure this camera is active
        _combatCamera.MakeCurrent();

        // Configure camera settings for combat
        _combatCamera.Zoom = new Vector2(1.0f, 1.0f); // Adjust zoom as needed
        _combatCamera.Position = new Vector2(0, 0);   // Center of the arena
    }
}
