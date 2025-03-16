using Godot;
using System;

public partial class OfficerBacon : Area2D
{
    // Reference to a label that will show the interaction prompt
    private Label interactionPrompt;
    private Label dialogueLabel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Connect signals for when objects enter/exit this Area2D
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        // Create the interaction prompt label
        interactionPrompt = new Label();
        interactionPrompt.Text = "Press 'E' to interact";
        interactionPrompt.Position = new Vector2(0, -50); // Position above the NPC
        interactionPrompt.HorizontalAlignment = HorizontalAlignment.Center;

        // Make the prompt invisible initially
        interactionPrompt.Visible = false;

        // Add the label as a child of this node
        dialogueLabel = new Label();
        AddChild(dialogueLabel);
        AddChild(interactionPrompt);
    }

    // Called when another physics body enters this Area2D
    private void OnBodyEntered(Node2D body)
    {
        // Check if the body that entered is the player
        if (body.IsInGroup("player")) // Adjust this to match your player node name
        {
            // Show the interaction prompt
            interactionPrompt.Visible = true;
        }
    }

    // Called when a physics body exits this Area2D
    private void OnBodyExited(Node2D body)
    {
        // Check if the body that left is the player
        if (body.IsInGroup("player")) // Adjust this to match your player node name
        {
            // Hide the interaction prompt
            dialogueLabel.Visible = false;
            interactionPrompt.Visible = false;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Check for the 'E' key press when the prompt is visible
        if (interactionPrompt.Visible && Input.IsActionJustPressed("interact"))
        {
            // This is where you'd put the code for what happens when the player interacts
            // For example: Start a dialogue, give an item, etc.
            dialogueLabel.Text = "You're coming with me."; // Set the dialogue text
            dialogueLabel.Position = new Vector2(0, -50); // Position above the NPC
            dialogueLabel.HorizontalAlignment = HorizontalAlignment.Left;
            interactionPrompt.Visible = false;
            dialogueLabel.Visible = true;
            GetTree().CreateTimer(2).Timeout += () =>
            {
                GameController ow = GameController.Instance;
                ow.LoadCombatScene("res://scenes/combat/arena/GenericArena.tscn");
            };
        }
    }
}
