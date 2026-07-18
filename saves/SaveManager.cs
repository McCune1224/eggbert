using Godot;
using Godot.Collections;
using System.Linq;

public partial class SaveManager : Node
{
    [Signal]
    public delegate void SaveCompletedEventHandler();

    private static SaveManager _instance;
    public static SaveManager Instance => _instance;

    private const string SaveFileName = "user://savegame.tres";

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public bool HasSave()
    {
        return ResourceLoader.Exists(SaveFileName);
    }

    public void DeleteSave()
    {
        var dir = DirAccess.Open("user://");
        if (dir != null && dir.FileExists("savegame.tres"))
        {
            dir.Remove("savegame.tres");
            GameLogger.Info("SaveManager", "Save file deleted.");
        }
    }

    /// <summary>
    /// Saves the game state, storing the save point location for respawn/continue.
    /// Called by SavePoint.OnInteract().
    /// </summary>
    public void SaveGame(string scenePath, Vector2 position, string locationName)
    {
        GameLogger.Info("SaveManager", $"Saving game at {locationName} ({scenePath})");

        SaveFile saveFile = new()
        {
            SavePointScenePath = scenePath,
            SavePointPosition = position,
            LocationName = locationName,
            SaveTimestamp = Time.GetUnixTimeFromSystem(),
            PlayTimeSeconds = Time.GetTicksMsec() / 1000.0
        };

        // Collect data from all ISavable nodes in the "persist" group
        int savedCount = 0;
        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is ISavable savable)
            {
                saveFile.ComponentData[savable.SaveKey] = savable.Serialize();
                savedCount++;
            }
            else
            {
                GameLogger.Warn("SaveManager", $"Node '{node.Name}' in 'persist' group does not implement ISavable — skipping.");
            }
        }
        GameLogger.Info("SaveManager", $"Saved {savedCount} ISavable components to '{SaveFileName}'");

        ResourceSaver.Save(saveFile, SaveFileName);
        GameLogger.Info("SaveManager", $"Save file written to '{SaveFileName}'");
        EmitSignal(SignalName.SaveCompleted);
    }

    /// <summary>
    /// Loads the saved game state. Player loads first (priority 10) and triggers
    /// the level scene switch; then Equipment (5), Inventory (0), WorldFlags (0) restore.
    /// </summary>
    /// <returns>True if the save was loaded successfully and a level was loaded.</returns>
    public bool LoadGame()
    {
        GameLogger.Info("SaveManager", "LoadGame called.");
        if (!ResourceLoader.Exists(SaveFileName))
        {
            GameLogger.Error("SaveManager", "No save file found (ResourceLoader.Exists returned false).");
            return false;
        }

        GameLogger.Info("SaveManager", "Save file exists on disk. Loading...");
        var loadedResource = ResourceLoader.Load(SaveFileName);
        GameLogger.Info("SaveManager", $"ResourceLoader.Load returned: type={loadedResource?.GetType().Name}, is null? {loadedResource == null}");

        if (loadedResource is not SaveFile saveFile)
        {
            GameLogger.Warn("SaveManager", $"Save file is old/corrupt format ({loadedResource?.GetType().Name}). Deleting and starting fresh.");
            DeleteSave();
            GameLogger.Info("SaveManager", "Old save deleted. Returning (no level load).");
            return false;
        }

        GameLogger.Info("SaveManager", $"SaveFile loaded. Location={saveFile.LocationName}, ScenePath={saveFile.SavePointScenePath}, Pos={saveFile.SavePointPosition}, ComponentKeys={saveFile.ComponentData.Keys.Count}");

        // Collect and sort by load priority (descending)
        var persistNodes = new System.Collections.Generic.List<ISavable>();
        foreach (Node node in GetTree().GetNodesInGroup("persist"))
        {
            if (node is ISavable savable)
            {
                GameLogger.Debug("SaveManager", $"Found ISavable node: {node.Name} key={savable.SaveKey} priority={savable.GetLoadPriority()}");
                persistNodes.Add(savable);
            }
            else
            {
                GameLogger.Debug("SaveManager", $"Node '{node.Name}' in 'persist' group does not implement ISavable — skipping.");
            }
        }

        GameLogger.Info("SaveManager", $"Found {persistNodes.Count} ISavable nodes to deserialize.");
        persistNodes.Sort((a, b) => b.GetLoadPriority().CompareTo(a.GetLoadPriority()));

        int deserialized = 0;
        int expected = persistNodes.Count;
        foreach (var savable in persistNodes)
        {
            if (saveFile.ComponentData.TryGetValue(savable.SaveKey, out var data))
            {
                GameLogger.Info("SaveManager", $"Deserializing key='{savable.SaveKey}' (priority={savable.GetLoadPriority()})");
                savable.Deserialize(data);
                deserialized++;
            }
            else
            {
                GameLogger.Debug("SaveManager", $"No saved data for key '{savable.SaveKey}' — skipping.");
            }
        }
        GameLogger.Info("SaveManager", $"LoadGame complete: deserialized {deserialized}/{expected} components — VALID={deserialized == expected}");
        return true;
    }
}
