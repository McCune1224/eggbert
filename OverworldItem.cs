using Godot;
using System;

public partial class OverworldItem : Area2D
{
    private RichTextLabel _tl;
    private string defaultText = "Press 'E' to interact";
    private bool isActiveText = false;

    public override void _Ready()
    {
        // Adds body entered signal?
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }
    private void OnBodyEntered(Node2D body)
    {
        if (body.IsClass("CharacterBody2D") && !isActiveText)
        {
            _tl = new RichTextLabel();
            _tl.Text = defaultText;
            _tl.Position = new Vector2(100, 100); // Adjust position as needed
            _tl.Size = new Vector2(200, 50); // Adjust size as needed

            // Add the RichTextLabel to the scene
            GetTree().Root.AddChild(_tl);
            isActiveText = true;
        }

    }
    private void OnBodyExited(Node2D body)
    {
        if (isActiveText)
        {
            isActiveText = false;
            _tl.QueueFree();
            GD.Print("Smell ya later");
            // Optional: Remove the text after a delay
            // var timer = GetTree().CreateTimer(2.0);
            // timer.Timeout += () => { _tl.QueueFree(); isActiveText = false; };
        }
    }
}
