using Godot;

/// <summary>
/// Base class for interactable areas (signs, phones, sleeping NPCs, etc.)
/// Handles the common Area2D pattern:
/// - Player detection via BodyEntered/BodyExited
/// - Prompt sprite show/hide
/// - Interact action dispatch
///
/// Subclasses override OnInteract() for custom behavior.
/// </summary>
public abstract partial class InteractableArea : Area2D
{
    [Export] public DialogVoiceResource Voice { get; set; }

    protected bool PlayerInRange { get; set; } = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (!PlayerInRange) return;
        if (!@event.IsActionPressed("interact")) return;

        GameLogger.Debug("InteractableArea", $"'{Name}': interact triggered (type={GetType().Name})");
        OnInteract();
        GetViewport().SetInputAsHandled();
    }

    protected virtual void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        PlayerInRange = true;
        Player.Instance?.InteractionPrompt?.SetInteractableAvailable(this, true);

        GameLogger.Debug("InteractableArea", $"'{Name}': player entered range");
    }

    protected virtual void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        PlayerInRange = false;
        Player.Instance?.InteractionPrompt?.SetInteractableAvailable(this, false);

        if (!CutsceneController.Instance.IsPlaying)
            DialogManager.Instance.Reset();

        GameLogger.Debug("InteractableArea", $"'{Name}': player exited range");
    }

    /// <summary>
    /// Override to define what happens when the player interacts.
    /// </summary>
    protected abstract void OnInteract();

    /// <summary>
    /// Shows dialog lines via CutsceneController.
    /// </summary>
    protected void ShowDialog(string[] lines, DialogVoiceResource voice = null)
    {
        if (lines == null || lines.Length == 0) return;
        CutsceneController.Instance.StartDialog(lines, voice ?? Voice);
    }
}
