using Godot;
using Godot.Collections;
using System;

public partial class LevelTileMapLayer : TileMapLayer
{
    public override void _Ready()
    {
        GameController.Instance.ChangeTileMapBounds(GetTilemapBounds());
    }
    public override void _Process(double delta) { }
    public Array<Vector2> GetTilemapBounds()
    {
        Array<Vector2> bounds = new Array<Vector2>();
        bounds.Add(GetUsedRect().Position * RenderingQuadrantSize);
        bounds.Add(GetUsedRect().End * RenderingQuadrantSize);

        return bounds;
    }
}
