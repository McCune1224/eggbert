using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Cutscene director for AnimationPlayer-driven cutscenes (replaces the fragile
/// async step-runner for cinematics). A cutscene is a small .tscn scene:
///
///   CutsceneScene (Node2D)
///     Director (Node, CutsceneDirector.cs)
///       AnimationPlayer  — timeline whose METHOD tracks call Director methods
///
/// Method-call tracks drive: Say (dialog, pauses the timeline until the player
/// advances), FadeIn/FadeOut, SetFlag, LockPlayer/UnlockPlayer, PlacePlayer,
/// HidePlayer/ShowPlayer, ShowLocation. Because the timeline PAUSES during dialog,
/// timestamps stay deterministic and cancels are clean (free the scene / stop the
/// AnimationPlayer — no async step runner, no tween races).
///
/// Fired via <see cref="CutsceneTrigger.CutsceneScene"/>: the trigger instantiates
/// the scene under the current level and calls <see cref="Play"/>.
/// </summary>
[GlobalClass]
public partial class CutsceneDirector : Node
{
    private AnimationPlayer _anim;
    private bool _dialogPaused;
    private bool _claimedPlaying;

    public override void _Ready()
    {
        _anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (_anim == null && GetParent() != null)
            _anim = GetParent().GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
    }

    public override void _ExitTree()
    {
        // If the level unloaded mid-cutscene, never leave the CutsceneController
        // claiming a cutscene is playing (that would block all future cutscenes).
        ReleasePlaying();
    }

    /// <summary>Claims the CutsceneController playing state so systems gating on
    /// <see cref="CutsceneController.IsPlaying"/> (dev-save hotkeys, interaction
    /// prompts, cutscene double-fire protection) treat this cutscene like any other.</summary>
    private void ClaimPlaying()
    {
        if (_claimedPlaying || CutsceneController.Instance == null)
            return;
        _claimedPlaying = true;
        CutsceneController.Instance.ClaimExternalCutscene(true);
    }

    private void ReleasePlaying()
    {
        if (!_claimedPlaying || CutsceneController.Instance == null)
            return;
        _claimedPlaying = false;
        CutsceneController.Instance.ClaimExternalCutscene(false);
    }

    /// <summary>Plays the first animation on the AnimationPlayer and frees the scene when it ends.</summary>
    public async void Play()
    {
        if (_anim == null || _anim.GetAnimationList().Length == 0)
        {
            GD.PushError("CutsceneDirector: no animation to play.");
            QueueFree();
            return;
        }

        _anim.Play(_anim.GetAnimationList()[0]);
        ClaimPlaying();
        GameLogger.Info("Cutscene", $"CutsceneDirector: playing '{_anim.CurrentAnimation}'");
        await ToSignal(_anim, AnimationPlayer.SignalName.AnimationFinished);

        // The scene may have been freed with the level mid-cutscene (level unload);
        // never touch freed nodes from the continuation.
        if (!IsInsideTree())
            return;
        GameLogger.Info("Cutscene", "CutsceneDirector: finished — freeing scene");

        ReleasePlaying();
        UnlockPlayer();
        QueueFree();
    }

    /// <summary>Stops playback immediately (used if a level unloads mid-cutscene).</summary>
    public void Stop()
    {
        if (_anim != null && _anim.IsPlaying())
            _anim.Stop();
        QueueFree();
    }

    // -----------------------------------------------------------------------
    // Timeline-callable methods (MethodTrack targets)
    // -----------------------------------------------------------------------

    /// <summary>Shows one dialog line; pauses the timeline until the player advances.</summary>
    public void Say(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        GameLogger.Debug("Cutscene", $"Director.Say: {line}");
        DialogManager.Instance.StartDialog(new List<string> { line });
        if (_anim != null && _anim.IsPlaying())
        {
            _anim.Pause();
            _dialogPaused = true;
        }
        _ = ResumeAfterDialog();
    }

    private async Task ResumeAfterDialog()
    {
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);
        // Guard against the scene being freed mid-dialog (level unload) and against
        // double-resume if two Says raced before the pause took effect.
        if (!IsInsideTree() || !_dialogPaused || _anim == null)
            return;
        _dialogPaused = false;
        _anim.Play();
    }

    public void FadeIn()
    {
        GameLogger.Debug("Cutscene", "Director.FadeIn");
        FadeTransition.Instance.PlayFadeIn();
    }

    public void FadeOut()
    {
        GameLogger.Debug("Cutscene", "Director.FadeOut");
        FadeTransition.Instance.PlayFadeOut();
    }

    public void SetFlag(string key)
    {
        GameLogger.Debug("Cutscene", $"Director.SetFlag: {key}");
        WorldFlags.Instance.SetFlag(key, true);
    }

    public void LockPlayer()
    {
        GameLogger.Debug("Cutscene", "Director.LockPlayer");
        if (Player.Instance != null)
            Player.Instance.InInteraction = true;
    }

    public void UnlockPlayer()
    {
        GameLogger.Debug("Cutscene", "Director.UnlockPlayer");
        if (Player.Instance != null)
            Player.Instance.InInteraction = false;
    }

    public void PlacePlayer(Vector2 pos)
    {
        GameLogger.Debug("Cutscene", $"Director.PlacePlayer: {pos}");
        if (Player.Instance != null)
            Player.Instance.Position = pos;
    }

    public void HidePlayer()
    {
        GameLogger.Debug("Cutscene", "Director.HidePlayer");
        if (Player.Instance != null)
            Player.Instance.Visible = false;
    }

    public void ShowPlayer()
    {
        GameLogger.Debug("Cutscene", "Director.ShowPlayer");
        if (Player.Instance != null)
            Player.Instance.Visible = true;
    }

    public void ShowLocation(string name)
    {
        GameLogger.Debug("Cutscene", $"Director.ShowLocation: {name}");
        FadeTransition.Instance.ShowLocation(name);
    }
}
