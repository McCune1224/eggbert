using Godot;
using System.Collections.Generic;

public partial class GrandpaSmith : StaticBody2D
{
    [Export] public DialogVoiceResource Voice;

    public override void _Ready()
    {
        var trigger = GetNode<CutsceneTrigger>("CutsceneTrigger");
        trigger.Triggered += OnTriggered;
    }

    private void OnTriggered()
    {
        if (WorldFlags.Instance.HasFlag("MetGrandpa"))
        {
            CutsceneController.Instance.StartCutscene(new List<CutsceneAction>
            {
                CutsceneAction.SayDialog(new[]
                {
                    "Good to see you again, Eggbert!",
                    "The eggsile district has been quiet lately.",
                }, Voice),
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
                }, Voice),
                CutsceneAction.MoveNpc("GrandpaSmith", new Vector2(300, Position.Y), 2.0f),
                CutsceneAction.Wait(0.5f),
                CutsceneAction.SetFlag("MetGrandpa", true),
            });
        }
    }
}