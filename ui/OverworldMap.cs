using Godot;

/// <summary>
/// Always-on HUD minimap: a semi-transparent pixel-art schematic map of the current
/// level with a player dot, NPC dots, door dots, save-point dots, warp dots, and a
/// blinking quest marker for the pinned quest's current objective. Generated
/// automatically per level by <see cref="LevelMapGenerator"/> — no per-level authoring.
/// </summary>
/// <remarks>
/// <b>Lifecycle:</b> Mirrors <see cref="ObjectiveTracker"/> — instantiated by
/// <see cref="GameController"/> into its Menu node, listens to
/// <c>LevelLoadStarted</c>/<c>LevelLoaded</c>, hidden during dialogs, cutscenes, and
/// combat arenas. The map panel sits bottom-right (top-right is the objective tracker,
/// top-left the debug overlay). Press <c>M</c> (<c>toggle_map</c>) to show/hide it.
/// <b>Rendering:</b> All dots are pixel-snapped squares/diamonds (no antialiasing) and
/// the map texture draws with nearest filtering for a crisp pixel-art look.
/// </remarks>
public partial class OverworldMap : CanvasLayer
{
    private const float CornerMargin = 10f;

    private PanelContainer _panel;
    private Label _levelLabel;
    private MapCanvas _canvas;
    private LevelMapData _data;
    private bool _levelReady;
    private bool _mapVisible = true;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");

        _levelLabel = new Label
        {
            ThemeTypeVariation = "HudLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        if (FontCache.Yoster != null)
            _levelLabel.AddThemeFontOverride("font", FontCache.Yoster);
        _panel.AddChild(_levelLabel);

        _canvas = new MapCanvas();
        _panel.AddChild(_canvas);
        _panel.Visible = false;

        GameController.Instance.LevelLoadStarted += OnLevelLoadStarted;
        GameController.Instance.LevelLoaded += OnLevelLoaded;
        QuestManager.Instance.QuestStateChanged += ResolveQuestMarker;
    }

    public override void _ExitTree()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.LevelLoadStarted -= OnLevelLoadStarted;
            GameController.Instance.LevelLoaded -= OnLevelLoaded;
        }

        if (QuestManager.Instance != null)
            QuestManager.Instance.QuestStateChanged -= ResolveQuestMarker;
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
        _levelLabel.Text = level is BaseLevel baseLevel ? baseLevel.LevelName : level.Name;
        _levelLabel.CustomMinimumSize = new Vector2(_data.Texture.GetWidth(), 0);

        _panel.ResetSize();
        AnchorToCorner();
        ResolveQuestMarker();
        _panel.Visible = true;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("toggle_map"))
            _mapVisible = !_mapVisible;

        if (!_levelReady || _data == null)
            return;

        bool blocked = DialogManager.Instance.IsDialogActive
            || CutsceneController.Instance.IsPlaying
            || GameController.Instance.CurrentLevel is CombatArena;
        _panel.Visible = _mapVisible && !blocked;
        _canvas.QueueRedraw(); // player dot + quest blink track state
    }

    /// <summary>Re-resolves the quest marker from the pinned quest's current objective.</summary>
    private void ResolveQuestMarker()
    {
        if (_canvas == null)
            return;

        var level = GameController.Instance?.CurrentLevel;
        _canvas.QuestWorldPosition = _levelReady && level != null
            ? LevelMapGenerator.ResolvePinnedObjectivePosition(QuestManager.Instance, level.SceneFilePath)
            : null;
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
/// Draws the map texture plus pixel-art markers (player, NPC, door, save, warp, quest).
/// Sized by the panel container to the map texture's dimensions.
/// </summary>
public partial class MapCanvas : Control
{
    // Palette — semi-transparent map + bright pixel markers.
    private static readonly Color PlayerDot = new(1f, 0.90f, 0.40f, 1f);
    private static readonly Color PlayerDotOutline = new(0f, 0f, 0f, 0.75f);
    private static readonly Color NpcDot = new(0.35f, 0.95f, 0.45f, 1f);
    private static readonly Color DoorDot = new(1f, 0.55f, 0.28f, 1f);
    private static readonly Color SaveDot = new(0.45f, 0.75f, 1f, 1f);
    private static readonly Color WarpDot = new(0.78f, 0.50f, 1f, 1f);
    private static readonly Color QuestDot = new(1f, 0.40f, 0.85f, 1f);
    private static readonly Color FrameLight = new(0.83f, 0.77f, 0.63f, 1f);
    private static readonly Color FrameDark = new(0.09f, 0.08f, 0.11f, 1f);

    public LevelMapData Data;
    /// <summary>World position of the pinned quest objective, or null when not in this level.</summary>
    public Vector2? QuestWorldPosition;

    public override void _Ready()
    {
        // Crisp pixel-art: never smooth-scale the map texture.
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public override void _Draw()
    {
        if (Data?.Texture == null)
            return;

        Vector2 size = Data.Texture.GetSize();
        DrawTexture(Data.Texture, Vector2.Zero);
        DrawPixelFrame(size);

        // Static markers (NPCs, doors, save points, warps).
        foreach (MapMarker marker in Data.Markers)
        {
            if (!Data.ContainsWorld(marker.WorldPosition))
                continue;
            Vector2 pos = PixelPos(marker.WorldPosition);
            switch (marker.Kind)
            {
                case MapMarkerKind.Npc:
                    DrawOutlinedSquare(pos, NpcDot);
                    break;
                case MapMarkerKind.Door:
                    DrawOutlinedSquare(pos, DoorDot);
                    break;
                case MapMarkerKind.SavePoint:
                    DrawPlus(pos, SaveDot);
                    break;
                case MapMarkerKind.WarpPoint:
                    DrawOutlinedSquare(pos, WarpDot);
                    break;
            }
        }

        // Blinking quest star for the pinned objective.
        if (QuestWorldPosition is Vector2 questPos && Data.ContainsWorld(questPos))
        {
            int radius = (int)(Time.GetTicksMsec() / 300) % 2 == 0 ? 2 : 1;
            DrawDiamond(PixelPos(questPos), radius + 1, PlayerDotOutline);
            DrawDiamond(PixelPos(questPos), radius, QuestDot);
        }

        // Player dot tracks movement every frame.
        var player = Player.Instance;
        if (player != null && Data.ContainsWorld(player.GlobalPosition))
        {
            Vector2 playerPos = PixelPos(player.GlobalPosition);
            DrawDiamond(playerPos, 3, PlayerDotOutline);
            DrawDiamond(playerPos, 2, PlayerDot);
        }
    }

    /// <summary>Projects a world position to the map pixel grid (floored so dots snap to pixels).</summary>
    private Vector2 PixelPos(Vector2 worldPos)
    {
        return Data.WorldToMap(worldPos).Floor();
    }

    private void DrawPixel(Vector2 pos, Color color)
    {
        DrawRect(new Rect2(pos, Vector2.One), color);
    }

    private void DrawOutlinedSquare(Vector2 center, Color color)
    {
        DrawSquare(center, 2, PlayerDotOutline);
        DrawSquare(center, 1, color);
    }

    private void DrawSquare(Vector2 center, int half, Color color)
    {
        for (int dy = -half; dy <= half; dy++)
            for (int dx = -half; dx <= half; dx++)
                DrawPixel(center + new Vector2(dx, dy), color);
    }

    private void DrawDiamond(Vector2 center, int radius, Color color)
    {
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radius)
                    DrawPixel(center + new Vector2(dx, dy), color);
    }

    /// <summary>Plus-shaped marker (save points).</summary>
    private void DrawPlus(Vector2 center, Color color)
    {
        for (int i = -1; i <= 1; i++)
        {
            DrawPixel(center + new Vector2(0, i), color);
            DrawPixel(center + new Vector2(i, 0), color);
        }
        DrawPixel(center + new Vector2(1, 1), PlayerDotOutline);
        DrawPixel(center + new Vector2(-1, 1), PlayerDotOutline);
        DrawPixel(center + new Vector2(1, -1), PlayerDotOutline);
        DrawPixel(center + new Vector2(-1, -1), PlayerDotOutline);
    }

    /// <summary>Classic RPG map frame: 1px dark ring + 1px cream ring around the map.</summary>
    private void DrawPixelFrame(Vector2 size)
    {
        float w = size.X;
        float h = size.Y;

        DrawRect(new Rect2(-1, -1, w + 2, 1), FrameDark);
        DrawRect(new Rect2(-1, h, w + 2, 1), FrameDark);
        DrawRect(new Rect2(-1, -1, 1, h + 2), FrameDark);
        DrawRect(new Rect2(w, -1, 1, h + 2), FrameDark);

        DrawRect(new Rect2(0, 0, w, 1), FrameLight);
        DrawRect(new Rect2(0, h - 1, w, 1), FrameLight);
        DrawRect(new Rect2(0, 0, 1, h), FrameLight);
        DrawRect(new Rect2(w - 1, 0, 1, h), FrameLight);
    }
}
