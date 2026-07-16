using Godot;

/// <summary>
/// A light beam emitted from a source. Casts rays, reflects off mirrors,
/// and activates sensors on contact. Uses Line2D for visual beam.
/// </summary>
[GlobalClass]
[Tool]
public partial class LightBeam : Node2D
{
    [Export] public float BeamLength { get; set; } = 400f;
    [Export] public Color BeamColor { get; set; } = new Color(1, 0.8f, 0.2f, 0.9f);
    [Export] public float BeamWidth { get; set; } = 4f;
    [Export] public int MaxReflections { get; set; } = 10;

    private Line2D _line;
    private Vector2 _direction = Vector2.Right;

    public override void _Ready()
    {
        _line = new Line2D
        {
            Width = BeamWidth,
            DefaultColor = BeamColor,
            Antialiased = true
        };
        AddChild(_line);

        CastBeam();
    }

    public void SetDirection(Vector2 dir)
    {
        _direction = dir.Normalized();
        CastBeam();
    }

    public void CastBeam()
    {
        var points = new System.Collections.Generic.List<Vector2>();
        points.Add(Vector2.Zero);

        Vector2 origin = Vector2.Zero;
        Vector2 dir = _direction;
        var space = GetWorld2D().DirectSpaceState;

        for (int i = 0; i < MaxReflections; i++)
        {
            var query = new PhysicsRayQueryParameters2D
            {
                From = GlobalPosition + origin,
                To = GlobalPosition + origin + dir * BeamLength,
                CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.TriggerAreaLayer,
                CollideWithAreas = true,
                CollideWithBodies = true
            };

            var result = space.IntersectRay(query);
            if (result.Count == 0)
            {
                points.Add(origin + dir * BeamLength);
                break;
            }

            Vector2 hitPoint = result["position"].AsVector2() - GlobalPosition;
            points.Add(hitPoint);

            var collider = result["collider"].Obj;
            if (collider is LightMirror mirror)
            {
                Vector2 normal = result["normal"].AsVector2();
                dir = dir.Bounce(normal).Normalized();
                origin = hitPoint;
                continue;
            }

            if (collider is LightSensor sensor)
            {
                sensor.Activate();
                break;
            }

            break;
        }

        _line.Points = points.ToArray();
    }
}
