using Godot;
using Godot.Collections;

public partial class LevelTileMapLayer : TileMapLayer
{
    public override void _Ready()
    {
        var bounds = GetTilemapBounds();
        GameController.Instance.ChangeTileMapBounds(bounds);
        GameLogger.Debug("LevelTileMapLayer", $"'{Name}': sent bounds {bounds[0]} → {bounds[1]}");
    }
    public Array<Vector2> GetTilemapBounds()
    {
        Array<Vector2> bounds = new Array<Vector2>();
        bounds.Add(GetUsedRect().Position * RenderingQuadrantSize);
        bounds.Add(GetUsedRect().End * RenderingQuadrantSize);

        return bounds;
    }
}
