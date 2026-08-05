using Godot;
using System.Collections.Generic;

/// <summary>
/// What a marker on the overworld map represents.
/// </summary>
public enum MapMarkerKind
{
    /// <summary>An interactable character (dialog/quiz NPC).</summary>
    Npc,
    /// <summary>An exit/transition or a door component.</summary>
    Door
}

/// <summary>
/// A single point of interest drawn on the overworld map, in world coordinates.
/// </summary>
public readonly struct MapMarker
{
    public Vector2 WorldPosition { get; }
    public MapMarkerKind Kind { get; }
    /// <summary>Source node name, used for debug logging and future tooltips.</summary>
    public string Label { get; }

    public MapMarker(Vector2 worldPosition, MapMarkerKind kind, string label)
    {
        WorldPosition = worldPosition;
        Kind = kind;
        Label = label;
    }
}

/// <summary>
/// Result of <see cref="LevelMapGenerator.Generate"/>: the schematic map texture,
/// the world rectangle it covers, and the detected markers.
/// </summary>
/// <remarks>
/// The texture is drawn at one pixel per world tile (16px) by default, up to 2px per
/// tile when the level is small enough to fit the display cap. <see cref="WorldToMap"/>
/// maps world coordinates onto the texture's pixel space so markers and the player dot
/// can be placed exactly.
/// </remarks>
public class LevelMapData
{
    /// <summary>The semi-transparent schematic map texture (RGBA8).</summary>
    public ImageTexture Texture;

    /// <summary>World-space rectangle covered by the map (includes a half-tile margin).</summary>
    public Rect2 WorldBounds;

    /// <summary>Map size in world tiles (columns × rows).</summary>
    public Vector2I Cells;

    /// <summary>Texture pixels per world tile (1 or 2 in the common case; fractional after downscaling huge levels).</summary>
    public float PixelsPerCell = 1f;

    /// <summary>Detected points of interest in world coordinates.</summary>
    public List<MapMarker> Markers = new();

    /// <summary>World tile size in pixels used by all level tilesets.</summary>
    public const float TileSize = 16f;

    /// <summary>
    /// Projects a world position onto the map texture's pixel space (top-left origin).
    /// </summary>
    public Vector2 WorldToMap(Vector2 worldPos)
    {
        return (worldPos - WorldBounds.Position) / TileSize * PixelsPerCell;
    }

    /// <summary>True when <paramref name="worldPos"/> falls inside the mapped world rectangle.</summary>
    public bool ContainsWorld(Vector2 worldPos) => WorldBounds.HasPoint(worldPos);
}
