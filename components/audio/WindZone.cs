using Godot;

/// <summary>
/// Outdoor area with wind ambience that swells and fades.
/// Add to outdoor levels (courtyard, beach, overworld).
/// </summary>
public partial class WindZone : Area2D
{
    [Export] public AudioStream WindLoop { get; set; }
    [Export] public float MaxVolume { get; set; } = -6f;
    [Export] public float FadeSeconds { get; set; } = 2f;

    private AudioStreamPlayer2D _player;
    private Tween _tween;
    private bool _playerInside = false;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionConfig.PlayerLayer;

        _player = new AudioStreamPlayer2D
        {
            Stream = WindLoop,
            Bus = "SFX",
            VolumeDb = -80f
        };
        AddChild(_player);

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInside = true;

        if (WindLoop == null) return;
        if (!_player.Playing) _player.Play();

        FadeTo(MaxVolume);
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInside = false;

        FadeTo(-80f);
    }

    private void FadeTo(float targetDb)
    {
        if (_tween != null && _tween.IsValid())
            _tween.Kill();

        _tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(_player, "volume_db", targetDb, FadeSeconds);
        _tween.TweenCallback(Callable.From(() =>
        {
            if (!_playerInside && _player.Playing)
                _player.Stop();
        }));
    }
}
