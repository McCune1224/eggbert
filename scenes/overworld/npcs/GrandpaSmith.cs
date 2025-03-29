using Godot;
using System;
using System.Collections.Generic;

public partial class GrandpaSmith : Area2D
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
            GameController.Instance.DialogManager.Reset();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Check for the 'E' key press when the prompt is visible
        if (interactionPrompt.Visible && Input.IsActionJustPressed("interact"))
        {
            interactionPrompt.Visible = false;

            List<string> dialog = new(); ;
            dialog.Add("This is the first item. Very Cool");
            dialog.Add("Item Two! Okay, not much to see here...");
            dialog.Add("THE GRAND FINALE!");
            GD.Print("MAN", GameController.Instance.DialogManager);
            GameController.Instance.DialogManager.StartDialog(GetTree(), GlobalPosition, dialog);
            // PackedScene tb = ResourceLoader.Load<PackedScene>("res://scenes/ui/TextBox.tscn");
            // TextBox cb = tb.Instantiate<TextBox>();
            // GetTree().Root.AddChild(cb);
            // GD.Print("TODO: ", cb);
            // cb.DisplayText(dialog[0]);

            // // This is where you'd put the code for what happens when the player interacts
            // // For example: Start a dialogue, give an item, etc.
            // dialogueLabel.Text = "Hello, young one! You look yoked out..."; // Set the dialogue text
            // dialogueLabel.Position = new Vector2(0, -50); // Position above the NPC
            // dialogueLabel.HorizontalAlignment = HorizontalAlignment.Left;
            //
            // interactionPrompt.Visible = false;
            // dialogueLabel.Visible = true;
            // //show the dialogueLabel
        }
    }
}
