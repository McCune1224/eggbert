using System;
using Godot;


public static class CollisionConfig
{
    // Layer constants (using bit flags: 1, 2, 4, 8, etc.)
    public const uint Player = 1;
    public const uint Walls = 2;
    public const uint NPCs = 4;
    public const uint Bullets = 8;
    public const uint Interactables = 16;
    public const uint Enemies = 32;
    public const uint TriggerAreas = 64;
    public const uint PlayerHitbox = 128;
    public const uint EnemyHitbox = 256;
    public const uint Items = 512;

    public const uint PlayerBulletMask = Player | PlayerHitbox | Walls;
    public const uint WallsMask = Player | NPCs | Bullets | Enemies;

    public static void PrintCollisionLayer(uint collisionLayer)
    {
        switch (collisionLayer)
        {
            case Player:
                GD.Log("Player");
                break;
            case Walls:
                GD.Print("Walls");
                break;
            case NPCs:
                GD.Print("NPCs");
                break;
            case Bullets:
                GD.Print("Bullets");
                break;
            case Interactables:
                GD.Print("Interactables");
                break;
            case Enemies:
                GD.Print("Enemies");
                break;
            case TriggerAreas:
                GD.Print("TriggerAreas");
                break;
            case PlayerHitbox:
                GD.Print("PlayerHitbox");
                break;
            case EnemyHitbox:
                GD.Print("EnemyHitbox");
                break;
            case Items:
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
