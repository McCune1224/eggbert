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

        // NOTE: Collect and sort all nodes with group persist by load priority
        // Any node within this group that implements ISavable will be saved...or at least it should be.
        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is ISavable savable)
            {
                savable.Save(newSave);
            }
            else
            {
                GD.PrintErr($"Node {node.Name} does not implement IPersistable.Save despite being in 'persist' group.");
            }
        }


        ResourceSaver.Save(newSave, SavePath);
    }

    public void LoadGame()
    {
        GD.Print("Loading game...");
        if (!ResourceLoader.Exists(SavePath))
            return;

        var loadedResource = ResourceLoader.Load(SavePath);
        var saveData = loadedResource as SaveResource;
        if (saveData == null)
        {
            GD.PrintErr($"Failed to load SaveGameData from {SavePath}. Resource type: {loadedResource?.GetType().Name}");
            return;
        }

        var persistantNodes = new System.Collections.Generic.List<ISavable>();

        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is ISavable persistable)
            {
                persistantNodes.Add(persistable);
            }
            else
            {
                GD.PrintErr($"Node {node.Name} does not implement IPersistable.Load despite being in 'persist' group.");
            }
        }
        persistantNodes.Sort((a, b) => a.GetLoadPriority().CompareTo(b.GetLoadPriority()));
        persistantNodes.ForEach(persistable =>
        {
            persistable.Load(saveData);
        });

    }
}
