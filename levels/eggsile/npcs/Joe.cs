using Godot;
using System.Collections.Generic;

public partial class Joe : StaticBody2D
{
    private ComponentPromptCollision promptCollision;

    [Export]
    private AudioStream speechSound;

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
                    "I hate the morning shift. There's always so much",
                    "to clean down here and so little help."
                }, speechSound)
            });
        }
    }
}
