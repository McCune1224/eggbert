using Godot;
using Godot.Collections;

public partial class LevelTileMapLayer : TileMapLayer
{
    private const float BorderThickness = 2f;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        if (!TryGetUsedLocalRect(out var localRect))
            return;

        CreateMapBorders(localRect);

        if (GameController.Instance == null)
        {
            GameLogger.Warn("LevelTileMapLayer", $"'{Name}': GameController is unavailable — skipping camera bounds update");
            return;
        }

        var bounds = GetWorldBounds(localRect);
        GameController.Instance.ChangeTileMapBounds(bounds);
        GameLogger.Debug("LevelTileMapLayer", $"'{Name}': sent bounds {bounds[0]} → {bounds[1]}");
    }

    private bool TryGetUsedLocalRect(out Rect2 localRect)
    {
        localRect = default;

        if (TileSet == null)
        {
            GameLogger.Warn("LevelTileMapLayer", $"'{Name}': TileSet is null — skipping map borders and camera bounds");
            return false;
        }

        var usedRect = GetUsedRect();
        if (usedRect.Size.X <= 0 || usedRect.Size.Y <= 0)
        {
            GameLogger.Warn("LevelTileMapLayer", $"'{Name}': tilemap has no used cells — skipping map borders and camera bounds");
            return false;
        }

        var halfTileSize = (Vector2)TileSet.TileSize / 2f;
        var topLeft = MapToLocal(usedRect.Position) - halfTileSize;
        var bottomRight = MapToLocal(usedRect.End - Vector2I.One) + halfTileSize;
        localRect = new Rect2(topLeft, bottomRight - topLeft);
        return true;
    }

    private Array<Vector2> GetWorldBounds(Rect2 localRect)
    {
        var topLeft = ToGlobal(localRect.Position);
        var topRight = ToGlobal(new Vector2(localRect.End.X, localRect.Position.Y));
        var bottomLeft = ToGlobal(new Vector2(localRect.Position.X, localRect.End.Y));
        var bottomRight = ToGlobal(localRect.End);

        var minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X), Mathf.Min(bottomLeft.X, bottomRight.X));
        var minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y), Mathf.Min(bottomLeft.Y, bottomRight.Y));
        var maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X), Mathf.Max(bottomLeft.X, bottomRight.X));
        var maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y), Mathf.Max(bottomLeft.Y, bottomRight.Y));

        return new Array<Vector2>
        {
            new Vector2(minX, minY),
            new Vector2(maxX, maxY)
        };
    }

    private void CreateMapBorders(Rect2 localRect)
    {
        var borders = new Node2D { Name = "MapBorders" };
        AddChild(borders);

        var center = localRect.GetCenter();
        var horizontalSize = new Vector2(localRect.Size.X + BorderThickness * 2f, BorderThickness);
        var verticalSize = new Vector2(BorderThickness, localRect.Size.Y);

        AddBorder(borders, "North", horizontalSize, new Vector2(center.X, localRect.Position.Y - BorderThickness / 2f));
        AddBorder(borders, "South", horizontalSize, new Vector2(center.X, localRect.End.Y + BorderThickness / 2f));
        AddBorder(borders, "West", verticalSize, new Vector2(localRect.Position.X - BorderThickness / 2f, center.Y));
        AddBorder(borders, "East", verticalSize, new Vector2(localRect.End.X + BorderThickness / 2f, center.Y));
    }

    private static void AddBorder(Node2D parent, string borderName, Vector2 size, Vector2 position)
    {
        var body = new StaticBody2D
        {
            Name = borderName,
            Position = position,
            CollisionLayer = CollisionConfig.WallsLayer,
            CollisionMask = 0
        };
        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = size }
        };

        body.AddChild(shape);
        parent.AddChild(body);
    }
}
