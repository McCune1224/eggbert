using Godot;
using System.Collections.Generic;

public partial class OfficerBacon : StaticBody2D
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
                CutsceneAction.SayDialog(new[] { "You're coming with me." }, speechSound)
            });
        }
    }
}
