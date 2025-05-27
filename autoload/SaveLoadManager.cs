using Godot;



public partial class SaveLoadManager : Node
{

    private static SaveLoadManager _instance;
    public static SaveLoadManager Instance => _instance;

    private const string SavePath = "user://savegame.tres";

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            QueueFree(); // Ensure only one instance exists
        }
    }


    public void SaveGame()
    {
        SaveResource newSave = new();
        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            node.Call("Save", newSave);
            // TODO: Hand the array to each node to do whatever it needs to save, and then add the result to the resources array
        }

        ResourceSaver.Save(newSave, SavePath);
    }

    public void LoadGame()
    {
        if (!ResourceLoader.Exists(SavePath))
            return;

        var loadedResource = ResourceLoader.Load(SavePath);
        var saveData = loadedResource as SaveResource;
        if (saveData == null)
        {
            GD.PrintErr($"Failed to load SaveGameData from {SavePath}. Resource type: {loadedResource?.GetType().Name}");
            return;
        }


        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is IPersistable persistable)
            {
                persistable.Load(saveData);
            }
            else
            {
                GD.PrintErr($"Node {node.Name} does not implement IPersistable despite being in 'persist' group.");
            }
        }

    }
}
