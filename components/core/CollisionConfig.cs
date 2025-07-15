using System;
using Godot;


public static class CollisionConfig
{
    // Layer constants (using bit flags: 1, 2, 4, 8, etc.)
    public const uint PlayerLayer = 1;
    public const uint WallsLayer = 2;
    public const uint NPCLayer = 4;
    public const uint BulletLayer = 8;
    public const uint InteractableLayer = 16;
    public const uint EnemyLayer = 32;
    public const uint TriggerAreaLayer = 64;
    public const uint EnemyHitboxLayer = 256;
    public const uint ItemLayer = 512;

    public const uint PlayerBulletMask = PlayerLayer | WallsLayer;
    public const uint WallsMask = PlayerLayer | NPCLayer | BulletLayer | EnemyLayer;
    public const uint ItemMask = PlayerLayer | NPCLayer | BulletLayer | EnemyLayer;

    public static void PrintCollisionLayer(uint collisionLayer)
    {
        switch (collisionLayer)
        {
            case PlayerLayer:
                GD.Print("Player");
                break;
            case WallsLayer:
                GD.Print("Walls");
                break;
            case NPCLayer:
                GD.Print("NPCs");
                break;
            case BulletLayer:
                GD.Print("Bullets");
                break;
            case InteractableLayer:
                GD.Print("Interactables");
                break;
            case EnemyLayer:
                GD.Print("Enemies");
                break;
            case TriggerAreaLayer:
                GD.Print("TriggerAreas");
                break;
            case EnemyHitboxLayer:
                GD.Print("EnemyHitbox");
                break;
            case ItemLayer:
                GD.Print("Items");
                break;
            default:
                GD.Print("Unknown collision layer", collisionLayer);
                break;
        }
    }
    public static void PrintCollisionMask(uint collision)
    {
        switch (collision)
        {
            case PlayerBulletMask:
                GD.Print("PlayerBulletMask");
                break;
            case WallsMask:
                GD.Print("WallsMask");
                break;
            default:
                GD.Print("Unknown collision mask ", collision);
                break;
        }
    }
}
