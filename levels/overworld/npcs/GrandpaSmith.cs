using Godot;
using System;
using System.Collections.Generic;

public partial class GrandpaSmith : StaticBody2D
{
    // Reference to a label that will show the interaction prompt
    private Label dialogueLabel;
    private AudioStream speechSound = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");
    private ComponentPromptCollision promptCollision;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Add the label as a child of this node
        dialogueLabel = new Label();
        AddChild(dialogueLabel);
        promptCollision = GetNode<ComponentPromptCollision>("ComponentPromptCollision");
    }


    // public override void _Input(InputEvent @event)
    // {
    //     if (promptCollision.isPromptVisible() && Input.IsActionJustPressed("interact"))
    //     {
    //         promptCollision.HidePrompt();
    //         List<string> dialog = new();
    //         dialog.Add("This is the first item. Very Cool");
    //         DialogManager.Instance.StartDialog(dialog, speechSound);
    //     }
    // }

    public override void _Input(InputEvent @event)
    {
        if (promptCollision.isPromptVisible() && Input.IsActionJustPressed("interact"))
        {
            promptCollision.HidePrompt();
            CutsceneController.Instance.StartCutscene(new List<CutsceneAction>
            {
                CutsceneAction.SayDialog(new[]
                {
                    "This is a cutscene!",
                    "Watch me slide to the right...",
                    "...and set a WorldFlag when done."
                }),
                CutsceneAction.MoveNpc("GrandpaSmith", new Vector2(300, Position.Y), 2.0f),
                CutsceneAction.Wait(0.5f),
                CutsceneAction.SetFlag("MetGrandpa", true),
            });
        }
    }


}
