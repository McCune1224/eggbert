using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Signal-chain cutscene controller. Queue up actions and they execute sequentially.
/// </summary>
public partial class CutsceneController : Node
{
    private static CutsceneController _instance;
    public static CutsceneController Instance => _instance;

    private bool _isPlaying;
    public bool IsPlaying => _isPlaying;

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
            QueueFree();
    }

    /// <summary>Start a cutscene. Actions execute in order. No-op if one is already running.</summary>
    public async void StartCutscene(List<CutsceneAction> actions)
    {
        if (_isPlaying) return;
        _isPlaying = true;

        foreach (var action in actions)
            await ExecuteAction(action);

        _isPlaying = false;
    }

    private async Task ExecuteAction(CutsceneAction action)
    {
        switch (action.Type)
        {
            case CutsceneActionType.LockPlayer:
                Player.Instance.InInteraction = true;
                break;

            case CutsceneActionType.UnlockPlayer:
                Player.Instance.InInteraction = false;
                break;

            case CutsceneActionType.MoveNpc:
                await MoveNpc(action);
                break;

            case CutsceneActionType.SayDialog:
                await SayDialog(action);
                break;

            case CutsceneActionType.Wait:
                await Wait(action);
                break;

            case CutsceneActionType.SetFlag:
                SetWorldFlag(action);
                break;

            case CutsceneActionType.Fade:
                await Fade(action);
                break;
        }
    }

    private async Task MoveNpc(CutsceneAction action)
    {
        var npcPath = action.Params["npc_path"].AsString();
        var target = (Vector2)action.Params["target_position"];
        var duration = (float)action.Params["duration"];

        Node currentLevel = GameController.Instance.CurrentLevel;
        if (currentLevel == null)
        {
            GD.PrintErr("Cutscene: no level loaded, can't move NPC.");
            return;
        }

        // Guard: don't try to move the level root itself
        if (npcPath == "." || npcPath == "..")
        {
            GD.PrintErr($"Cutscene: can't move relative path '{npcPath}'. Use the NPC's node name (e.g. \"GrandpaSmith\").");
            return;
        }

        var npc = currentLevel.GetNode(npcPath);
        if (npc == null)
        {
            GD.PrintErr($"Cutscene: no node found at '{npcPath}' under current level.");
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(npc, "position", target, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task SayDialog(CutsceneAction action)
    {
        var lines = new List<string>(action.Params["lines"].AsGodotArray<string>());
        DialogManager.Instance.StartDialog(lines, null);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
    }

    private async Task Wait(CutsceneAction action)
    {
        var seconds = (float)action.Params["duration"];
        await Task.Delay((int)(seconds * 1000));
    }

    private void SetWorldFlag(CutsceneAction action)
    {
        var key = action.Params["key"].AsString();
        var value = action.Params["value"];
        WorldFlags.Instance.SetFlag(key, value);
    }

    private async Task Fade(CutsceneAction action)
    {
        var type = action.Params["type"].AsString();
        if (type == "out")
            await FadeTransition.Instance.PlayFadeOut();
        else
            await FadeTransition.Instance.PlayFadeIn();
    }
}
