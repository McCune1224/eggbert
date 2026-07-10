using Godot;
using System.Collections.Generic;

public partial class GrandpaSmith : StaticBody2D
{
    private DialogVoice _voice = new(null, 0.8f, "Grandpa Smith");
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
                        "Good to see you again, Eggbert!",
                        "The eggsile district has been quiet lately.",
                    }, _voice),
                });
            }
            else
            {
                CutsceneController.Instance.StartCutscene(new List<CutsceneAction>
                {
                    CutsceneAction.SayDialog(new[]
                    {
                        "This is a cutscene!",
                        "Watch me slide to the right...",
                        "...and set a WorldFlag when done."
                    }, _voice),
                    CutsceneAction.MoveNpc("GrandpaSmith", new Vector2(300, Position.Y), 2.0f),
                    CutsceneAction.Wait(0.5f),
                    CutsceneAction.SetFlag("MetGrandpa", true),
                });
            }
        }
    }
}
