using Godot;
using System.Collections.Generic;

public partial class Joe : StaticBody2D
{
    [Export] public DialogVoiceResource Voice;

    public override void _Ready()
    {
        var trigger = GetNode<CutsceneTrigger>("CutsceneTrigger");
        trigger.Triggered += OnTriggered;
    }

    private void OnTriggered()
    {
        CutsceneController.Instance.StartCutscene(new List<CutsceneAction>
        {
            CutsceneAction.SayDialog(new[]
            {
                "I hate the morning shift. There's always so much",
                "to clean down here and so little help."
            }, Voice)
        });
    }
}