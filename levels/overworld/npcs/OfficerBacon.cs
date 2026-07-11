using Godot;
using System.Collections.Generic;

public partial class OfficerBacon : StaticBody2D
{
    [Export] public DialogVoiceResource Voice;
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

            if (WorldFlags.Instance.HasFlag("MetGrandpa"))
            {
                CutsceneController.Instance.StartCutscene(new List<CutsceneAction>
                {
                    CutsceneAction.SayDialog(new[]
                    {
                        "So you know Grandpa Smith? Small world.",
                        "Stay out of trouble and we'll get along fine."
                    }, Voice),
                    CutsceneAction.SetFlag("MetOfficer", true),
                });
            }
            else
            {
                CutsceneController.Instance.StartCutscene(new List<CutsceneAction>
                {
                    CutsceneAction.SayDialog(new[] { "You're coming with me." }, Voice)
                });
            }
        }
    }
}
