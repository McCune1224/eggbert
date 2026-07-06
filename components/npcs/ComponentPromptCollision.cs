using Godot;

public partial class ComponentPromptCollision : Area2D
{
    [Export]
    private string promptText = "?";

    private Label _interactionPrompt;
    private Sprite2D _promptSprite;
    private Sprite2D _npcSprite;
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {
        _interactionPrompt = GetNode<Label>("Label");
        _promptSprite = GetNode<Sprite2D>("Sprite2D");
        _promptSprite.Visible = false;
        _promptSprite.GlobalScale = Vector2.One;

        foreach (Node child in GetChildren())
        {
            if (child is CollisionShape2D shape)
            {
                _collisionShape = shape;
                break;
            }
        }

        // Cache the parent NPC's Sprite2D (used for prompt positioning)
        foreach (Node sibling in GetParent().GetChildren())
        {
            if (sibling is Sprite2D s)
            {
                _npcSprite = s;
                break;
            }
        }

        if (_npcSprite == null)
        {
            GD.PrintErr($"No Sprite2D sibling found in parent of ComponentPromptCollision ({GetParent().Name})");
            return;
        }

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        // Position prompt above the NPC sprite
        float spriteHalfHeight = _npcSprite.GetRect().Size.Y / 2f;
        _promptSprite.Position = new Vector2(0, -spriteHalfHeight);

        _interactionPrompt.Visible = true;
        _promptSprite.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player"))
            return;

        _interactionPrompt.Visible = false;
        _promptSprite.Visible = false;
        DialogManager.Instance.Reset();
    }

    public bool isPromptVisible()
    {
        return _interactionPrompt.Visible;
    }

    public void HidePrompt()
    {
        _interactionPrompt.Visible = false;
    }

    public void ShowPrompt()
    {
        _interactionPrompt.Visible = true;
        _interactionPrompt.Text = promptText;
    }
}
