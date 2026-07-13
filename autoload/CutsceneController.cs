using Godot;
using System.Collections.Generic;
using System.Threading;
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
    public int LastChoiceIndex { get; private set; } = -1;

    private CancellationTokenSource _cts;

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
        _cts = new CancellationTokenSource();

        foreach (var action in actions)
        {
            if (_cts.IsCancellationRequested)
                break;
            await ExecuteAction(action);
        }

        _isPlaying = false;
        _cts = null;
        Player.Instance.InInteraction = false;
    }

    /// <summary>Abort the current cutscene. The in-progress action finishes, then no further actions run.</summary>
    public void Stop()
    {
        if (!_isPlaying || _cts == null) return;
        _cts.Cancel();
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

            case CutsceneActionType.MovePlayer:
                await MovePlayer(action);
                break;

            case CutsceneActionType.FaceDirection:
                FaceDirection(action);
                break;

            case CutsceneActionType.PlayAnimation:
                await PlayAnimation(action);
                break;

            case CutsceneActionType.CameraMove:
                await CameraMove(action);
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

            case CutsceneActionType.PromptChoice:
                await PromptChoice(action);
                break;

            case CutsceneActionType.Stop:
                Stop();
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

        if (_cts.IsCancellationRequested)
            tween.Kill();
    }

    private async Task MovePlayer(CutsceneAction action)
    {
        var target = (Vector2)action.Params["target_position"];
        var duration = (float)action.Params["duration"];

        var player = Player.Instance;
        if (player == null)
        {
            GD.PrintErr("Cutscene: no Player instance.");
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(player, "position", target, duration);
        await ToSignal(tween, Tween.SignalName.Finished);

        if (_cts.IsCancellationRequested)
            tween.Kill();
    }

    private void FaceDirection(CutsceneAction action)
    {
        var nodePath = action.Params["node_path"].AsString();
        var direction = action.Params["direction"].AsString();

        Node currentLevel = GameController.Instance.CurrentLevel;
        if (currentLevel == null) return;

        var node = currentLevel.GetNode(nodePath);
        if (node == null)
        {
            GD.PrintErr($"Cutscene: no node found at '{nodePath}' for FaceDirection.");
            return;
        }

        if (node is Node2D n2D && n2D.HasNode("AnimationPlayer"))
        {
            var anim = n2D.GetNode<AnimationPlayer>("AnimationPlayer");
            var animName = $"idle_{direction}";
            if (anim.HasAnimation(animName))
                anim.Play(animName);
            else
                GD.PrintErr($"Cutscene: no idle animation '{animName}' on '{nodePath}'.");
        }
    }

    private async Task PlayAnimation(CutsceneAction action)
    {
        var nodePath = action.Params["node_path"].AsString();
        var animName = action.Params["animation_name"].AsString();

        Node currentLevel = GameController.Instance.CurrentLevel;
        if (currentLevel == null) return;

        var node = currentLevel.GetNode(nodePath);
        if (node == null)
        {
            GD.PrintErr($"Cutscene: no node found at '{nodePath}' for PlayAnimation.");
            return;
        }

        if (node is Node2D n2D && n2D.HasNode("AnimationPlayer"))
        {
            var anim = n2D.GetNode<AnimationPlayer>("AnimationPlayer");
            if (!anim.HasAnimation(animName))
            {
                GD.PrintErr($"Cutscene: no animation '{animName}' on '{nodePath}'.");
                return;
            }
            anim.Play(animName);
            await ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
        }
    }

    private async Task CameraMove(CutsceneAction action)
    {
        var target = (Vector2)action.Params["target_position"];
        var duration = (float)action.Params["duration"];

        var camera = Player.Instance?.GetNodeOrNull<Camera2D>("PlayerCamera");
        if (camera == null)
        {
            GD.PrintErr("Cutscene: no PlayerCamera found.");
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(camera, "offset", target, duration);
        await ToSignal(tween, Tween.SignalName.Finished);

        if (_cts.IsCancellationRequested)
            tween.Kill();
    }

    private async Task SayDialog(CutsceneAction action)
    {
        var lines = new List<string>(action.Params["lines"].AsGodotArray<string>());
        DialogVoiceResource voice = null;
        if (action.Params.ContainsKey("voice") && action.Params["voice"].VariantType != Variant.Type.Nil)
            voice = (DialogVoiceResource)action.Params["voice"];
        DialogManager.Instance.StartDialog(lines, voice);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        if (_cts.IsCancellationRequested)
            DialogManager.Instance.Reset();
    }

    private async Task Wait(CutsceneAction action)
    {
        var seconds = (float)action.Params["duration"];
        var timer = GetTree().CreateTimer(seconds);
        await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
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

    private async Task PromptChoice(CutsceneAction action)
    {
        var choices = new List<string>(action.Params["choices"].AsGodotArray<string>());
        var flagKeys = new List<string>(action.Params["flag_keys"].AsGodotArray<string>());
        int index = await DialogManager.Instance.PromptChoices(choices);
        LastChoiceIndex = index;
        if (index >= 0 && index < flagKeys.Count && !string.IsNullOrEmpty(flagKeys[index]))
            WorldFlags.Instance.SetFlag(flagKeys[index], true);
    }
}