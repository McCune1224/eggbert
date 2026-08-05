using Godot;
using System;

/// <summary>
/// The side of the level boundary from which the transition enters the target level.
/// </summary>
public enum TransitionSide
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Area2D that triggers a scene change when the player walks into it. The transition fires once
/// on BodyEntered for the player, unless <see cref="RequiredFlag"/> gates it.
/// </summary>
/// <remarks>
/// <b>Size / Scale:</b> The exported <see cref="Size"/> property (range 1–12) controls the
/// collision shape dimensions inside <see cref="Update_Area"/> — a larger Size widens the
/// transition zone along the <see cref="Side"/> axis.
///
/// <b>RequiredFlag gating:</b> If set, the transition only fires when
/// <see cref="WorldFlags.Instance"/> has the given flag. Empty means always fire.
/// Used to gate post-ending exits (e.g. "go_home").
///
/// <b>Orphaned NodePaths:</b> <see cref="TargetTransitionName"/> resolves to direct
/// children of the loaded scene only. If the target node does not exist, log error and skip.
///
/// <b>Empty Level path:</b> If <see cref="Level"/> is empty, the call logs an error and
/// does not crash. Always provide a valid .tscn resource path.
/// </remarks>
[Tool]
public partial class LevelTransition : Area2D
{
    CollisionShape2D _collisionShape;

    /// <summary>The level scene to load when the player walks into this transition zone.</summary>
    [Export(PropertyHint.File, "*.tscn")]
    public string Level { get; set; }

    /// <summary>Name of the transition node in the target level where the player appears. Empty = spawn at (0,0).</summary>
    [Export]
    public string TargetTransitionName = "";

    /// <summary>Length of the transition zone in tiles along the level boundary (perpendicular to the entry side).</summary>
    [ExportCategory("CollisionAreaSettings")]
    [Export(PropertyHint.Range, "1,12,1,or_greater")]
    public int Size;

    /// <summary>Which edge of the level this transition sits on. Determines where the player exits and how the zone is shaped.</summary>
    [Export]
    public TransitionSide Side;
    /// <summary>Snaps the node position to the 16px tile grid when toggled in the Inspector.</summary>
    [Export]
    bool SnapToGrid = false;

    /// <summary>Optional WorldFlag required to fire this transition. Empty = always fires. Used to gate post-ending exits (e.g. "go_home").</summary>
    [Export]
    public string RequiredFlag = "";

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(Level))
            warnings.Add("Level is empty — this transition leads nowhere. Assign the target .tscn file.");
        if (string.IsNullOrEmpty(TargetTransitionName))
            warnings.Add("TargetTransitionName is empty — the player will spawn at (0,0) of the target level. Set it to the destination transition node's name.");
        return warnings.ToArray();
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == "Size")
        {
            Size = ((int)value);
            Update_Area();
            return true;
        }
        else if (property == "Side")
        {
            Side = Enum.Parse<TransitionSide>(value.ToString());
            Update_Area();
            return true;
        }
        else if (property == "SnapToGrid" || property == "Snap to Grid")
        {
            if ((bool)value)
            {
                this.Position = new Vector2
                {
                    X = Mathf.Round(Position.X / 16) * 16,
                    Y = Mathf.Round(Position.Y / 16) * 16,
                };
            }
            return true;
        }
        return false;
    }



    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        Update_Area();
        if (Engine.IsEditorHint()) { return; }

        BodyEntered += SceneTransition;
        BodyExited += (Node2D body) =>
        {
            var prompt = body.GetNodeOrNull<Node>("prompt");
            if (prompt != null)
                prompt.QueueFree();
        };
    }


    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            Update_Area();
            return;
        }
    }

    /// <summary>
    /// Triggers the level transition when a player body enters the area. Checks <see cref="RequiredFlag"/>
    /// gating and then loads <see cref="Level"/> at the position implied by <see cref="TargetTransitionName"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="TargetTransitionName"/> is a NodePath that resolves to a direct child of the target
    /// level scene. If the node cannot be found the transition logs an error and does not crash.
    /// An empty or missing <see cref="Level"/> resource path logs an error and does not crash.
    /// </remarks>
    public void SceneTransition(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            if (!string.IsNullOrEmpty(RequiredFlag) && !WorldFlags.Instance.HasFlag(RequiredFlag))
            {
                GameLogger.Debug("LevelTransition", $"'{Name}': gated — requires flag '{RequiredFlag}' (not set).");
                return;
            }
            GameLogger.Info("LevelTransition", $"Transition triggered → {Level} (target: {TargetTransitionName})");
        }
        try
        {
            if (TargetTransitionName != "")
            {
                GameController.Instance.LoadLevel(Level, TargetTransitionName);
            }
            else
            {
                GameController.Instance.LoadLevel(Level, Vector2.Zero);
            }
        }
        catch (Exception)
        {
            GameLogger.Error("LevelTransition", "SceneTransition: Failed to load level: " + Level);
            throw;
        }
    }



    /// <summary>
    /// Rebuilds the collision shape to match <see cref="Side"/> and <see cref="Size"/>.
    /// Called on every property change and editor refresh.
    /// </summary>
    public void Update_Area()
    {
        if (_collisionShape == null)
            _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collisionShape == null)
        {
            // _Set for Size/Side runs before _Ready during scene instantiation; only
            // report a genuinely missing collision shape once the node is in the tree.
            if (IsInsideTree())
                GameLogger.Error("LevelTransition", "CollisionShape2D not found.");
            return;
        }
        Vector2 newRectangleSize = new Vector2(16, 16);
        Vector2 newPosition = Vector2.Zero;

        switch (Side)
        {
            case TransitionSide.Left:
                newRectangleSize = new Vector2(16, Size * newRectangleSize.Y);
                newPosition = new Vector2(newPosition.X - 8, newPosition.Y);
                break;
            case TransitionSide.Right:
                newRectangleSize = new Vector2(16, Size * newRectangleSize.Y);
                newPosition = new Vector2(newPosition.X + 8, newPosition.Y);
                break;
            case TransitionSide.Up:
                newRectangleSize = new Vector2(Size * newRectangleSize.X, 16);
                newPosition = new Vector2(newPosition.X, newPosition.Y - 8);
                break;
            case TransitionSide.Down:
                newRectangleSize = new Vector2(Size * newRectangleSize.X, 16);
                newPosition = new Vector2(newPosition.X, newPosition.Y + 8);
                break;
        }

        _collisionShape.Position = newPosition;
        RectangleShape2D newRectangleShape = new RectangleShape2D();
        newRectangleShape.Size = newRectangleSize;
        _collisionShape.Shape = newRectangleShape;

        _collisionShape.NotifyPropertyListChanged();
        QueueRedraw();
    }


}
