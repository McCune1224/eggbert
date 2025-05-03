using Godot;
using Godot.Collections;

public partial class PlayerCamera : Camera2D
{

    public override void _Ready()
    {
        GameController.Instance.Connect(nameof(GameController.TileMapBoundsChanged), new Callable(this, nameof(UpdateLimits)));
        UpdateLimits(GameController.Instance.CurrentTileMapBounds);
    }

    public void UpdateLimits(Array<Vector2> limits)
    {
        if (limits == null) { GD.PrintErr("Limits are null."); return; }
        if (limits.Count == 0)
        {
            GD.PrintErr("No limits provided to update.");
            return;
        }
        GD.Print("HIT UPDATE LIMITS WITH NONE LIMITS");
        LimitLeft = (int)limits[0].X;
        LimitTop = (int)limits[0].Y;
        LimitRight = (int)limits[1].X;
        LimitBottom = (int)limits[1].Y;
    }
}
