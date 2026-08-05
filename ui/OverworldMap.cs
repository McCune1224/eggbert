using Godot;

/// <summary>
/// Always-on HUD minimap: a semi-transparent schematic map of the current level with a
/// player dot, NPC dots, and door dots. Generated automatically per level by
/// <see cref="LevelMapGenerator"/> — no per-level authoring.
/// </summary>
/// <remarks>
/// <b>Lifecycle:</b> Mirrors <see cref="ObjectiveTracker"/> — instantiated by
/// <see cref="GameController"/> into its Menu node, listens to
/// <c>LevelLoadStarted</c>/<c>LevelLoaded</c>, hidden during dialogs, cutscenes, and
/// combat arenas. The map panel sits bottom-right (top-right is the objective tracker,
/// top-left the debug overlay).
/// <b>Rendering:</b> The map texture is drawn by <see cref="MapCanvas"/> once per frame
/// (the player dot moves); the texture itself is rebuilt only on level load.
/// </remarks>
public partial class OverworldMap : CanvasLayer
{
    private const float CornerMargin = 10f;

    private PanelContainer _panel;
    private MapCanvas _canvas;
    private LevelMapData _data;
    private bool _levelReady;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");
        _canvas = new MapCanvas();
        _panel.AddChild(_canvas);
        _panel.Visible = false;

        GameController.Instance.LevelLoadStarted += OnLevelLoadStarted;
        GameController.Instance.LevelLoaded += OnLevelLoaded;
    }

    public override void _ExitTree()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.LevelLoadStarted -= OnLevelLoadStarted;
            GameController.Instance.LevelLoaded -= OnLevelLoaded;
        }
    }

    private void OnLevelLoadStarted()
    {
        _levelReady = false;
        _data = null;
        _panel.Visible = false;
    }

    private async void OnLevelLoaded()
    {
        // Let tilemaps register bounds and physics bodies settle before sampling.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInsideTree())
            return;

        var level = GameController.Instance.CurrentLevel;
        _levelReady = level is BaseLevel && level is not CombatArena;
        if (!_levelReady)
            return;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _data = LevelMapGenerator.Generate(level);
        sw.Stop();
        GameLogger.Debug("OverworldMap", $"'{level.Name}': map generated in {sw.ElapsedMilliseconds}ms — " +
            $"{_data?.Markers.Count ?? 0} markers");

        if (_data == null)
        {
            _panel.Visible = false;
            return;
        }

        _canvas.Data = _data;
        _canvas.CustomMinimumSize = _data.Texture.GetSize();
        _panel.ResetSize();
        AnchorToCorner();
        _panel.Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!_levelReady || _data == null)
            return;

        bool blocked = DialogManager.Instance.IsDialogActive
            || CutsceneController.Instance.IsPlaying
            || GameController.Instance.CurrentLevel is CombatArena;
        _panel.Visible = !blocked;
        _canvas.QueueRedraw(); // player dot tracks movement
    }

    /// <summary>Anchors the panel to the bottom-right corner with a fixed margin.</summary>
    private void AnchorToCorner()
    {
        Vector2 size = _panel.Size;
        _panel.OffsetLeft = -(size.X + CornerMargin);
        _panel.OffsetTop = -(size.Y + CornerMargin);
        _panel.OffsetRight = -CornerMargin;
        _panel.OffsetBottom = -CornerMargin;
    }
}

/// <summary>
/// Draws the map texture plus the player/NPC/door dots. Sized by the panel container to
/// the map texture's dimensions.
/// </summary>
public partial class MapCanvas : Control
{
    private static readonly Color PlayerDot = new(1f, 0.90f, 0.40f, 1f);
    private static readonly Color PlayerDotOutline = new(0f, 0f, 0f, 0.55f);
    private static readonly Color NpcDot = new(0.45f, 0.95f, 0.50f, 1f);
    private static readonly Color DoorDot = new(1f, 0.55f, 0.30f, 1f);

    public LevelMapData Data;

    public override void _Draw()
    {
        if (Data?.Texture == null)
            return;

        DrawTexture(Data.Texture, Vector2.Zero);

        foreach (MapMarker marker in Data.Markers)
        {
            if (!Data.ContainsWorld(marker.WorldPosition))
                continue;
            DrawCircle(ClampToMap(marker.WorldPosition), 2.6f,
                marker.Kind == MapMarkerKind.Npc ? NpcDot : DoorDot);
        }

        var player = Player.Instance;
        if (player != null && Data.ContainsWorld(player.GlobalPosition))
        {
            var playerPos = ClampToMap(player.GlobalPosition);
            DrawCircle(playerPos, 4f, PlayerDotOutline);
            DrawCircle(playerPos, 2.6f, PlayerDot);
        }
    }

    /// <summary>
    /// Projects a world position to map pixels, clamped inside the texture so dots near
    /// (or just past) the map edge stay visible instead of overflowing the panel.
    /// </summary>
    private Vector2 ClampToMap(Vector2 worldPos)
    {
        const float inset = 2.5f;
        var raw = Data.WorldToMap(worldPos);
        return new Vector2(
            Mathf.Clamp(raw.X, inset, Data.Texture.GetWidth() - inset),
            Mathf.Clamp(raw.Y, inset, Data.Texture.GetHeight() - inset));
    }
}
