using Godot;
using System.Collections.Generic;

/// <summary>
/// Crackpot — a bouncing pot enemy for the Kitchen area.
/// Leaps to random positions, leaves damaging puddles.
/// Parry: bounces it away and cleanses all puddles.
/// </summary>
public partial class Crackpot : Area2D
{
    private enum State { Idle, Telegraph, Leaping, PuddleActive, Cooldown }

    [Export] public int MaxHP = 60;
    [Export] public float LeapSpeed = 300f;
    [Export] public int ContactDamage = 8;
    [Export] public int PuddleDamage = 5;
    [Export] public float PuddleLifetime = 4f;
    [Export] public float TeleDuration = 0.8f;
    [Export] public float CooldownDuration = 1.2f;

    public HealthComponent Health { get; private set; }

    private State _state = State.Idle;
    private float _stateTimer = 0f;
    private float _stateDuration = 1f;
    private Vector2 _targetPosition;
    private Vector2 _startPosition;
    private float _leapProgress = 0f;
    private bool _leapReturning = false; // leap goes up then down

    private Sprite2D _sprite;
    private Color _baseTint = new Color(0.6f, 0.3f, 0.1f);
    private static readonly Color TelegraphColor = new Color(1f, 0.5f, 0f);
    private static readonly Color StunColor = new Color(0.5f, 0.5f, 1f);

    private static PackedScene _cachedPuddleScene;
    private static PackedScene PuddleScene =>
        _cachedPuddleScene ??= ResourceLoader.Load<PackedScene>("res://combat/enemies/CrackpotPuddle.tscn");

    private readonly List<Node2D> _activePuddles = new();

    public override void _Ready()
    {
        AddToGroup("enemy");
        CollisionLayer = CollisionConfig.EnemyLayer;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.BulletLayer;
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
            _sprite.Modulate = _baseTint;
        Health = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (Health == null)
        {
            Health = new HealthComponent { Name = "HealthComponent", MaxHP = MaxHP, CurrentHP = MaxHP };
            AddChild(Health);
        }
        Health.Died += OnDied;
        _startPosition = GlobalPosition;
        EnterState(State.Idle);
    }

    private void OnDied()
    {
        GameLogger.Info("Crackpot", "Crackpot defeated.");
        Health.Died -= OnDied;

        // Clean up all puddles
        CleansePuddles();

        // Death fade
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate:a", 0f, 0.3f);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            var arena = GetParent() as CombatArena;
            arena?.OnEnemyDefeated();
            QueueFree();
        }));
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
                // Flash while telegraphing
                if (_sprite != null)
                    _sprite.Modulate = _stateTimer % 0.15f < 0.075f ? TelegraphColor : _baseTint;
                if (_stateTimer >= TeleDuration)
                {
                    PickTargetPosition();
                    EnterState(State.Leaping);
                }
                break;

            case State.Leaping:
                // Arc toward target
                _leapProgress = Mathf.Clamp(_stateTimer / 0.4f, 0f, 1f);
                if (_leapProgress < 1f)
                {
                    float t = _leapProgress;
                    float eased = t * t * (3f - 2f * t); // smoothstep
                    float heightOffset = -Mathf.Sin(t * Mathf.Pi) * 60f; // arc arc
                    Vector2 pos = _startPosition.Lerp(_targetPosition, eased);
                    pos.Y += heightOffset;
                    GlobalPosition = pos;

                    // Rotate sprite while leaping
                    if (_sprite != null)
                        _sprite.Rotation += dt * 8f;
                }
                else
                {
                    // Land — spawn puddle
                    GlobalPosition = _targetPosition;
                    if (_sprite != null)
                        _sprite.Rotation = 0f;
                    SpawnPuddle();
                    EnterState(State.PuddleActive);
                }
                break;

            case State.PuddleActive:
                // Idle while puddle is active
                if (_stateTimer >= PuddleLifetime)
                    EnterState(State.Cooldown);
                break;

            case State.Cooldown:
                if (_stateTimer >= CooldownDuration)
                    EnterState(State.Idle);
                break;
        }
    }

    private void EnterState(State newState)
    {
        _state = newState;
        _stateTimer = 0f;

        switch (newState)
        {
            case State.Idle:
                _stateDuration = 0.8f + (float)GD.RandRange(0, 0.5f);
                _startPosition = GlobalPosition;
                if (_sprite != null)
                {
                    _sprite.Modulate = _baseTint;
                    _sprite.Rotation = 0f;
                }
                break;

            case State.Telegraph:
                _stateDuration = TeleDuration;
                break;

            case State.Leaping:
                _leapProgress = 0f;
                break;

            case State.PuddleActive:
                _stateDuration = PuddleLifetime;
                if (_sprite != null)
                    _sprite.Modulate = _baseTint;
                break;

            case State.Cooldown:
                _stateDuration = CooldownDuration;
                if (_sprite != null)
                    _sprite.Modulate = new Color(0.8f, 0.8f, 0.8f); // desaturated = tired
                break;
        }
    }

    private void PickTargetPosition()
    {
        // Pick a random position within the combat area
        float rangeX = 300f;
        float rangeY = 200f;
        float x = (float)GD.RandRange(-rangeX, rangeX);
        float y = (float)GD.RandRange(-rangeY, rangeY);
        _targetPosition = new Vector2(x, y);

        // Face toward target
        Vector2 dir = GlobalPosition.DirectionTo(_targetPosition);
        if (_sprite != null)
            _sprite.Scale = new Vector2(dir.X >= 0 ? 1 : -1, 1);
    }

    private void SpawnPuddle()
    {
        if (PuddleScene == null) return;

        var puddle = PuddleScene.Instantiate<Node2D>();
        puddle.GlobalPosition = GlobalPosition;
        GetParent().AddChild(puddle);
        _activePuddles.Add(puddle);
    }

    /// <summary>
    /// Called on parry. Bounces away and cleanses all active puddles.
    /// </summary>
    public void OnParried(Vector2 knockback)
    {
        GameLogger.Debug("Crackpot", "Crackpot parried!");

        // Cleanse puddles
        CleansePuddles();

        // Bounce away
        var tween = CreateTween();
        tween.TweenProperty(this, "position", Position + knockback * 0.3f, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (_state != State.Cooldown)
                EnterState(State.Cooldown);
        }));

        // Visual feedback
        if (_sprite != null)
        {
            _sprite.Modulate = StunColor;
            var resetTween = CreateTween();
            resetTween.TweenInterval(0.3f);
            resetTween.TweenProperty(_sprite, "modulate", _baseTint, 0.2f);
        }
    }

    private void CleansePuddles()
    {
        foreach (var puddle in _activePuddles)
        {
            if (GodotObject.IsInstanceValid(puddle))
                puddle.QueueFree();
        }
        _activePuddles.Clear();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player") && _state != State.Cooldown && _state != State.Telegraph)
            Player.Instance.HealthComponent?.TakeDamage(ContactDamage, this);
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is RedBullet bullet && bullet.Reflected)
        {
            OnParried(GlobalPosition.DirectionTo(bullet.GlobalPosition) * 300f);
        }
    }
}
