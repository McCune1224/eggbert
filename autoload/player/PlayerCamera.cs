using Godot;
using Godot.Collections;

public partial class PlayerCamera : Camera2D
{
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeElapsed;
    private Vector2 _shakeOffset;

    public override void _Ready()
    {
        GameController.Instance.Connect(nameof(GameController.TileMapBoundsChanged), new Callable(this, nameof(UpdateLimits)));
    }

    public void UpdateLimits(Array<Vector2> limits)
    {
        if (limits == null) { GameLogger.Error("Camera", "Limits are null."); return; }
        if (limits.Count == 0)
        {
            GameLogger.Error("Camera", "No limits provided to update.");
            return;
        }
        LimitLeft = (int)limits[0].X;
        LimitTop = (int)limits[0].Y;
        LimitRight = (int)limits[1].X;
        LimitBottom = (int)limits[1].Y;
    }

    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        _shakeDuration = Mathf.Max(_shakeDuration, duration);
        _shakeElapsed = 0f;
    }

    public override void _Process(double delta)
    {
        if (_shakeElapsed < _shakeDuration)
        {
            _shakeElapsed += (float)delta;
            float t = _shakeElapsed / _shakeDuration;
            float decay = 1f - t;
            _shakeOffset = new Vector2(
                (float)GD.RandRange(-1f, 1f) * _shakeIntensity * decay,
                (float)GD.RandRange(-1f, 1f) * _shakeIntensity * decay
            );
            Offset = _shakeOffset;
        }
        else if (_shakeOffset != Vector2.Zero)
        {
            _shakeOffset = Vector2.Zero;
            Offset = Vector2.Zero;
        }
    }
}
