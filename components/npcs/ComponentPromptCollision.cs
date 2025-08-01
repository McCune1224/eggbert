using Godot.Collections;
using Godot;

public partial class ComponentPromptCollision : Area2D
{
    [Export]
    private string promptText = "?";

    private Label _interactionPrompt;
    private Sprite2D _promptSprite;
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {

        _interactionPrompt = GetNode<Label>("Label");
        _promptSprite = GetNode<Sprite2D>("Sprite2D");
        _promptSprite.Visible = false;

        Array<Node> children = GetChildren();
        foreach (Node child in children)
        {
            if (child is CollisionShape2D shape)
            {
                _collisionShape = shape;
                break;
            }
        }

        // WARNING:Assuming 'sprite' is a Sprite2D child of the NPC node, not
        // sure if there is a better way to get it
        Array<Node> siblings = GetParent().GetChildren();
        Sprite2D sprite = null;
        foreach (Node sibling in siblings)
        {
            if (sibling is Sprite2D s)
            {
                sprite = s;
                break;
            }
        }
        if (sprite == null)
        {
            GD.PrintErr("Sprite2D not found in siblings of ComponentPromptCollision in " + GetParent().Name);
            return;
        }


        AlignLabelText();

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }


    private void OnBodyEntered(Node2D body)
    {
        // Check if the body that entered is the player
        if (body.IsInGroup("player")) // Adjust this to match your player node name
        {
            //calculate the position of the NPC sprite, and position the prompt sprite to be above the NPC
            Array<Node> siblings = GetParent().GetChildren();
            Sprite2D npcSprite = null;
            foreach (Node sibling in siblings)
            {
                if (sibling is Sprite2D s)
                {
                    npcSprite = s;
                    break;
                }
            }
            if (npcSprite == null)
            {
                GD.PrintErr("Sprite2D not found in siblings of ComponentPromptCollision in " + GetParent().Name);
                return;
            }
            // Vector2 npcSpriteGlobalPosition = npcSprite.GlobalPosition;
            // Vector2 spriteDimensions = npcSprite.Texture.GetSize() * npcSprite.Scale;
            // // Position the interaction prompt above the sprite
            // _promptSprite.GlobalPosition = new Vector2(
            //     npcSpriteGlobalPosition.X,
            //     npcSpriteGlobalPosition.Y - 5
            // );
            _promptSprite.Position = new Vector2(
                0,
                -(npcSprite.Texture.GetSize().Y / 2) / 4 - 20
            );

            // Show the interaction prompt
            GD.Print("Player hit");
            _interactionPrompt.Visible = true;
            _promptSprite.Visible = true;
        }
    }

    private void OnBodyExited(Node2D body)
    {
        // Check if the body that left is the player
        if (body.IsInGroup("player")) // Adjust this to match your player node name
        {
            // Hide the interaction prompt
            _interactionPrompt.Visible = false;
            _promptSprite.Visible = false;
            DialogManager.Instance.Reset();
        }
    }

    private void AlignLabelText()
    {
        Array<Node> siblings = GetParent().GetChildren();
        Sprite2D sprite = null;
        foreach (Node sibling in siblings)
        {
            if (sibling is Sprite2D s)
            {
                sprite = s;
                break;
            }
        }

        // Vector2 spriteDimensions = sprite.Texture.GetSize() * sprite.Scale;
        Vector2 spriteDimensions = Vector2.Zero;
        GD.Print(sprite.GetParent().Name, " Sprite Texture Dimensions: ", spriteDimensions);

        _interactionPrompt.Position = new Vector2(
             -spriteDimensions.X / 2,
             0
        );


        // Determine alignment based on promptPosition for Godot 4.3
        // Assumes this script is on a Label or derived Control node

        // if (Mathf.Abs(Position.Y) >= Mathf.Abs(Position.X))
        // {
        //     if (Position.Y < 0)
        //     {
        //         // Above NPC
        //         _interactionPrompt.HorizontalAlignment = HorizontalAlignment.Center;
        //         _interactionPrompt.VerticalAlignment = VerticalAlignment.Top;
        //     }
        //     else
        //     {
        //         // Below NPC
        //         _interactionPrompt.HorizontalAlignment = HorizontalAlignment.Center;
        //         _interactionPrompt.VerticalAlignment = VerticalAlignment.Bottom;
        //     }
        // }
        // else
        // {
        //     if (Position.X < 0)
        //     {
        //         // Left of NPC
        //         _interactionPrompt.HorizontalAlignment = HorizontalAlignment.Left;
        //         _interactionPrompt.VerticalAlignment = VerticalAlignment.Center;
        //     }
        //     else
        //     {
        //         // Right of NPC
        //         _interactionPrompt.HorizontalAlignment = HorizontalAlignment.Right;
        //         _interactionPrompt.VerticalAlignment = VerticalAlignment.Center;
        //     }
        // }
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
        AlignLabelText();
    }
}
