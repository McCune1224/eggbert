using Godot;
using System.Collections.Generic;

public partial class CombatOatmeal : Area2D
{
    public enum OatmealFlavor
    {
        Vanilla,
        Strawberry,
        Chocolate,
        Mint
    }

    private static readonly Dictionary<OatmealFlavor, int> FlavorMaxHP = new()
    {
        { OatmealFlavor.Vanilla, 30 },
        { OatmealFlavor.Strawberry, 25 },
        { OatmealFlavor.Chocolate, 40 },
        { OatmealFlavor.Mint, 20 },
    };

    [Export] public OatmealFlavor Flavor { get; set; } = OatmealFlavor.Vanilla;

    private AnimationPlayer _animationPlayer;
    private Sprite2D _sprite;
    private bool _isFiring = false;
    private float _attackCooldown = 2.0f;
    private float _timeSinceLastAttack = 0.0f;

    public HealthComponent Health { get; private set; }

    private static readonly Color VanillaColor = new Color(1f, 0.95f, 0.8f);
    private static readonly Color StrawberryColor = new Color(1f, 0.6f, 0.7f);
    private static readonly Color ChocolateColor = new Color(0.5f, 0.3f, 0.15f);
    private static readonly Color MintColor = new Color(0.5f, 1f, 0.7f);

    public override void _Ready()
    {
        AddToGroup("enemy");
        CollisionLayer = CollisionConfig.EnemyLayer;
        CollisionMask = 0;

        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite2D");

        ApplyFlavorVisuals();

        Health = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (Health == null)
        {
            int hp = FlavorMaxHP.GetValueOrDefault(Flavor, 30);
            Health = new HealthComponent { Name = "HealthComponent", MaxHP = hp, CurrentHP = hp };
            AddChild(Health);
        }
        Health.Died += OnDied;
    }

    public void ApplyFlavor()
    {
        ApplyFlavorVisuals();
        if (Health != null && FlavorMaxHP.TryGetValue(Flavor, out int hp))
        {
            Health.SetMaxHP(hp, true);
        }
    }

    private void ApplyFlavorVisuals()
    {
        Color tint = Flavor switch
        {
            OatmealFlavor.Strawberry => StrawberryColor,
            OatmealFlavor.Chocolate => ChocolateColor,
            OatmealFlavor.Mint => MintColor,
            _ => VanillaColor
        };

        if (_sprite != null)
            _sprite.Modulate = tint;
    }

    private void OnDied()
    {
        Health.Died -= OnDied;
        var arena = GetParent() as CombatArena;
        if (arena != null)
            arena.OnEnemyDefeated();
        QueueFree();
    }

    public override void _Process(double delta)
    {
        _timeSinceLastAttack += (float)delta;

        if (!_isFiring && _timeSinceLastAttack >= _attackCooldown)
        {
            Attack();
            _timeSinceLastAttack = 0;
        }
    }

    public void Fire()
    {
        _animationPlayer.Play("default");
    }

    private void Attack()
    {
        switch (Flavor)
        {
            case OatmealFlavor.Strawberry:
                AttackHoming();
                break;
            case OatmealFlavor.Chocolate:
                AttackAimed();
                break;
            case OatmealFlavor.Mint:
                AttackBurst();
                break;
            default:
                AttackSpread();
                break;
        }
    }

    private void AttackSpread()
    {
        if (_isFiring) return;
        _animationPlayer.Play("default");
        _isFiring = true;

        int numBullets = 3;
        float spreadAngle = 30f;
        float angleStep = spreadAngle / (numBullets > 1 ? numBullets - 1 : 1);

        Vector2 targetPos = CombatTargeter.GetPlayerPosition();
        Vector2 directionToPlayer = (targetPos - GlobalPosition).Normalized();
        float baseAngle = Mathf.Atan2(directionToPlayer.Y, directionToPlayer.X);
        float startAngle = baseAngle - Mathf.DegToRad(spreadAngle / 2);

        for (int i = 0; i < numBullets; i++)
        {
            RedBullet bullet = SpawnBullet();
            float bulletAngle = startAngle + Mathf.DegToRad(angleStep * i);
            Vector2 dir = new Vector2(Mathf.Cos(bulletAngle), Mathf.Sin(bulletAngle));
            bullet.SetDirection(dir, 250f);
        }

        _isFiring = false;
    }

    private void AttackHoming()
    {
        if (_isFiring) return;
        _animationPlayer.Play("default");
        _isFiring = true;

        for (int i = 0; i < 2; i++)
        {
            RedBullet bullet = SpawnBullet();
            float offset = (i - 0.5f) * 15f;
            Vector2 toPlayer = (CombatTargeter.GetPlayerPosition() - GlobalPosition).Normalized();
            float angle = Mathf.Atan2(toPlayer.Y, toPlayer.X) + Mathf.DegToRad(offset);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            bullet.SetDirection(dir, 180f);
            bullet.IsHoming = true;
        }

        _isFiring = false;
    }

    private void AttackAimed()
    {
        if (_isFiring) return;
        _animationPlayer.Play("default");
        _isFiring = true;

        for (int i = 0; i < 2; i++)
        {
            RedBullet bullet = SpawnBullet();
            Vector2 toPlayer = (CombatTargeter.GetPlayerPosition() - GlobalPosition).Normalized();
            float spreadOffset = (i == 0) ? -8f : 8f;
            float angle = Mathf.Atan2(toPlayer.Y, toPlayer.X) + Mathf.DegToRad(spreadOffset);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            bullet.SetDirection(dir, 350f);
        }

        _isFiring = false;
    }

    private void AttackBurst()
    {
        if (_isFiring) return;
        _animationPlayer.Play("default");
        _isFiring = true;

        int numBullets = 5;
        float spreadAngle = 20f;
        float angleStep = spreadAngle / (numBullets > 1 ? numBullets - 1 : 1);

        Vector2 targetPos = CombatTargeter.GetPlayerPosition();
        Vector2 directionToPlayer = (targetPos - GlobalPosition).Normalized();
        float baseAngle = Mathf.Atan2(directionToPlayer.Y, directionToPlayer.X);
        float startAngle = baseAngle - Mathf.DegToRad(spreadAngle / 2);

        for (int i = 0; i < numBullets; i++)
        {
            RedBullet bullet = SpawnBullet();
            float bulletAngle = startAngle + Mathf.DegToRad(angleStep * i);
            Vector2 dir = new Vector2(Mathf.Cos(bulletAngle), Mathf.Sin(bulletAngle));
            bullet.SetDirection(dir, 400f);
        }

        _isFiring = false;
    }

    private RedBullet SpawnBullet()
    {
        RedBullet bullet = ResourceLoader.Load<PackedScene>("res://combat/bullets/RedBullet.tscn")
            .Instantiate<RedBullet>();

        if (bullet.GetParent() != null)
            bullet.GetParent().RemoveChild(bullet);

        bullet.CollisionMask = CollisionConfig.PlayerBulletMask;
        bullet.CollisionLayer = CollisionConfig.BulletLayer;
        bullet.GlobalPosition = GlobalPosition;
        bullet.FiredBy = this;
        bullet.ResetLifetime();

        GetParent().AddChild(bullet);
        return bullet;
    }
}
