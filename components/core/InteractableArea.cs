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

    protected Sprite2D PromptSprite { get; private set; }
    protected bool PlayerInRange { get; set; } = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        PromptSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (PromptSprite != null)
        {
            PromptSprite.Visible = false;
            if (!Settings.ShowInteractionPrompt)
            {
                PromptSprite.QueueFree();
                PromptSprite = null;
            }
        }

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (!PlayerInRange) return;
        if (!@event.IsActionPressed("interact")) return;

        OnInteract();
        GetViewport().SetInputAsHandled();
    }

    protected virtual void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        PlayerInRange = true;

        if (PromptSprite != null && GodotObject.IsInstanceValid(PromptSprite))
            PromptSprite.Visible = true;
    }

    protected virtual void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        PlayerInRange = false;

        if (PromptSprite != null && GodotObject.IsInstanceValid(PromptSprite))
            PromptSprite.Visible = false;

        if (!CutsceneController.Instance.IsPlaying)
            DialogManager.Instance.Reset();
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
