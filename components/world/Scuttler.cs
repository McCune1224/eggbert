using Godot;

/// <summary>
/// Small background creature (rat, bug) that scuttles back and forth.
/// Triggers movement when player gets close.
/// </summary>
/// <summary>
/// Small background creature (rat, bug) that scuttles back and forth.
/// Triggers movement when player gets close.
/// Can be configured with any small sprite texture.
/// </summary>
public partial class Scuttler : Sprite2D
    [Export] public float TriggerRadius { get; set; } = 80f;
    [Export] public float PauseMin { get; set; } = 1f;
    [Export] public float PauseMax { get; set; } = 3f;

    private Vector2 _startPosition;
    private Vector2 _targetPosition;
    private bool _moving = false;
    private bool _returning = false;
    private float _pauseTimer = 0f;
    private Area2D _triggerArea;

    public override void _Ready()
    {
        _startPosition = Position;
        _targetPosition = _startPosition + new Vector2(ScuttleDistance, 0);
        Scale = new Vector2(-1, 1); // Face right by default

        // Create trigger area
        _triggerArea = new Area2D();
        var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = TriggerRadius } };
        _triggerArea.AddChild(shape);
        AddChild(_triggerArea);
        _triggerArea.CollisionLayer = 0;
        _triggerArea.CollisionMask = CollisionConfig.PlayerLayer;
        _triggerArea.BodyEntered += OnTriggerEntered;

        _pauseTimer = (float)GD.RandRange(PauseMin, PauseMax);
    }

    public override void _Process(double delta)
    {
        if (!_moving && _pauseTimer > 0f)
        {
            _pauseTimer -= (float)delta;
            if (_pauseTimer <= 0f)
            {
                _moving = true;
                _returning = false;
                FlipH = true;
            }
            return;
        }

        if (!_moving) return;

        Vector2 target = _returning ? _startPosition : _targetPosition;
        Vector2 dir = target - Position;

        if (dir.LengthSquared() < 4f)
        {
            Position = target;
            _moving = false;
            _pauseTimer = (float)GD.RandRange(PauseMin, PauseMax);
            FlipH = !_returning; // Face opposite direction
            _returning = !_returning;
            return;
        }

        Position += dir.Normalized() * ScuttleSpeed * (float)delta;
    }

    private void OnTriggerEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        // Wake up and start moving
        if (!_moving && _pauseTimer > 0f)
            _pauseTimer = 0.1f; // Quick start
    }
}
