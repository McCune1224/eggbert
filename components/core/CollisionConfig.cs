using Godot;

public static class CollisionConfig
{
    public const uint PlayerLayer = 1;
    public const uint WallsLayer = 2;
    public const uint NPCLayer = 4;
    public const uint BulletLayer = 8;
    public const uint InteractableLayer = 16;
    public const uint EnemyLayer = 32;
    public const uint TriggerAreaLayer = 64;
    public const uint PlayerHitboxLayer = 128;
    public const uint EnemyHitboxLayer = 256;
    public const uint ItemLayer = 512;

    public const uint PlayerBulletMask = PlayerLayer | WallsLayer;
    public const uint WallsMask = PlayerLayer | NPCLayer | BulletLayer | EnemyLayer | InteractableLayer;
    public const uint ItemMask = PlayerLayer | NPCLayer | BulletLayer | EnemyLayer;
}
