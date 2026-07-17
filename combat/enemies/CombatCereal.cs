using Godot;
using System.Collections.Generic;

/// <summary>
/// Cereal mini-boss for the Secret Tunnels (story beat 7).
/// Unique "shrapnel" bullet pattern: fires two staggered ring bursts that
/// expand outward in all directions — the player must find gaps in the rings
/// and weave between them.
/// </summary>
public partial class CombatCereal : Area2D
{
    private enum State { Idle, Telegraph, Attack, Cooldown }

    [Export] public int MaxHP { get; set; } = 70;
    [Export] public int ContactDamage { get; set; } = 8;
    /// <summary>Number of bullets per ring burst. Higher = denser rings.</summary>
    [Export] public int BulletsPerRing { get; set; } = 14;
    [Export] public float BulletSpeed { get; set; } = 160f;

    private State _state = State.Idle;
    private float _stateTimer = 0f;
    private float _stateDuration = 1.5f;
    private int _ringsFiredThisAttack = 0;
    private const int RingsPerVolley = 2;
    private float _ringDelayTimer = 0f;
    private const float RingDelay = 0.45f;
    private float _ringAngleOffset = 0f;

    public HealthComponent Health { get; private set; }

    private Sprite2D _sprite;
    private static readonly Color BaseColor = new Color(0.95f, 0.7f, 0.4f);
    private static readonly Color TelegraphColor = new Color(1f, 0.4f, 0.3f);

    private static PackedScene _cachedBulletScene;
    private static PackedScene BulletScene => _cachedBulletScene ??=
        ResourceLoader.Load<PackedScene>("res://combat/bullets/RedBullet.tscn");

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.EnemyLayer;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.BulletLayer;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null) _sprite.Modulate = BaseColor;

        Health = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (Health == null)
        {
            Health = new HealthComponent { Name = "HealthComponent", MaxHP = MaxHP, CurrentHP = MaxHP };
            AddChild(Health);
        }
        Health.Died += OnDied;

        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        GameLogger.Info("Combat", $"CombatCereal '{Name}': ready — HP={Health.CurrentHP}/{Health.MaxHP}");
    }

    private void OnDied()
    {
        Health.Died -= OnDied;
        if (GetParent() is CombatArena arena)
            arena.OnEnemyDefeated();
        GameLogger.Info("Combat", $"CombatCereal '{Name}': defeated");
        QueueFree();
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is RedBullet bullet && bullet.Reflected)
        {
            Health.TakeDamage(10 + Equipment.Instance.TotalAttackBoost, bullet);
            GameLogger.Debug("Combat", $"CombatCereal '{Name}': hit by reflected bullet for {10 + Equipment.Instance.TotalAttackBoost} DMG");
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player") && _state != State.Cooldown)
            Player.Instance.HealthComponent?.TakeDamage(ContactDamage, this);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _stateTimer += dt;

        switch (_state)
        {
            case State.Idle:
                if (_stateTimer >= _stateDuration) EnterState(State.Telegraph);
                break;
            case State.Telegraph:
                if (_sprite != null) _sprite.Modulate = TelegraphColor;
                if (_stateTimer >= _stateDuration) EnterState(State.Attack);
                break;
            case State.Attack:
                if (_sprite != null) _sprite.Modulate = BaseColor;
                if (_ringsFiredThisAttack == 0)
                {
                    FireRing();
                    _ringsFiredThisAttack++;
                    _ringDelayTimer = 0f;
                }
                else if (_ringsFiredThisAttack < RingsPerVolley)
                {
                    _ringDelayTimer += dt;
                    if (_ringDelayTimer >= RingDelay)
                    {
                        FireRing();
                        _ringsFiredThisAttack++;
                        _ringDelayTimer = 0f;
                    }
                }
                else
                {
                    EnterState(State.Cooldown);
                }
                // safety timeout
                if (_stateTimer >= _stateDuration * 3f)
                    EnterState(State.Cooldown);
                break;
            case State.Cooldown:
                if (_stateTimer >= _stateDuration) EnterState(State.Idle);
                break;
        }
    }

    private void EnterState(State newState)
    {
        _state = newState;
        _stateTimer = 0f;
        _ringsFiredThisAttack = 0;
        _ringDelayTimer = 0f;

        switch (newState)
        {
            case State.Idle:
                _stateDuration = 1.0f;
                if (_sprite != null) _sprite.Modulate = BaseColor;
                break;
            case State.Telegraph:
                _stateDuration = 0.7f;
                GameLogger.Debug("Combat", $"CombatCereal '{Name}': telegraphing shrapnel burst");
                break;
            case State.Attack:
                _stateDuration = 2.0f;
                GameLogger.Debug("Combat", $"CombatCereal '{Name}': firing {RingsPerVolley} shrapnel rings");
                break;
            case State.Cooldown:
                _stateDuration = 1.6f;
                if (_sprite != null) _sprite.Modulate = BaseColor;
                break;
        }
    }

    private void FireRing()
    {
        int n = BulletsPerRing;
        float angleStep = 360f / n;
        float startAngle = _ringAngleOffset;
        _ringAngleOffset = (_ringAngleOffset + angleStep / 2f) % 360f; // offset next ring to fill gaps

        for (int i = 0; i < n; i++)
        {
            float angle = Mathf.DegToRad(startAngle + angleStep * i);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            RedBullet bullet = BulletScene.Instantiate<RedBullet>();
            if (bullet.GetParent() != null) bullet.GetParent().RemoveChild(bullet);

            bullet.CollisionMask = CollisionConfig.PlayerBulletMask;
            bullet.CollisionLayer = CollisionConfig.BulletLayer;
            bullet.GlobalPosition = GlobalPosition;
            bullet.FiredBy = this;
            bullet.ResetLifetime();
            bullet.SetDirection(dir, BulletSpeed);

            GetParent().AddChild(bullet);
        }
    }
}