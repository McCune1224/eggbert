using Godot;
using System.Collections.Generic;

/// <summary>
/// Sunnyside Leader final boss (Beach finale, story beat 10).
/// Multi-phase bullet patterns that escalate as HP drops:
///   Phase 1 (>66% HP): aimed spread (3 bullets, slow)
///   Phase 2 (33-66% HP): ring burst (12 bullets)
///   Phase 3 (<33% HP): spiral (rotating shots) + aimed spread combined
/// </summary>
public partial class CombatSunnysideLeader : Area2D
{
    private enum State { Idle, Telegraph, Attack, Cooldown }

    [Export] public int MaxHP { get; set; } = 120;
    [Export] public int ContactDamage { get; set; } = 10;

    private State _state = State.Idle;
    private float _stateTimer = 0f;
    private float _stateDuration = 1.5f;

    private int _phase = 1;
    private float _spiralAngle = 0f;
    private int _spiralShotsThisVolley = 0;
    private const int Phase3SpiralShots = 16;
    private const float SpiralStep = 22.5f;
    private float _spiralShotTimer = 0f;
    private const float SpiralDelay = 0.07f;

    public HealthComponent Health { get; private set; }

    private Sprite2D _sprite;
    private static readonly Color Phase1Color = new Color(1f, 0.95f, 0.7f);
    private static readonly Color Phase2Color = new Color(1f, 0.7f, 0.5f);
    private static readonly Color Phase3Color = new Color(1f, 0.4f, 0.4f);
    private static readonly Color TelegraphColor = new Color(1f, 0.25f, 0.3f);

    private static PackedScene _cachedBulletScene;
    private static PackedScene BulletScene => _cachedBulletScene ??=
        ResourceLoader.Load<PackedScene>("res://combat/bullets/RedBullet.tscn");

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.EnemyLayer;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.BulletLayer;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null) _sprite.Modulate = Phase1Color;

        Health = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (Health == null)
        {
            Health = new HealthComponent { Name = "HealthComponent", MaxHP = MaxHP, CurrentHP = MaxHP };
            AddChild(Health);
        }
        Health.Died += OnDied;
        Health.Damaged += OnDamaged;

        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        GameLogger.Info("Combat", $"CombatSunnysideLeader '{Name}': ready — HP={Health.CurrentHP}/{Health.MaxHP}");
    }

    private void OnDied()
    {
        Health.Died -= OnDied;
        if (GetParent() is CombatArena arena)
            arena.OnEnemyDefeated();
        GameLogger.Info("Combat", $"CombatSunnysideLeader '{Name}': defeated");
        QueueFree();
    }

    private void OnDamaged(int amount, Node source)
    {
        int phaseBefore = _phase;
        float hpFrac = (float)Health.CurrentHP / Health.MaxHP;
        if (hpFrac <= 0.33f) _phase = 3;
        else if (hpFrac <= 0.66f) _phase = 2;
        else _phase = 1;

        if (_phase != phaseBefore)
        {
            GameLogger.Info("Combat", $"CombatSunnysideLeader '{Name}': phase {phaseBefore} → {_phase} (HP={Health.CurrentHP}/{Health.MaxHP})");
            if (_sprite != null)
            {
                _sprite.Modulate = _phase switch
                {
                    2 => Phase2Color,
                    3 => Phase3Color,
                    _ => Phase1Color
                };
            }
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is RedBullet bullet && bullet.Reflected)
        {
            Health.TakeDamage(10 + Equipment.Instance.TotalAttackBoost, bullet);
            GameLogger.Debug("Combat", $"CombatSunnysideLeader '{Name}': hit by reflected bullet for {10 + Equipment.Instance.TotalAttackBoost} DMG");
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
                if (_sprite != null) _sprite.Modulate = _phase switch { 2 => Phase2Color, 3 => Phase3Color, _ => Phase1Color };
                ExecuteAttack(dt);
                if (_stateTimer >= _stateDuration * 2.5f)
                    EnterState(State.Cooldown);
                break;
            case State.Cooldown:
                if (_stateTimer >= _stateDuration) EnterState(State.Idle);
                break;
        }
    }

    private void ExecuteAttack(float dt)
    {
        switch (_phase)
        {
            case 1:
                AttackAimedSpread();
                EnterState(State.Cooldown);
                break;
            case 2:
                AttackRing();
                EnterState(State.Cooldown);
                break;
            case 3:
                // Spiral volley — keep firing until enough shots launched, then cooldown
                _spiralShotTimer += dt;
                if (_spiralShotTimer >= SpiralDelay && _spiralShotsThisVolley < Phase3SpiralShots)
                {
                    FireSpiralBullet();
                    _spiralShotsThisVolley++;
                    _spiralShotTimer = 0f;
                }
                if (_spiralShotsThisVolley >= Phase3SpiralShots)
                {
                    // Follow with a quick aimed spread to mix it up
                    AttackAimedSpread();
                    EnterState(State.Cooldown);
                }
                break;
        }
    }

    private void EnterState(State newState)
    {
        _state = newState;
        _stateTimer = 0f;
        _spiralShotsThisVolley = 0;
        _spiralShotTimer = 0f;

        switch (newState)
        {
            case State.Idle:
                _stateDuration = _phase == 3 ? 0.6f : 1.0f;
                break;
            case State.Telegraph:
                _stateDuration = _phase == 3 ? 0.5f : 0.7f;
                GameLogger.Debug("Combat", $"CombatSunnysideLeader '{Name}': telegraphing (phase {_phase})");
                break;
            case State.Attack:
                _stateDuration = 1.8f;
                GameLogger.Debug("Combat", $"CombatSunnysideLeader '{Name}': attacking (phase {_phase})");
                break;
            case State.Cooldown:
                _stateDuration = _phase == 3 ? 1.0f : 1.5f;
                break;
        }
    }

    private void AttackAimedSpread()
    {
        int numBullets = 3;
        float spreadAngle = 25f;
        float angleStep = spreadAngle / (numBullets > 1 ? numBullets - 1 : 1);
        Vector2 toPlayer = (CombatTargeter.GetPlayerPosition() - GlobalPosition).Normalized();
        float baseAngle = Mathf.Atan2(toPlayer.Y, toPlayer.X);
        float startAngle = baseAngle - Mathf.DegToRad(spreadAngle / 2);

        for (int i = 0; i < numBullets; i++)
        {
            float angle = startAngle + Mathf.DegToRad(angleStep * i);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnBullet(dir, 220f);
        }
    }

    private void AttackRing()
    {
        int n = 12;
        float angleStep = 360f / n;
        for (int i = 0; i < n; i++)
        {
            float angle = Mathf.DegToRad(angleStep * i);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnBullet(dir, 170f);
        }
    }

    private void FireSpiralBullet()
    {
        float angle = Mathf.DegToRad(_spiralAngle);
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        SpawnBullet(dir, 200f);
        _spiralAngle = (_spiralAngle + SpiralStep) % 360f;
    }

    private void SpawnBullet(Vector2 dir, float speed)
    {
        RedBullet bullet = BulletScene.Instantiate<RedBullet>();
        if (bullet.GetParent() != null) bullet.GetParent().RemoveChild(bullet);

        bullet.CollisionMask = CollisionConfig.PlayerBulletMask;
        bullet.CollisionLayer = CollisionConfig.BulletLayer;
        bullet.GlobalPosition = GlobalPosition;
        bullet.FiredBy = this;
        bullet.ResetLifetime();
        bullet.SetDirection(dir, speed);

        GetParent().AddChild(bullet);
    }
}