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
        _audioPlayer = new AudioStreamPlayer { Bus = "SFX" };
        AddChild(_audioPlayer);

        SetProcess(true);
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
            CollisionMask = CollisionConfig.WallsLayer
        };

        var result = space.IntersectRay(query);
        if (result.Count == 0)
        {
            _currentFloor = FloorType.Default;
            return;
        }

        var collider = result["collider"].Obj;
        if (collider is TileMapLayer tileLayer)
        {
            // Determine floor type from tile metadata or layer name
            string layerName = tileLayer.Name.ToString().ToLower();
            if (layerName.Contains("grass")) _currentFloor = FloorType.Grass;
            else if (layerName.Contains("metal") || layerName.Contains("factory")) _currentFloor = FloorType.Metal;
            else if (layerName.Contains("water")) _currentFloor = FloorType.Water;
            else if (layerName.Contains("wood")) _currentFloor = FloorType.Wood;
            else _currentFloor = FloorType.Stone;
        }
        else
        {
            _currentFloor = FloorType.Stone;
        }
    }

    private void PlayFootstep()
    {
        AudioStream sfx = GetFloorSfx();
        if (sfx == null) return;

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
