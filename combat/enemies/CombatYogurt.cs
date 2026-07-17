using Godot;
using System.Collections.Generic;

/// <summary>
/// Yogurt mini-boss for the Warden's Quarters (story beat 5).
/// Unique "spiral splash" bullet pattern: fires bullets in a rotating spiral
/// that sweeps around the arena, forcing the player to find gaps in the spiral.
/// </summary>
public partial class CombatYogurt : Area2D
{
    private enum State { Idle, Telegraph, Attack, Cooldown }

    [Export] public int MaxHP { get; set; } = 60;
    [Export] public int ContactDamage { get; set; } = 8;

    private State _state = State.Idle;
    private float _stateTimer = 0f;
    private float _stateDuration = 1.5f;

    private float _spiralAngle = 0f;
    private int _spiralShotsFired = 0;
    private const int SpiralShotsPerVolley = 12;
    private const float SpiralAngleStep = 24f; // degrees between each spiral bullet
    private const float SpiralVolleyDelay = 0.08f; // seconds between spiral shots
    private float _spiralShotTimer = 0f;
    private const float BulletSpeed = 180f;

    public HealthComponent Health { get; private set; }

    private Sprite2D _sprite;
    private AnimationPlayer _animationPlayer;
    private static readonly Color BaseColor = new Color(0.95f, 0.95f, 0.82f);
    private static readonly Color TelegraphColor = new Color(1f, 0.4f, 0.5f);

    private static PackedScene _cachedBulletScene;
    private static PackedScene BulletScene => _cachedBulletScene ??=
        ResourceLoader.Load<PackedScene>("res://combat/bullets/RedBullet.tscn");

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.EnemyLayer;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.BulletLayer;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
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

        GameLogger.Info("Combat", $"CombatYogurt '{Name}': ready — HP={Health.CurrentHP}/{Health.MaxHP}");
    }

    private void OnDied()
    {
        Health.Died -= OnDied;
        if (GetParent() is CombatArena arena)
            arena.OnEnemyDefeated();
        GameLogger.Info("Combat", $"CombatYogurt '{Name}': defeated");
        QueueFree();
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is RedBullet bullet && bullet.Reflected)
        {
            Health.TakeDamage(10 + Equipment.Instance.TotalAttackBoost, bullet);
            GameLogger.Debug("Combat", $"CombatYogurt '{Name}': hit by reflected bullet for {10 + Equipment.Instance.TotalAttackBoost} DMG");
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player") && _state != State.Cooldown)
        {
            Player.Instance.HealthComponent?.TakeDamage(ContactDamage, this);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _stateTimer += dt;

        switch (_state)
        {
            case State.Idle:
                if (_stateTimer >= _stateDuration)
                    EnterState(State.Telegraph);
                break;
            case State.Telegraph:
                if (_sprite != null) _sprite.Modulate = TelegraphColor;
                if (_stateTimer >= _stateDuration)
                    EnterState(State.Attack);
                break;
            case State.Attack:
                if (_sprite != null) _sprite.Modulate = BaseColor;
                _spiralShotTimer += dt;
                if (_spiralShotTimer >= SpiralVolleyDelay && _spiralShotsFired < SpiralShotsPerVolley)
                {
                    FireSpiralBullet();
                    _spiralShotsFired++;
                    _spiralShotTimer = 0f;
                }
                if (_spiralShotsFired >= SpiralShotsPerVolley)
                    EnterState(State.Cooldown);
                else if (_stateTimer >= _stateDuration * 3f) // safety timeout
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
        _stateTimer = 0f;
        _spiralShotsFired = 0;
        _spiralShotTimer = 0f;

        switch (newState)
        {
            case State.Idle:
                _stateDuration = 1.2f;
                if (_sprite != null) _sprite.Modulate = BaseColor;
                break;
            case State.Telegraph:
                _stateDuration = 0.8f;
                GameLogger.Debug("Combat", $"CombatYogurt '{Name}': telegraphing spiral attack");
                break;
            case State.Attack:
                _stateDuration = 2.0f;
                GameLogger.Debug("Combat", $"CombatYogurt '{Name}': firing spiral volley");
                break;
            case State.Cooldown:
                _stateDuration = 1.8f;
                if (_sprite != null) _sprite.Modulate = BaseColor;
                break;
        }
    }

    private void FireSpiralBullet()
    {
        RedBullet bullet = BulletScene.Instantiate<RedBullet>();
        if (bullet.GetParent() != null) bullet.GetParent().RemoveChild(bullet);

        bullet.CollisionMask = CollisionConfig.PlayerBulletMask;
        bullet.CollisionLayer = CollisionConfig.BulletLayer;
        bullet.GlobalPosition = GlobalPosition;
        bullet.FiredBy = this;
        bullet.ResetLifetime();

        float angle = Mathf.DegToRad(_spiralAngle);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        bullet.SetDirection(dir, BulletSpeed);

        GetParent().AddChild(bullet);

        _spiralAngle = (_spiralAngle + SpiralAngleStep) % 360f;
    }
}