using Godot;
using System.Collections.Generic;

public partial class OfficerBacon : StaticBody2D
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