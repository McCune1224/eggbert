using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The type of action a cutscene step performs. Each member controls a distinct behavior:
/// SayDialog — displays dialog lines via DialogManager.
/// MoveNpc — moves a named NPC node to a target position over a duration.
/// MovePlayer — moves the player character to a target position.
/// FaceDirection — plays a face-direction idle animation on a node (e.g. "idle_down").
/// PlayAnimation — plays a named animation on an AnimationPlayer node.
/// CameraMove — smoothly moves the player camera to an offset position.
/// Wait — pauses execution for a specified number of seconds.
/// SetFlag — sets a world flag to a value in WorldFlags.
/// Fade — fades the screen in or out.
/// PromptChoice — presents the player with a choice prompt; the selection index becomes a world flag.
/// LockPlayer — disables player movement/input for the duration of the cutscene.
/// UnlockPlayer — re-enables player movement/input.
/// Stop — halts the currently running cutscene.
/// DialogBranch — advances a DialogBranch resource from a specific start node, enabling branching narrative.
/// </summary>
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
    Stop,
    DialogBranch
}

/// <summary>
/// A single step in a cutscene sequence, defined by <see cref="StepType"/> and conditionally
/// executed via <see cref="CutsceneCondition"/>. Fields scoped by [ExportGroup] are only
/// meaningful for their associated step types (see each field's summary).
/// </summary>
[GlobalClass]
public partial class CutsceneStep : Resource
{
    /// <summary>
    /// Which type of step this is. Determines which [Export] fields are interpreted.
    /// </summary>
    [Export] public StepType Type { get; set; } = StepType.SayDialog;
    /// <summary>
    /// Condition that must be met for this step to execute. If null, the step always runs.
    /// </summary>
    [Export] public CutsceneCondition Condition { get; set; }

    /// <summary>Lines displayed when <see cref="StepType"/> is SayDialog or PromptChoice. Ignored by other types.</summary>
    [ExportGroup("Dialog")]
    [Export] public string[] DialogLines { get; set; }
    /// <summary>Voice resource for SayDialog and PromptChoice steps. Falls back to <see cref="DialogManager.Instance.DefaultVoice"/> if null.</summary>
    [Export] public DialogVoiceResource DialogVoice { get; set; }

    /// <summary>NodePath to the NPC node to move. Used by MoveNpc only.</summary>
    [ExportGroup("Movement")]
    [Export] public NodePath TargetNode { get; set; }
    /// <summary>Target world position the node moves toward. Used by MoveNpc and MovePlayer.</summary>
    [Export] public Vector2 MoveTarget { get; set; }
    /// <summary>Duration of the move in seconds. Used by MoveNpc and MovePlayer.</summary>
    [Export] public float MoveDuration { get; set; } = 1.0f;

    /// <summary>NodePath to the node whose animation is played. Used by FaceDirection and PlayAnimation.</summary>
    [ExportGroup("Animation")]
    [Export] public NodePath AnimationNode { get; set; }
    /// <summary>Name of the animation to play (FaceDirection prepends "idle_"; PlayAnimation uses this directly). Used by FaceDirection and PlayAnimation.</summary>
    [Export] public string AnimationName { get; set; }

    /// <summary>Number of seconds to pause execution. Used by Wait only.</summary>
    [ExportGroup("Timing")]
    [Export] public float WaitSeconds { get; set; }

    /// <summary>The flag key to set. Used by SetFlag only.</summary>
    [ExportGroup("World Flag")]
    [Export] public string SetFlagKey { get; set; }
    /// <summary>The value to assign to <see cref="SetFlagKey"/>. Used by SetFlag only.</summary>
    [Export] public Variant SetFlagValue { get; set; }

    /// <summary>"out" fades the screen to black; "in" fades it back in. Used by Fade only.</summary>
    [ExportGroup("Fade")]
    [Export] public string FadeDirection { get; set; } = "out";

    /// <summary>Displayed choice labels shown to the player. Used by PromptChoice only.</summary>
    [ExportGroup("Choice")]
    [Export] public string[] ChoiceOptions { get; set; }
    /// <summary>World flags set to true when the player picks the corresponding option. Used by PromptChoice only.</summary>
    [Export] public string[] ChoiceFlags { get; set; }
    /// <summary>Prompt lines shown before the choice menu. Used by PromptChoice only.</summary>
    [Export] public string[] ChoicePromptLines { get; set; }
    /// <summary>Voice resource for choice prompt audio. Used by PromptChoice only.</summary>
    [Export] public DialogVoiceResource ChoicePromptVoice { get; set; }

    /// <summary>The DialogBranch resource to advance. Used by DialogBranch only.</summary>
    [ExportGroup("Dialog Branch")]
    [Export] public Resource DialogBranchResource { get; set; }
    /// <summary>The starting node id within <see cref="DialogBranchResource"/>. Used by DialogBranch only.</summary>
    [Export] public string StartNodeId { get; set; } = "";

    /// <summary>
    /// Checks whether the cutscene condition is met for this step. Returns true if <see cref="Condition"/> is null.
    /// </summary>
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
            case StepType.DialogBranch:
                await ExecuteDialogBranch(controller);
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

    private async Task ExecuteDialogBranch(CutsceneController controller)
    {
        if (DialogBranchResource is not DialogBranch branch)
        {
            GameLogger.Error("Cutscene", "DialogBranch: no DialogBranch resource assigned.");
            return;
        }

        GameLogger.Debug("Cutscene", $"DialogBranch: '{branch.ResourcePath}', start='{StartNodeId}'");
        await controller.RunDialogBranch(branch, StartNodeId);
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
