using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum StepType
{
    SayDialog,
    MoveNpc,
    MovePlayer,
    FaceDirection,
    PlayAnimation,
    CameraMove,
    Wait,
    SetFlag,
    Fade,
    PromptChoice,
    LockPlayer,
    UnlockPlayer,
    Stop
}

[GlobalClass]
public partial class CutsceneStep : Resource
{
    [Export] public StepType Type { get; set; } = StepType.SayDialog;
    [Export] public CutsceneCondition Condition { get; set; }

    [ExportGroup("Dialog")]
    [Export] public string[] DialogLines { get; set; }
    [Export] public DialogVoiceResource DialogVoice { get; set; }

    [ExportGroup("Movement")]
    [Export] public NodePath TargetNode { get; set; }
    [Export] public Vector2 MoveTarget { get; set; }
    [Export] public float MoveDuration { get; set; } = 1.0f;

    [ExportGroup("Animation")]
    [Export] public NodePath AnimationNode { get; set; }
    [Export] public string AnimationName { get; set; }

    [ExportGroup("Timing")]
    [Export] public float WaitSeconds { get; set; }

    [ExportGroup("World Flag")]
    [Export] public string SetFlagKey { get; set; }
    [Export] public Variant SetFlagValue { get; set; }

    [ExportGroup("Fade")]
    [Export] public string FadeDirection { get; set; } = "out";

    [ExportGroup("Choice")]
    [Export] public string[] ChoiceOptions { get; set; }
    [Export] public string[] ChoiceFlags { get; set; }
    [Export] public string[] ChoicePromptLines { get; set; }
    [Export] public DialogVoiceResource ChoicePromptVoice { get; set; }

    public bool ShouldExecute(WorldFlags flags, int lastChoiceIndex)
    {
        if (Condition == null) return true;
        return Condition.IsMet(flags, lastChoiceIndex);
    }

    public async Task Execute(CutsceneController controller)
    {
        switch (Type)
        {
            case StepType.SayDialog:
                await ExecuteSayDialog(controller);
                break;
            case StepType.MoveNpc:
                await ExecuteMoveNpc(controller);
                break;
            case StepType.MovePlayer:
                await ExecuteMovePlayer(controller);
                break;
            case StepType.FaceDirection:
                ExecuteFaceDirection(controller);
                break;
            case StepType.PlayAnimation:
                await ExecutePlayAnimation(controller);
                break;
            case StepType.CameraMove:
                await ExecuteCameraMove(controller);
                break;
            case StepType.Wait:
                await ExecuteWait(controller);
                break;
            case StepType.SetFlag:
                ExecuteSetFlag();
                break;
            case StepType.Fade:
                await ExecuteFade(controller);
                break;
            case StepType.PromptChoice:
                await ExecutePromptChoice(controller);
                break;
            case StepType.LockPlayer:
                Player.Instance.InInteraction = true;
                break;
            case StepType.UnlockPlayer:
                Player.Instance.InInteraction = false;
                break;
            case StepType.Stop:
                controller.Stop();
                break;
        }
    }

    private async Task ExecuteSayDialog(CutsceneController controller)
    {
        var voice = DialogVoice ?? DialogManager.Instance.DefaultVoice;
        var lines = DialogLines != null ? new List<string>(DialogLines) : new List<string>();
        if (lines.Count == 0) return;

        GameLogger.Debug("Cutscene", $"SayDialog: {lines.Count} lines, speaker='{voice?.SpeakerName ?? "narrator"}'");
        DialogManager.Instance.StartDialog(lines, voice);
        await controller.ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        if (controller.Cancelled)
            DialogManager.Instance.Reset();
        else
            GameLogger.Debug("Cutscene", "SayDialog: completed");
    }

    private async Task ExecuteMoveNpc(CutsceneController controller)
    {
        var level = GameController.Instance?.CurrentLevel;
        if (level == null || TargetNode == null)
        {
            GameLogger.Error("Cutscene", "MoveNpc: no level loaded or no TargetNode set.");
            return;
        }

        var npc = level.GetNodeOrNull(TargetNode);
        if (npc == null)
        {
            GameLogger.Error("Cutscene", $"MoveNpc: no node found at '{TargetNode}'.");
            return;
        }

        Vector2 from = npc is Node2D n2d ? n2d.Position : Vector2.Zero;
        GameLogger.Debug("Cutscene", $"MoveNpc: '{TargetNode}' from {from} → {MoveTarget} over {MoveDuration}s");
        var tween = controller.CreateTween();
        tween.TweenProperty(npc, "position", MoveTarget, MoveDuration);
        await controller.ToSignal(tween, Tween.SignalName.Finished);

        if (controller.Cancelled)
            tween.Kill();
        else
            GameLogger.Debug("Cutscene", $"MoveNpc: '{TargetNode}' arrived at {MoveTarget}");
    }

    private async Task ExecuteMovePlayer(CutsceneController controller)
    {
        var player = Player.Instance;
        if (player == null)
        {
            GameLogger.Error("Cutscene", "MovePlayer: no Player instance.");
            return;
        }

        Vector2 from = player.Position;
        GameLogger.Debug("Cutscene", $"MovePlayer: {from} → {MoveTarget} over {MoveDuration}s");
        var tween = controller.CreateTween();
        tween.TweenProperty(player, "position", MoveTarget, MoveDuration);
        await controller.ToSignal(tween, Tween.SignalName.Finished);

        if (controller.Cancelled)
            tween.Kill();
        else
            GameLogger.Debug("Cutscene", $"MovePlayer: arrived at {MoveTarget}");
    }

    private void ExecuteFaceDirection(CutsceneController controller)
    {
        var level = GameController.Instance?.CurrentLevel;
        if (level == null || AnimationNode == null) return;

        var node = level.GetNodeOrNull(AnimationNode);
        if (node is Node2D n2d && n2d.HasNode("AnimationPlayer"))
        {
            var anim = n2d.GetNode<AnimationPlayer>("AnimationPlayer");
            var animName = $"idle_{AnimationName}";
            if (anim.HasAnimation(animName))
            {
                anim.Play(animName);
                GameLogger.Debug("Cutscene", $"FaceDirection: '{AnimationNode}' → '{animName}'");
            }
        }
    }

    private async Task ExecutePlayAnimation(CutsceneController controller)
    {
        var level = GameController.Instance?.CurrentLevel;
        if (level == null || AnimationNode == null) return;

        var node = level.GetNodeOrNull(AnimationNode);
        if (node is Node2D n2d && n2d.HasNode("AnimationPlayer"))
        {
            var anim = n2d.GetNode<AnimationPlayer>("AnimationPlayer");
            if (!anim.HasAnimation(AnimationName))
            {
                GameLogger.Error("Cutscene", $"PlayAnimation: no animation '{AnimationName}' on '{AnimationNode}'.");
                return;
            }
            GameLogger.Debug("Cutscene", $"PlayAnimation: '{AnimationNode}' → '{AnimationName}'");
            anim.Play(AnimationName);
            await controller.ToSignal(anim, AnimationPlayer.SignalName.AnimationFinished);
            GameLogger.Debug("Cutscene", $"PlayAnimation: '{AnimationName}' finished");
        }
    }

    private async Task ExecuteCameraMove(CutsceneController controller)
    {
        var camera = Player.Instance?.GetNodeOrNull<Camera2D>("PlayerCamera");
        if (camera == null)
        {
            GameLogger.Error("Cutscene", "CameraMove: no PlayerCamera found.");
            return;
        }

        GameLogger.Debug("Cutscene", $"CameraMove: offset {camera.Offset} → {MoveTarget} over {MoveDuration}s");
        var tween = controller.CreateTween();
        tween.TweenProperty(camera, "offset", MoveTarget, MoveDuration);
        await controller.ToSignal(tween, Tween.SignalName.Finished);

        if (controller.Cancelled)
            tween.Kill();
        else
            GameLogger.Debug("Cutscene", $"CameraMove: arrived at {MoveTarget}");
    }

    private async Task ExecuteWait(CutsceneController controller)
    {
        GameLogger.Debug("Cutscene", $"Wait: {WaitSeconds}s");
        var timer = controller.GetTree().CreateTimer(WaitSeconds);
        await controller.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    private void ExecuteSetFlag()
    {
        if (!string.IsNullOrEmpty(SetFlagKey))
        {
            WorldFlags.Instance.SetFlag(SetFlagKey, SetFlagValue);
            GameLogger.Debug("Cutscene", $"SetFlag: '{SetFlagKey}' = {SetFlagValue}");
        }
    }

    private async Task ExecuteFade(CutsceneController controller)
    {
        GameLogger.Debug("Cutscene", $"Fade: {FadeDirection}");
        if (FadeDirection == "out")
            await FadeTransition.Instance.PlayFadeOut();
        else
            await FadeTransition.Instance.PlayFadeIn();
    }

    private async Task ExecutePromptChoice(CutsceneController controller)
    {
        if (ChoicePromptLines != null && ChoicePromptLines.Length > 0)
        {
            var promptVoice = ChoicePromptVoice ?? DialogManager.Instance.DefaultVoice;
            GameLogger.Debug("Cutscene", $"PromptChoice: showing prompt ({ChoicePromptLines.Length} lines)");
            DialogManager.Instance.StartDialog(new List<string>(ChoicePromptLines), promptVoice);
            await controller.ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
            if (controller.Cancelled) { DialogManager.Instance.Reset(); return; }
        }

        var choices = ChoiceOptions != null ? new List<string>(ChoiceOptions) : new List<string>();
        if (choices.Count == 0) return;

        GameLogger.Debug("Cutscene", $"PromptChoice: {choices.Count} options — '{string.Join("', '", choices)}'");
        int index = await DialogManager.Instance.PromptChoices(choices);
        controller.LastChoiceIndex = index;
        GameLogger.Info("Cutscene", $"PromptChoice: selected #{index} — '{choices[index]}'");

        if (ChoiceFlags != null && index >= 0 && index < ChoiceFlags.Length && !string.IsNullOrEmpty(ChoiceFlags[index]))
        {
            WorldFlags.Instance.SetFlag(ChoiceFlags[index], true);
            GameLogger.Debug("Cutscene", $"PromptChoice: set flag '{ChoiceFlags[index]}'=true");
        }
    }
}
