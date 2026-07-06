using Godot;
using System.Collections.Generic;

public partial class GrandpaSmith : StaticBody2D
{
    private AudioStream speechSound = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");
    private ComponentPromptCollision promptCollision;

    public override void _Ready()
    {
        promptCollision = GetNode<ComponentPromptCollision>("ComponentPromptCollision");
    }

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
                }, speechSound),
                CutsceneAction.MoveNpc("GrandpaSmith", new Vector2(300, Position.Y), 2.0f),
                CutsceneAction.Wait(0.5f),
                CutsceneAction.SetFlag("MetGrandpa", true),
            });
        }
    }
}
