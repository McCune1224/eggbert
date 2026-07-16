using Godot;



public partial class SaveLoadManager : Node
{
    [Signal]
    public delegate void SaveCompletedEventHandler();

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


    public bool HasSave()
    {
        return ResourceLoader.Exists(SavePath);
    }

    public void SaveGame()
    {
        GameLogger.Info("SaveLoad", "Saving game...");
        SaveResource newSave = new();

        // Collect all nodes in the 'persist' group that implement ISavable
        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is ISavable savable)
            {
                savable.Save(newSave);
            }
            else
            {
                GameLogger.Error("SaveLoad", $"Node {node.Name} is in 'persist' group but does not implement ISavable.");
            }
        }

        ResourceSaver.Save(newSave, SavePath);
        EmitSignal(SignalName.SaveCompleted);
    }

    public void LoadGame()
    {
        GameLogger.Info("SaveLoad", "Loading game...");
        if (!ResourceLoader.Exists(SavePath))
            return;

        var loadedResource = ResourceLoader.Load(SavePath);
        var saveData = loadedResource as SaveResource;
        if (saveData == null)
        {
            GameLogger.Error("SaveLoad", $"Failed to load SaveResource from {SavePath}. Resource type: {loadedResource?.GetType().Name}");
            return;
        }

        var persistentNodes = new System.Collections.Generic.List<ISavable>();

        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is ISavable persistable)
            {
                persistentNodes.Add(persistable);
            }
            else
            {
                GameLogger.Error("SaveLoad", $"Node {node.Name} is in 'persist' group but does not implement ISavable.");
            }
        }
        persistentNodes.Sort((a, b) => a.GetLoadPriority().CompareTo(b.GetLoadPriority()));
        persistentNodes.ForEach(persistable =>
        {
            persistable.Load(saveData);
        });

    }
}
