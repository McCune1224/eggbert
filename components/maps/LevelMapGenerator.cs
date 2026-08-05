using Godot;
using System.Collections.Generic;

/// <summary>
/// Builds a schematic map for any level at runtime, with zero per-level authoring.
/// </summary>
/// <remarks>
/// <b>Why a physics probe instead of tile data:</b> Eggbert tilesets carry no per-tile
/// collision (no <c>physics_layer</c> in any <c>assets/tilemaps/*.tres</c>); walls are
/// <c>StaticBody2D</c> bodies plus the procedural <c>MapBorders</c> spawned by
/// <see cref="LevelTileMapLayer"/>. So the map samples the level's physics space: every
/// world tile inside the union of tilemap used-rects is probed with a small circle
/// against the Walls layer. Blocked tiles render as walls, walkable tiles as floor, and
/// unpainted tiles stay transparent unless something blocks them (e.g. unpainted
/// barriers between painted lanes).
///
/// <b>Markers:</b> NPCs and doors are detected structurally from the level's direct
/// children — see <see cref="ScanMarkers"/>. No scene edits are required for a level to
/// appear on the map.
/// </remarks>
public static class LevelMapGenerator
{
    /// <summary>Maximum map texture width in pixels.</summary>
    public const int MaxMapWidth = 176;
    /// <summary>Maximum map texture height in pixels.</summary>
    public const int MaxMapHeight = 128;

    /// <summary>Probe radius for painted cells — small enough that 1-tile corridors survive
    /// (a cell center is 8px from its edges; r=6 never reaches the neighbor cell).</summary>
    private const float ProbeRadius = 6f;
    /// <summary>Probe radius for unpainted border/barrier cells — large enough to catch the
    /// 2px MapBorders that sit just outside the painted grid.</summary>
    private const float BorderProbeRadius = 8f;

    // Palette (RGBA8). The map is semi-transparent so gameplay stays readable underneath.
    private static readonly Color FloorColor = new(0.13f, 0.15f, 0.24f, 0.62f);
    private static readonly Color WallColor = new(0.70f, 0.76f, 0.88f, 0.78f);

    /// <summary>
    /// Generates the schematic map for <paramref name="levelRoot"/>. The level must be
    /// inside the scene tree so its physics bodies are registered.
    /// </summary>
    /// <returns>Map data, or <c>null</c> when the level has no usable tilemap.</returns>
    public static LevelMapData Generate(Node levelRoot)
    {
        var tileLayers = new List<TileMapLayer>();
        foreach (Node node in levelRoot.FindChildren("*", "TileMapLayer", true, false))
        {
            if (node is TileMapLayer tml)
                tileLayers.Add(tml);
        }
        if (tileLayers.Count == 0)
        {
            GameLogger.Debug("LevelMapGenerator", $"'{levelRoot.Name}': no TileMapLayer children — no map.");
            return null;
        }

        // Union of the layers' used rects in ABSOLUTE world-cell coordinates (layers may
        // be nested and positioned, e.g. the warp-icon tilemap under WarpPoint).
        var cellOrigin = Vector2I.Zero;
        var cellEnd = Vector2I.Zero;
        bool haveCells = false;
        var paintedCells = new HashSet<Vector2I>();
        foreach (var layer in tileLayers)
        {
            if (layer.TileSet == null)
                continue;
            var usedRect = layer.GetUsedRect();
            if (usedRect.Size.X <= 0 || usedRect.Size.Y <= 0)
                continue;

            var halfTile = (Vector2)layer.TileSet.TileSize / 2f;
            var worldTl = layer.ToGlobal(layer.MapToLocal(usedRect.Position)) - halfTile;
            var worldBr = layer.ToGlobal(layer.MapToLocal(usedRect.End - Vector2I.One)) + halfTile;
            var layerOrigin = new Vector2I(Mathf.FloorToInt(worldTl.X / LevelMapData.TileSize),
                Mathf.FloorToInt(worldTl.Y / LevelMapData.TileSize));
            var layerEnd = new Vector2I(Mathf.CeilToInt(worldBr.X / LevelMapData.TileSize),
                Mathf.CeilToInt(worldBr.Y / LevelMapData.TileSize));

            if (!haveCells)
            {
                cellOrigin = layerOrigin;
                cellEnd = layerEnd;
                haveCells = true;
            }
            else
            {
                cellOrigin = new Vector2I(Mathf.Min(cellOrigin.X, layerOrigin.X), Mathf.Min(cellOrigin.Y, layerOrigin.Y));
                cellEnd = new Vector2I(Mathf.Max(cellEnd.X, layerEnd.X), Mathf.Max(cellEnd.Y, layerEnd.Y));
            }

            foreach (Vector2I cell in layer.GetUsedCells())
            {
                var worldCenter = layer.ToGlobal(layer.MapToLocal(cell));
                paintedCells.Add(CellKey(worldCenter));
            }
        }

        if (!haveCells)
        {
            GameLogger.Debug("LevelMapGenerator", $"'{levelRoot.Name}': no painted tilemap layers — no map.");
            return null;
        }

        var cellsW = cellEnd.X - cellOrigin.X;
        var cellsH = cellEnd.Y - cellOrigin.Y;
        if (cellsW <= 0 || cellsH <= 0)
            return null;

        // Extend the map by one cell on every side: the unpainted margin ring is probed
        // with a larger radius so the level's 2px MapBorders render as a wall frame, and
        // it keeps the world→map transform exact (bounds.Position = first cell's corner).
        var probeOrigin = cellOrigin - Vector2I.One;
        var probeEnd = cellEnd + Vector2I.One;
        var mapCellsW = probeEnd.X - probeOrigin.X;
        var mapCellsH = probeEnd.Y - probeOrigin.Y;

        var bounds = new Rect2(probeOrigin.X * LevelMapData.TileSize,
            probeOrigin.Y * LevelMapData.TileSize,
            mapCellsW * LevelMapData.TileSize, mapCellsH * LevelMapData.TileSize);

        var data = new LevelMapData
        {
            WorldBounds = bounds,
            Cells = new Vector2I(mapCellsW, mapCellsH),
        };

        SampleWalkability(levelRoot, probeOrigin, paintedCells, data);
        ScanMarkers(levelRoot, data);
        return data;
    }

    /// <summary>Quantizes a world position to the containing tile key (floor, not round —
    /// banker's rounding would shift alternate cells by ±1).</summary>
    private static Vector2I CellKey(Vector2 worldPos)
    {
        return new Vector2I(
            Mathf.FloorToInt(worldPos.X / LevelMapData.TileSize),
            Mathf.FloorToInt(worldPos.Y / LevelMapData.TileSize));
    }

    /// <summary>
    /// Probes every relevant tile inside the union cell rect against the Walls physics
    /// layer and bakes the result into the map texture.
    /// </summary>
    private static void SampleWalkability(Node levelRoot, Vector2I cellOrigin,
        HashSet<Vector2I> paintedCells, LevelMapData data)
    {
        var cellsW = data.Cells.X;
        var cellsH = data.Cells.Y;
        int probeCount = 0;
        var bytes = new byte[cellsW * cellsH * 4];

        var space = ((Node2D)levelRoot).GetWorld2D().DirectSpaceState;
        var probe = new CircleShape2D { Radius = ProbeRadius };
        var borderProbe = new CircleShape2D { Radius = BorderProbeRadius };

        for (int y = 0; y < cellsH; y++)
        {
            for (int x = 0; x < cellsW; x++)
            {
                var cell = new Vector2I(cellOrigin.X + x, cellOrigin.Y + y);
                var center = new Vector2(cell.X + 0.5f, cell.Y + 0.5f) * LevelMapData.TileSize;
                bool painted = paintedCells.Contains(cell);

                // Probe painted tiles (walkable or wall) plus thin border/barrier tiles:
                // the map edge (MapBorders are unpainted 2px bodies) and unpainted tiles
                // adjacent to painted ones (unpainted barriers between lanes).
                bool borderCell = false;
                if (!painted)
                {
                    bool edge = x == 0 || y == 0 || x == cellsW - 1 || y == cellsH - 1;
                    bool nearPainted = (x > 0 && paintedCells.Contains(new Vector2I(cell.X - 1, cell.Y)))
                        || (x + 1 < cellsW && paintedCells.Contains(new Vector2I(cell.X + 1, cell.Y)))
                        || (y > 0 && paintedCells.Contains(new Vector2I(cell.X, cell.Y - 1)))
                        || (y + 1 < cellsH && paintedCells.Contains(new Vector2I(cell.X, cell.Y + 1)));
                    if (!edge && !nearPainted)
                        continue; // interior void — leave transparent
                    borderCell = true;
                }

                probeCount++;
                bool blocked = IsBlocked(space, borderCell ? borderProbe : probe, center);

                int idx = (y * cellsW + x) * 4;
                if (painted)
                {
                    WriteColor(bytes, idx, blocked ? WallColor : FloorColor);
                }
                else if (blocked)
                {
                    WriteColor(bytes, idx, WallColor); // unpainted barrier/border
                }
                // else: unpainted, unblocked → transparent
            }
        }

        GameLogger.Debug("LevelMapGenerator", $"'{levelRoot.Name}': sampled {probeCount} tiles ({cellsW}x{cellsH} cells)");

        var image = Image.CreateFromData(cellsW, cellsH, false, Image.Format.Rgba8, bytes);
        ApplySizeCap(image, data);

        data.Texture = ImageTexture.CreateFromImage(image);
    }

    private static bool IsBlocked(PhysicsDirectSpaceState2D space, CircleShape2D probe, Vector2 center)
    {
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = probe,
            Transform = new Transform2D(0f, center),
            CollisionMask = CollisionConfig.WallsLayer,
            CollideWithAreas = false,
            CollideWithBodies = true,
        };
        return space.IntersectShape(query, 4).Count > 0;
    }

    /// <summary>Scales the image down when the level is bigger than the display cap.</summary>
    private static void ApplySizeCap(Image image, LevelMapData data)
    {
        int w = image.GetWidth();
        int h = image.GetHeight();

        // Upscale small levels to 2px per tile for readability when they fit.
        if (w * 2 <= MaxMapWidth && h * 2 <= MaxMapHeight)
        {
            image.Resize(w * 2, h * 2, Image.Interpolation.Nearest);
        }
        else
        {
            w = image.GetWidth();
            h = image.GetHeight();
        }

        if (image.GetWidth() > MaxMapWidth || image.GetHeight() > MaxMapHeight)
        {
            float factor = Mathf.Min((float)MaxMapWidth / image.GetWidth(), (float)MaxMapHeight / image.GetHeight());
            image.Resize(Mathf.Max(1, Mathf.RoundToInt(image.GetWidth() * factor)),
                Mathf.Max(1, Mathf.RoundToInt(image.GetHeight() * factor)), Image.Interpolation.Bilinear);
        }

        data.PixelsPerCell = (float)image.GetWidth() / data.Cells.X;
    }

    private static void WriteColor(byte[] bytes, int idx, Color c)
    {
        bytes[idx] = (byte)(c.R * 255f);
        bytes[idx + 1] = (byte)(c.G * 255f);
        bytes[idx + 2] = (byte)(c.B * 255f);
        bytes[idx + 3] = (byte)(c.A * 255f);
    }

    /// <summary>
    /// Detects NPC and door markers from the level's direct children (the documented
    /// authoring convention places components at the level root).
    /// </summary>
    /// <remarks>
    /// NPC detection, in priority order:
    /// 1. Any node in the <c>"npc"</c> group (explicit opt-in for characters whose
    ///    interaction trigger is a separate node, e.g. OfficerBacon).
    /// 2. A plain <c>Node2D</c> (not itself an Area2D trigger) with a Sprite2D child and
    ///    an <c>OnInteract</c> <see cref="CutsceneTrigger"/> or
    ///    <see cref="DialogBranchTrigger"/> child — matches NPC_Gardener, Chef,
    ///    PatrolGuard, etc. while excluding trigger-rooted props (TimeClock,
    ///    VendingMachine).
    /// 3. <see cref="QuizNpc"/> and <see cref="SleepingNPC"/> roots count directly.
    /// Doors: <see cref="LevelTransition"/> exits plus <see cref="Door"/> components
    /// (<see cref="KeyDoor"/>/<see cref="TimedDoor"/> are subclasses).
    /// </remarks>
    public static void ScanMarkers(Node levelRoot, LevelMapData data)
    {
        foreach (Node child in levelRoot.GetChildren())
        {
            switch (child)
            {
                case LevelTransition:
                    data.Markers.Add(new MapMarker(((Node2D)child).GlobalPosition, MapMarkerKind.Door, child.Name));
                    break;
                case Door:
                    data.Markers.Add(new MapMarker(((Node2D)child).GlobalPosition, MapMarkerKind.Door, child.Name));
                    break;
                case SavePoint:
                    data.Markers.Add(new MapMarker(((Node2D)child).GlobalPosition, MapMarkerKind.SavePoint, child.Name));
                    break;
                case WarpPoint:
                    data.Markers.Add(new MapMarker(((Node2D)child).GlobalPosition, MapMarkerKind.WarpPoint, child.Name));
                    break;
                case QuizNpc:
                case SleepingNPC:
                    data.Markers.Add(new MapMarker(((Node2D)child).GlobalPosition, MapMarkerKind.Npc, child.Name));
                    break;
                default:
                    if (child.IsInGroup("npc") || IsCharacterNpc(child))
                        data.Markers.Add(new MapMarker(((Node2D)child).GlobalPosition, MapMarkerKind.Npc, child.Name));
                    break;
            }
        }
    }

    /// <summary>
    /// Resolves the map-marker position for the pinned quest's current objective, or
    /// <c>null</c> when there is no active pinned objective located in
    /// <paramref name="currentLevelPath"/>. Pure function — testable headless.
    /// </summary>
    public static Vector2? ResolvePinnedObjectivePosition(QuestManager questManager, string currentLevelPath)
    {
        if (questManager == null || string.IsNullOrEmpty(currentLevelPath))
            return null;

        QuestDefinition quest = questManager.GetPinnedQuest();
        QuestObjective objective = questManager.GetCurrentObjective(quest);
        if (objective == null || string.IsNullOrEmpty(objective.LocationLevel))
            return null;

        return string.Equals(objective.LocationLevel, currentLevelPath, System.StringComparison.Ordinal)
            ? objective.LocationPosition
            : null;
    }

    private static bool IsCharacterNpc(Node node)
    {
        if (node is not Node2D node2d)
            return false;
        // Area2D roots are trigger zones and props (TimeClock, VendingMachine,
        // BaconChat, …), not NPCs — even when they carry a sprite.
        if (node2d is Area2D)
            return false;

        bool hasSprite = false;
        bool hasNpcTrigger = false;
        foreach (Node childNode in node2d.GetChildren())
        {
            if (childNode is Sprite2D or AnimatedSprite2D)
                hasSprite = true;
            else if (childNode is CutsceneTrigger cutscene && cutscene.Mode == TriggerMode.OnInteract)
                hasNpcTrigger = true;
            else if (childNode is DialogBranchTrigger)
                hasNpcTrigger = true;
        }
        return hasSprite && hasNpcTrigger;
    }
}
