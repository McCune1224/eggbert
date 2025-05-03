using Godot;
using System;

public partial class OverworldItem : Area2D
{
    CollisionShape2D _collision;
    Sprite2D _sprite;

    // Optional export variables to fine-tune the collision
    [Export] private bool AutoSizeCollision = true;
    [Export] private float CollisionPadding = 0f; // Extra padding around the sprite
    [Export] private bool UseCircleShape = false; // Whether to use a circle or rectangle

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.ItemLayer;
        CollisionMask = CollisionConfig.ItemMask;

        // Get the collision shape
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");

        // Get the sprite
        _sprite = GetNode<Sprite2D>("Sprite2D");

        if (AutoSizeCollision && _sprite != null && _collision != null)
        {
            AdjustCollisionShape();
        }
    }

    /// <summary>
    /// Adjusts the collision shape to match the sprite's dimensions
    /// </summary>
    public void AdjustCollisionShape()
    {
        if (_sprite == null || _collision == null)
            return;

        // Wait until the texture is ready
        if (_sprite.Texture == null)
        {
            CallDeferred(nameof(AdjustCollisionShape));
            return;
        }

        // Get the sprite dimensions
        Vector2 spriteSize = _sprite.GetRect().Size;

        // Account for sprite scale
        spriteSize *= _sprite.Scale;

        if (UseCircleShape)
        {
            // Create a circular shape if not already present
            if (_collision.Shape == null || !(_collision.Shape is CircleShape2D))
            {
                _collision.Shape = new CircleShape2D();
            }

            // Set the radius to half the max dimension plus padding
            float radius = Mathf.Max(spriteSize.X, spriteSize.Y) / 2f + CollisionPadding;
            ((CircleShape2D)_collision.Shape).Radius = radius;
        }
        else
        {
            // Create a rectangular shape if not already present
            if (_collision.Shape == null || !(_collision.Shape is RectangleShape2D))
            {
                _collision.Shape = new RectangleShape2D();
            }

            // Set the size to the sprite size plus padding
            ((RectangleShape2D)_collision.Shape).Size = spriteSize + new Vector2(CollisionPadding * 2, CollisionPadding * 2);
        }
    }

    /// <summary>
    /// Call this method after setting a new sprite texture to update the collision shape
    /// </summary>
    public void UpdateSpriteAndCollision(Texture2D newTexture)
    {
        if (_sprite != null)
        {
            _sprite.Texture = newTexture;
            if (AutoSizeCollision)
            {
                AdjustCollisionShape();
            }
        }
    }
}
