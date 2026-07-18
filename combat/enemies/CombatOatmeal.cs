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

    private enum State
    {
        Idle,
        Telegraph,
        Attacking,
        Cooldown
    }

    private static readonly Dictionary<OatmealFlavor, int> FlavorMaxHP = new()
    {
        { OatmealFlavor.Vanilla, 30 },
        { OatmealFlavor.Strawberry, 25 },
        { OatmealFlavor.Chocolate, 40 },
        { OatmealFlavor.Mint, 20 },
    };

    private struct TimingProfile
    {
        public float IdleMin;
        public float IdleMax;
        public float Telegraph;
        public float Cooldown;
    }

    private static readonly Dictionary<OatmealFlavor, TimingProfile> FlavorProfile = new()
    {
        { OatmealFlavor.Vanilla,    new TimingProfile { IdleMin = 0.8f, IdleMax = 1.6f, Telegraph = 0.50f, Cooldown = 1.0f } },
        { OatmealFlavor.Strawberry, new TimingProfile { IdleMin = 0.7f, IdleMax = 1.3f, Telegraph = 0.45f, Cooldown = 0.9f } },
        { OatmealFlavor.Chocolate,  new TimingProfile { IdleMin = 1.0f, IdleMax = 2.0f, Telegraph = 0.60f, Cooldown = 1.2f } },
        { OatmealFlavor.Mint,       new TimingProfile { IdleMin = 0.6f, IdleMax = 1.0f, Telegraph = 0.35f, Cooldown = 1.4f } },
    };

    [Export] public OatmealFlavor Flavor { get; set; } = OatmealFlavor.Vanilla;

    private AnimationPlayer _animationPlayer;
    private Sprite2D _sprite;
    private Color _baseTint = new Color(1f, 1f, 1f);

    private State _state = State.Idle;
    private float _stateTimer = 0f;
    private float _stateDuration = 1f;

    public HealthComponent Health { get; private set; }

    private static readonly Color VanillaColor = new Color(1f, 0.95f, 0.8f);
    private static readonly Color StrawberryColor = new Color(1f, 0.6f, 0.7f);
    private static readonly Color ChocolateColor = new Color(0.5f, 0.3f, 0.15f);
    private static readonly Color MintColor = new Color(0.5f, 1f, 0.7f);

    private static readonly Color TelegraphColor = new Color(1f, 0.35f, 0.3f);


    private static PackedScene _cachedBulletScene;
    private static PackedScene BulletScene => _cachedBulletScene ??= ResourceLoader.Load<PackedScene>("res://combat/bullets/RedBullet.tscn");
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

        EnterState(State.Idle);
    }

    public void ApplyFlavor()
    {
        ApplyFlavorVisuals();
        if (Health != null && FlavorMaxHP.TryGetValue(Flavor, out int hp))
            Health.SetMaxHP(hp, true);
    }

    private void ApplyFlavorVisuals()
    {
        _baseTint = Flavor switch
        {
            OatmealFlavor.Strawberry => StrawberryColor,
            OatmealFlavor.Chocolate => ChocolateColor,
            OatmealFlavor.Mint => MintColor,
            _ => VanillaColor
        };

        if (_sprite != null)
            _sprite.Modulate = _baseTint;
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
        _stateTimer += (float)delta;

        switch (_state)
        {
            case State.Idle:
                if (_stateTimer >= _stateDuration)
                    EnterState(State.Telegraph);
                break;

            case State.Telegraph:
            {
                // Pulse toward red, intensifying as the wind-up completes.
                float t = Mathf.Clamp(_stateTimer / _stateDuration, 0f, 1f);
                float pulse = (Mathf.Sin(t * Mathf.Pi * 6f) * 0.5f + 0.5f) * t;
                if (_sprite != null)
                    _sprite.Modulate = _baseTint.Lerp(TelegraphColor, pulse);

                if (_stateTimer >= _stateDuration)
                    EnterState(State.Attacking);
                break;
            }

            case State.Attacking:
                Attack();
                if (_sprite != null)
                    _sprite.Modulate = _baseTint;
                EnterState(State.Cooldown);
                break;

            case State.Cooldown:
                if (_stateTimer >= _stateDuration)
                    EnterState(State.Idle);
                break;
        }
    }

    private void EnterState(State newState)
    {
        _state = newState;
        GameLogger.Debug("CombatOatmeal", $"State → {newState} (flavor: {Flavor})");
        _stateTimer = 0f;

        var profile = FlavorProfile.GetValueOrDefault(Flavor, FlavorProfile[OatmealFlavor.Vanilla]);

        switch (newState)
        {
            case State.Idle:
                _stateDuration = (float)GD.RandRange(profile.IdleMin, profile.IdleMax);
                if (_sprite != null)
                    _sprite.Modulate = _baseTint;
                break;
            case State.Telegraph:
                _stateDuration = profile.Telegraph;
                break;
            case State.Cooldown:
                _stateDuration = profile.Cooldown;
                break;
            // Attacking is instantaneous — no duration, transitions same frame.
        }
    }



    private void Attack()
    {
        _animationPlayer?.Play("default");

        string attackType;
        switch (Flavor)
        {
            case OatmealFlavor.Strawberry:
                attackType = "Homing (2x 180px/s)";
                AttackHoming();
                break;
            case OatmealFlavor.Chocolate:
                attackType = "Aimed (2x 350px/s)";
                AttackAimed();
                break;
            case OatmealFlavor.Mint:
                attackType = "Burst (5x 400px/s)";
                AttackBurst();
                break;
            default:
                attackType = "Spread (3x 250px/s, 30°)";
                AttackSpread();
                break;
        }
        GameLogger.Debug("CombatOatmeal", $"Attack: flavor={Flavor} — {attackType}");
    }

    private void AttackSpread()
    {
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
    }

    private void AttackHoming()
    {
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
    }

    private void AttackAimed()
    {
        for (int i = 0; i < 2; i++)
        {
            RedBullet bullet = SpawnBullet();
            Vector2 toPlayer = (CombatTargeter.GetPlayerPosition() - GlobalPosition).Normalized();
            float spreadOffset = (i == 0) ? -8f : 8f;
            float angle = Mathf.Atan2(toPlayer.Y, toPlayer.X) + Mathf.DegToRad(spreadOffset);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            bullet.SetDirection(dir, 350f);
        }
    }

    private void AttackBurst()
    {
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
    }

    private RedBullet SpawnBullet()
    {
        RedBullet bullet = BulletScene.Instantiate<RedBullet>();

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