using Godot;

/// <summary>
/// Detects when a light beam hits it. Activates doors or other machinery.
/// </summary>
public partial class LightSensor : Area2D
{
    [Signal]
    public delegate void BeamReceivedEventHandler();

    [ExportGroup("Target")]
    [Export]
    /// Path to a Door node that opens when the beam hits this sensor.
    public NodePath TargetDoorPath { get; set; }
    [ExportGroup("Visuals")]
    [Export]
    /// Color when a beam is actively hitting the sensor.
    public Color ActiveColor { get; set; } = new Color(0, 1, 0, 0.5f);
    [Export]
    /// Color when no beam is hitting the sensor.
    public Color InactiveColor { get; set; } = new Color(1, 0, 0, 0.3f);
    private bool _active = false;
    private Sprite2D _sprite;
    private Door _targetDoor;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.TriggerAreaLayer;
        CollisionMask = 0; // Detected via raycast from LightBeam, not physics overlap

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
            _sprite.Modulate = InactiveColor;

        if (TargetDoorPath != null && !TargetDoorPath.IsEmpty)
            _targetDoor = GetNodeOrNull<Door>(TargetDoorPath);
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (TargetDoorPath == null || TargetDoorPath.IsEmpty)
            warnings.Add("TargetDoorPath is not set. The sensor will activate but won't open any door.");
        return warnings.ToArray();
    }
    public void Activate()
    {
        if (_active) return;
        _active = true;

        if (_sprite != null)
            _sprite.Modulate = ActiveColor;

        _targetDoor?.Open();
        EmitSignal(SignalName.BeamReceived);
    }

    public void Deactivate()
    {
        if (!_active) return;
        _active = false;

        if (_sprite != null)
            _sprite.Modulate = InactiveColor;

        _targetDoor?.Close();
    }
}
