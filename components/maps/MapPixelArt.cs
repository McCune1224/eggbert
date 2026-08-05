using Godot;
using System;

/// <summary>
/// Shared pixel-art palette and shape helpers for the overworld map HUD and the
/// pause-menu full map. All shapes are drawn through a pixel-setter delegate so the
/// same code renders to a Control (<see cref="Control._Draw"/>) or into an
/// <see cref="Image"/> (baked pause-menu texture).
/// </summary>
public static class MapPixelArt
{
    // Palette — semi-transparent map + bright pixel markers.
    public static readonly Color PlayerDot = new(1f, 0.90f, 0.40f, 1f);
    public static readonly Color PlayerDotOutline = new(0f, 0f, 0f, 0.75f);
    public static readonly Color NpcDot = new(0.35f, 0.95f, 0.45f, 1f);
    public static readonly Color DoorDot = new(1f, 0.55f, 0.28f, 1f);
    public static readonly Color SaveDot = new(0.45f, 0.75f, 1f, 1f);
    public static readonly Color WarpDot = new(0.78f, 0.50f, 1f, 1f);
    public static readonly Color QuestDot = new(1f, 0.40f, 0.85f, 1f);
    public static readonly Color FrameLight = new(0.83f, 0.77f, 0.63f, 1f);
    public static readonly Color FrameDark = new(0.09f, 0.08f, 0.11f, 1f);

    /// <summary>Marker color by kind.</summary>
    public static Color KindColor(MapMarkerKind kind)
    {
        return kind switch
        {
            MapMarkerKind.Npc => NpcDot,
            MapMarkerKind.Door => DoorDot,
            MapMarkerKind.SavePoint => SaveDot,
            MapMarkerKind.WarpPoint => WarpDot,
            _ => DoorDot,
        };
    }

    /// <summary>Draws a marker for <paramref name="kind"/> at <paramref name="center"/>.</summary>
    public static void DrawMarker(Action<Vector2, Color> px, MapMarkerKind kind, Vector2 center)
    {
        switch (kind)
        {
            case MapMarkerKind.SavePoint:
                DrawPlus(px, center, SaveDot);
                break;
            default:
                DrawOutlinedSquare(px, center, KindColor(kind));
                break;
        }
    }

    public static void DrawOutlinedSquare(Action<Vector2, Color> px, Vector2 center, Color color)
    {
        DrawSquare(px, center, 2, PlayerDotOutline);
        DrawSquare(px, center, 1, color);
    }

    public static void DrawSquare(Action<Vector2, Color> px, Vector2 center, int half, Color color)
    {
        for (int dy = -half; dy <= half; dy++)
            for (int dx = -half; dx <= half; dx++)
                px(center + new Vector2(dx, dy), color);
    }

    public static void DrawDiamond(Action<Vector2, Color> px, Vector2 center, int radius, Color color)
    {
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radius)
                    px(center + new Vector2(dx, dy), color);
    }

    /// <summary>Plus-shaped marker (save points).</summary>
    public static void DrawPlus(Action<Vector2, Color> px, Vector2 center, Color color)
    {
        for (int i = -1; i <= 1; i++)
        {
            px(center + new Vector2(0, i), color);
            px(center + new Vector2(i, 0), color);
        }
        px(center + new Vector2(1, 1), PlayerDotOutline);
        px(center + new Vector2(-1, 1), PlayerDotOutline);
        px(center + new Vector2(1, -1), PlayerDotOutline);
        px(center + new Vector2(-1, -1), PlayerDotOutline);
    }

    /// <summary>
    /// A small wedge on the player dot pointing along <paramref name="facing"/> —
    /// drawn only when the facing is (close to) axis-aligned.
    /// </summary>
    public static void DrawFacingNub(Action<Vector2, Color> px, Vector2 center, Vector2 facing, Color color)
    {
        if (facing.X > 0.5f)
        {
            px(center + new Vector2(2, 0), color);
            px(center + new Vector2(3, 0), color);
        }
        else if (facing.X < -0.5f)
        {
            px(center + new Vector2(-2, 0), color);
            px(center + new Vector2(-3, 0), color);
        }
        else if (facing.Y > 0.5f)
        {
            px(center + new Vector2(0, 2), color);
            px(center + new Vector2(0, 3), color);
        }
        else if (facing.Y < -0.5f)
        {
            px(center + new Vector2(0, -2), color);
            px(center + new Vector2(0, -3), color);
        }
    }

    /// <summary>Classic RPG map frame: 1px dark ring + 1px cream ring around the map.</summary>
    public static void DrawFrame(Action<Vector2, Color> px, Vector2 size)
    {
        float w = size.X;
        float h = size.Y;

        DrawLine(px, new Vector2(-1, -1), new Vector2(w + 1, -1), FrameDark);
        DrawLine(px, new Vector2(-1, h), new Vector2(w + 1, h), FrameDark);
        DrawLine(px, new Vector2(-1, -1), new Vector2(-1, h + 1), FrameDark);
        DrawLine(px, new Vector2(w, -1), new Vector2(w, h + 1), FrameDark);

        DrawLine(px, new Vector2(0, 0), new Vector2(w - 1, 0), FrameLight);
        DrawLine(px, new Vector2(0, h - 1), new Vector2(w - 1, h - 1), FrameLight);
        DrawLine(px, new Vector2(0, 0), new Vector2(0, h - 1), FrameLight);
        DrawLine(px, new Vector2(w - 1, 0), new Vector2(w - 1, h - 1), FrameLight);
    }

    private static void DrawLine(Action<Vector2, Color> px, Vector2 from, Vector2 to, Color color)
    {
        if (Mathf.IsEqualApprox(from.Y, to.Y))
        {
            for (int x = (int)Mathf.Min(from.X, to.X); x <= (int)Mathf.Max(from.X, to.X); x++)
                px(new Vector2(x, from.Y), color);
        }
        else if (Mathf.IsEqualApprox(from.X, to.X))
        {
            for (int y = (int)Mathf.Min(from.Y, to.Y); y <= (int)Mathf.Max(from.Y, to.Y); y++)
                px(new Vector2(from.X, y), color);
        }
    }
}
