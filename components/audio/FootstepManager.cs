using Godot;

/// <summary>
/// Plays footstep sounds based on the floor type the player is walking on.
/// Detects floor type from TileMap layer / collision.
/// </summary>
public partial class FootstepManager : Node
{
    [Export] public AudioStream StoneSfx { get; set; }
    [Export] public AudioStream MetalSfx { get; set; }
    [Export] public AudioStream GrassSfx { get; set; }
    [Export] public AudioStream WaterSfx { get; set; }
    [Export] public AudioStream WoodSfx { get; set; }
    [Export] public float StepInterval { get; set; } = 0.35f;

    private float _stepTimer = 0f;
    private Player _player;
    private AudioStreamPlayer _audioPlayer;

    // TileSet custom-data layer names for floor type
    private enum FloorType { Stone, Metal, Grass, Water, Wood, Default }
    private FloorType _currentFloor = FloorType.Default;

    public override void _Ready()
    {
        _player = Player.Instance;
        if (_player == null)
        {
            GameLogger.Error("FootstepManager", $"'{Name}': Player.Instance is null!");
            SetProcess(false);
            return;
        }

        _audioPlayer = new AudioStreamPlayer { Bus = "SFX" };
        AddChild(_audioPlayer);

        SetProcess(true);
        GameLogger.Debug("FootstepManager", $"'{Name}': _Ready — stepInterval={StepInterval}");
    }

    public override void _Process(double delta)
    {
        if (_player == null) return;

        // Only play footsteps when moving
        Vector2 velocity = _player.Velocity;
        if (velocity.LengthSquared() < 1f)
        {
            _stepTimer = 0f;
            return;
        }

        DetectFloorType();
        _stepTimer += (float)delta;

        if (_stepTimer >= StepInterval)
        {
            _stepTimer = 0f;
            PlayFootstep();
        }
    }

    private void DetectFloorType()
    {
        // Raycast downward from player position to detect floor type
        var space = _player.GetWorld2D().DirectSpaceState;
        var query = new PhysicsRayQueryParameters2D
        {
            From = _player.GlobalPosition + new Vector2(0, 16),
            To = _player.GlobalPosition + new Vector2(0, 32),
            CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.InteractableLayer | CollisionConfig.PlayerLayer
        };

        var result = space.IntersectRay(query);
        if (result.Count == 0)
        {
            if (_currentFloor != FloorType.Default)
            {
                _currentFloor = FloorType.Default;
                GameLogger.Debug("FootstepManager", $"'{Name}': floor → Default (no tile)");
            }
            return;
        }

        var collider = result["collider"].Obj;
        if (collider is TileMapLayer tileLayer)
        {
            // Determine floor type from tile metadata or layer name
            string layerName = tileLayer.Name.ToString().ToLower();
            FloorType newFloor = _currentFloor;
            if (layerName.Contains("grass")) newFloor = FloorType.Grass;
            else if (layerName.Contains("metal") || layerName.Contains("factory")) newFloor = FloorType.Metal;
            else if (layerName.Contains("water")) newFloor = FloorType.Water;
            else if (layerName.Contains("wood")) newFloor = FloorType.Wood;
            else newFloor = FloorType.Stone;

            if (newFloor != _currentFloor)
            {
                _currentFloor = newFloor;
                GameLogger.Debug("FootstepManager", $"'{Name}': floor → {newFloor} (layer='{layerName}')");
            }
        }
        else
        {
            if (_currentFloor != FloorType.Stone)
            {
                _currentFloor = FloorType.Stone;
                GameLogger.Debug("FootstepManager", $"'{Name}': floor → Stone (non-TileMap collider)");
            }
        }
    }

    private void PlayFootstep()
    {
        AudioStream sfx = GetFloorSfx();
        if (sfx == null)
        {
            GameLogger.Warn("FootstepManager", $"'{Name}': no SFX for floor '{_currentFloor}'");
            return;
        }

        _audioPlayer.Stream = sfx;
        _audioPlayer.PitchScale = 0.9f + (float)GD.RandRange(0, 0.2f);
        _audioPlayer.Play();
    }

    private AudioStream GetFloorSfx() => _currentFloor switch
    {
        FloorType.Grass => GrassSfx,
        FloorType.Metal => MetalSfx,
        FloorType.Water => WaterSfx,
        FloorType.Wood => WoodSfx,
        _ => StoneSfx
    };
}
