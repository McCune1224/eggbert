using Godot;
using Godot.Collections;


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

        // Collect and sort IPersistable nodes by load priority
        var persistables = new System.Collections.Generic.List<IPersistable>();
        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is IPersistable persistable)
            {
                persistables.Add(persistable);
            }
            else
            {
                GD.PrintErr($"Node {node.Name} does not implement IPersistable.Save despite being in 'persist' group.");
            }
        }

        persistables.Sort((a, b) => a.GetLoadPriority().CompareTo(b.GetLoadPriority()));

        // Save each persistable node in sorted order
        foreach (var persistable in persistables)
        {
            persistable.Save(newSave);
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
                GD.PrintErr($"Node {node.Name} does not implement IPersistable.Load despite being in 'persist' group.");
            }
        }

    }
}
