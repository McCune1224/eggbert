using Godot;

public partial class SavedGame : Resource
{
    [Export]
    Vector2 PlayerPosition;
    [Export]
    Godot.Collections.Array<SavedData> saveData;
}
