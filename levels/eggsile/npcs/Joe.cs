using Godot;
using System;
using System.Collections.Generic;

public partial class Joe : Area2D
{

    private ComponentPromptCollision promptCollision;
    private Label dialogueLabel;
    [Export]
    private AudioStream speechSound = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");
    [Export]
    Vector2 _labelPositionOffset = new Vector2(100, 100);
    Vector2 _labelPosition;

    public override void _Ready()
    {
        // Connect signals for when objects enter/exit this Area2D
        _labelPosition = GlobalPosition + _labelPositionOffset;
        promptCollision = GetNode<ComponentPromptCollision>("ComponentPromptCollision");

        // Add the label as a child of this node
        dialogueLabel = new Label();
        AddChild(dialogueLabel);
    }


    public override void _Input(InputEvent @event)
    {
        // Check for the 'E' key press when the prompt is visible
        if (promptCollision.isPromptVisible() && Input.IsActionJustPressed("interact"))
        {
            promptCollision.HidePrompt();

            List<string> dialog = new(); ;
            dialog.Add("I hate the morning shift. Theres always so much to clean down here and so little help.");
            DialogManager.Instance.StartDialog(dialog, speechSound);
            // PackedScene tb = ResourceLoader.Load<PackedScene>("res://ui/TextBox.tscn");
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
