using Godot;

public partial class SavedGame : Resource
{
    [Export]
    public Vector2 PlayerPosition;
    [Export]
    public Godot.Collections.Array<SavedData> SaveData;
}
