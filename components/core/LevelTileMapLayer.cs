using Godot;
using Godot.Collections;

public partial class LevelTileMapLayer : TileMapLayer
{
    public override void _Ready()
    {
        GameController.Instance.ChangeTileMapBounds(GetTilemapBounds());
    }
    public Array<Vector2> GetTilemapBounds()
    {
        Array<Vector2> bounds = new Array<Vector2>();
        bounds.Add(GetUsedRect().Position * RenderingQuadrantSize);
        bounds.Add(GetUsedRect().End * RenderingQuadrantSize);

        return bounds;
    }
}
