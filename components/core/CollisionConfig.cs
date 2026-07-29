/// <summary>
/// Physics collision layer and mask constants used throughout the project.
/// </summary>
/// <remarks>
/// Layer numbers correspond to Godot's 20 collision layers (bits 0–19). Use bit-shift
/// constants (e.g. <see cref="PlayerLayer"/> = 1 &lt;&lt; 0) when configuring
/// <see cref="CollisionObject2D.CollisionLayer"/> or <see cref="CollisionObject2D.CollisionMask"/>.
///
/// | Layer # | Constant            | Bit   | Typical Use                          |
/// |---------|---------------------|-------|--------------------------------------|
/// | 1       | <see cref="PlayerLayer"/>        | 0     | Player body                          |
/// | 2       | <see cref="WallsLayer"/>        | 1     | Static world geometry                |
/// | 3       | NPCs                | 2     | NPC bodies                           |
/// | 4       | <see cref="BulletLayer"/>       | 3     | Player and enemy bullets             |
/// | 5       | Interactables       | 4     | Level-transition zones, switches     |
/// | 6       | <see cref="EnemyLayer"/>       | 5     | Enemy bodies                         |
/// | 7       | <see cref="TriggerAreaLayer"/>  | 6     | Trigger areas (doors, puzzles)       |
/// | 8       | <see cref="PlayerHitboxLayer"/> | 7     | Player hurtbox / attack hitbox       |
/// | 9       | <see cref="EnemyHitboxLayer"/>  | 8     | Enemy hurtbox / attack hitbox        |
/// | 10      | <see cref="ItemLayer"/>         | 9     | Pickups, items                       |
/// </remarks>


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
}
