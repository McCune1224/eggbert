using Godot;

public partial class RollingEgg : Area2D
{
    private enum State { Idle, Telegraph, Attacking, Cooldown, Stunned }

    [Export] public float ChargeSpeed = 250f;
    [Export] public int MaxHP = 40;
    [Export] public int ContactDamage = 10;

    public HealthComponent Health { get; private set; }

    private State _state = State.Idle;
    private float _stateTimer = 0f;
    private float _stateDuration = 1f;
    private Vector2 _moveDirection = Vector2.Down;
    private int _wallBounces = 0;
    private Vector2 _knockbackVelocity = Vector2.Zero;
    private float _knockbackFriction = 0.85f;

    private Sprite2D _sprite;
    private Color _baseTint = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color TelegraphColor = new Color(1f, 1f, 0.3f);
    private static readonly Color StunColor = new Color(0.5f, 0.5f, 1f);

    public override void _Ready()
    {
        AddToGroup("enemy");
        CollisionLayer = CollisionConfig.EnemyLayer;
        CollisionMask = CollisionConfig.PlayerLayer | CollisionConfig.BulletLayer;
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;

        _sprite = GetNode<Sprite2D>("Sprite2D");
        _sprite.Modulate = _baseTint;

        Health = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (Health == null)
        {
            Health = new HealthComponent { Name = "HealthComponent", MaxHP = MaxHP, CurrentHP = MaxHP };
            AddChild(Health);
        }
        Health.Died += OnDied;

        // Pick random initial direction
        _moveDirection = Vector2.Right.Rotated((float)GD.RandRange(0, Mathf.Pi * 2));
        EnterState(State.Idle);
    }

    private void OnDied()
    {
        Health.Died -= OnDied;
        var arena = GetParent() as CombatArena;
        arena?.OnEnemyDefeated();
        QueueFree();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Handle knockback from parry
        if (_knockbackVelocity.LengthSquared() > 1f)
        {
            GlobalPosition += _knockbackVelocity * dt;
            _knockbackVelocity *= _knockbackFriction;
            if (_knockbackVelocity.LengthSquared() < 1f)
                _knockbackVelocity = Vector2.Zero;
            return; // skip state machine while knockback is active
        }

        _stateTimer += dt;

        switch (_state)
        {
            case State.Idle:
                if (_stateTimer >= _stateDuration)
                    EnterState(State.Telegraph);
                break;

            case State.Telegraph:
            {
                float t = Mathf.Clamp(_stateTimer / _stateDuration, 0f, 1f);
                float pulse = (Mathf.Sin(t * Mathf.Pi * 4f) * 0.5f + 0.5f) * t;
                if (_sprite != null)
                    _sprite.Modulate = _baseTint.Lerp(TelegraphColor, pulse);
                if (_stateTimer >= _stateDuration)
                    EnterState(State.Attacking);
                break;
            }

            case State.Attacking:
            {
                GlobalPosition += _moveDirection * ChargeSpeed * dt;

                // Bounce off walls — check arena bounds. Arena is ~480x320 centered.
                // Using GenericArena default size (480, 320).
                float halfW = 240f, halfH = 160f;
                if (GlobalPosition.X < -halfW) { GlobalPosition = new Vector2(-halfW, GlobalPosition.Y); _moveDirection = new Vector2(-_moveDirection.X, _moveDirection.Y); _wallBounces++; }
                if (GlobalPosition.X > halfW) { GlobalPosition = new Vector2(halfW, GlobalPosition.Y); _moveDirection = new Vector2(-_moveDirection.X, _moveDirection.Y); _wallBounces++; }
                if (GlobalPosition.Y < -halfH) { GlobalPosition = new Vector2(GlobalPosition.X, -halfH); _moveDirection = new Vector2(_moveDirection.X, -_moveDirection.Y); _wallBounces++; }
                if (GlobalPosition.Y > halfH) { GlobalPosition = new Vector2(GlobalPosition.X, halfH); _moveDirection = new Vector2(_moveDirection.X, -_moveDirection.Y); _wallBounces++; }

                // After 3 bounces or 4s, return to cooldown
                if (_wallBounces >= 3 || _stateTimer >= 4f)
                    EnterState(State.Cooldown);
                break;
            }

            case State.Cooldown:
                if (_stateTimer >= _stateDuration)
                    EnterState(State.Idle);
                break;

            case State.Stunned:
                if (_stateTimer >= _stateDuration)
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
                _stateDuration = (float)GD.RandRange(0.8, 1.5);
                if (_sprite != null) _sprite.Modulate = _baseTint;
                // Pick new direction toward player (weighted) or random
                float randomChance = (float)GD.RandRange(0f, 1f);
                if (randomChance < 0.6f)
                {
                    Vector2 toPlayer = CombatTargeter.GetPlayerPosition() - GlobalPosition;
                    _moveDirection = toPlayer.Normalized();
                }
                else
                {
                    _moveDirection = Vector2.Right.Rotated((float)GD.RandRange(0, Mathf.Pi * 2));
                }
                break;

            case State.Telegraph:
                _stateDuration = 0.6f;
                _wallBounces = 0;
                break;

            // Attacking is instantaneous — EnterState transitions same frame.
            case State.Cooldown:
                _stateDuration = (float)GD.RandRange(0.5, 1.0);
                if (_sprite != null) _sprite.Modulate = _baseTint;
                break;

            case State.Stunned:
                _stateDuration = 0.8f;
                if (_sprite != null) _sprite.Modulate = StunColor;
                break;
        }
    }

    /// <summary>Called by ParryComponent on successful parry. knockback is a velocity impulse.</summary>
    public void OnParried(Vector2 knockback)
    {
        _knockbackVelocity = knockback;
        EnterState(State.Stunned);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player") && _state == State.Attacking)
        {
            Player.Instance.HealthComponent?.TakeDamage(ContactDamage, this);
            EnterState(State.Cooldown);
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        // Bullets hitting this enemy — redirected via parry
        if (area is RedBullet bullet && bullet.Reflected)
        {
            Health.TakeDamage(10, bullet);
        }
    }
}
