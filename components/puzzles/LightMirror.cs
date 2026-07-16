using Godot;

/// <summary>
/// A pushable mirror that reflects light beams.
/// Grid-aligned: placed at 45° angles for orthogonal beam reflection.
/// </summary>
[GlobalClass]
[Tool]
public partial class LightMirror : StaticBody2D
{
    [ExportGroup("Mirror")]
    [Export] public Texture2D MirrorTexture { get; set; }

    private Sprite2D _sprite;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.WallsLayer;
        CollisionMask = CollisionConfig.PlayerLayer;

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null && MirrorTexture != null)
            _sprite.Texture = MirrorTexture;

        AddToGroup("pushable");
    }

    /// <summary>
    /// Rotate the mirror 45° clockwise (cycles through 4 orientations).
    /// </summary>
    public void Rotate()
    {
        Rotation += Mathf.DegToRad(45);
    }

    /// <summary>
    /// Returns the surface normal for the current rotation.
    /// Used by LightBeam for reflection calculation.
    /// </summary>
    public Vector2 GetSurfaceNormal(Vector2 incomingDir)
    {
        // For a mirror at 45°, the normal is perpendicular to the surface
        float angle = Rotation;
        Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        if (normal.Dot(incomingDir) > 0)
            normal = -normal;
        return normal;
    }
}
