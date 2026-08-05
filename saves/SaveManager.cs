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
    /// <summary>Directory for named dev save states (user://saves/&lt;slot&gt;.tres).</summary>
    private const string DevSavesDirectory = "user://saves/";
    /// <summary>Repo-committed, read-only fixture states (res://tests/savestates/&lt;slot&gt;.tres).</summary>
    private const string FixturesDirectory = "res://tests/savestates/";
    /// <summary>Slot used by the quick capture/load hotkeys (Ctrl+S / Ctrl+L).</summary>
    public const string QuickSlotName = "quick";

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

    // --- Slot path helpers ---

    /// <summary>
    /// Filesystem existence check for slot files. Deliberately NOT
    /// ResourceLoader.Exists: that consults the resource cache, so a path that
    /// was loaded once (e.g. a renamed-away slot) still reports "exists" after
    /// the file is deleted. FileAccess.FileExists is physical-truth.
    /// </summary>
    private static bool SlotFileExists(string path)
    {
        return FileAccess.FileExists(path);
    }

    /// <summary>
    /// Returns the on-disk path for a slot. Empty slot = the default player save.
    /// Named slots resolve to user://saves/&lt;slot&gt;.tres, falling back to a
    /// committed fixture under res://tests/savestates/ when no user copy exists.
    /// </summary>
    private string ResolveSlotPath(string slotName)
    {
        if (string.IsNullOrEmpty(slotName))
            return SaveFileName;

        string sanitized = SanitizeSlotName(slotName);
        string userPath = DevSavesDirectory + sanitized + ".tres";
        if (SlotFileExists(userPath))
            return userPath;

        string fixturePath = FixturesDirectory + sanitized + ".tres";
        if (SlotFileExists(fixturePath))
        {
            GameLogger.Debug("SaveManager", $"Slot '{slotName}' resolved to read-only fixture {fixturePath}");
            return fixturePath;
        }

        return userPath;
    }

    /// <summary>
    /// Write destination for a slot: always the user:// copy, never a committed
    /// fixture (fixtures are read-only; saving over one must create a user copy instead).
    /// </summary>
    private string WriteSlotPath(string slotName)
    {
        if (string.IsNullOrEmpty(slotName))
            return SaveFileName;
        return DevSavesDirectory + SanitizeSlotName(slotName) + ".tres";
    }

    /// <summary>True if the slot exists either as a user save or a committed fixture.</summary>
    public bool HasSave(string slotName = "")
    {
        return SlotFileExists(ResolveSlotPath(slotName));
    }

    /// <summary>
    /// Deletes a slot. The default player save is removed from user://.
    /// Named slots are removed from user://saves/; committed fixtures
    /// (res://tests/savestates/) are read-only and are never deleted.
    /// </summary>
    public void DeleteSave(string slotName = "")
    {
        if (string.IsNullOrEmpty(slotName))
        {
            var dir = DirAccess.Open("user://");
            if (dir != null && dir.FileExists("savegame.tres"))
            {
                dir.Remove("savegame.tres");
                GameLogger.Info("SaveManager", "Save file deleted.");
            }
            return;
        }

        string sanitized = SanitizeSlotName(slotName);
        string userPath = DevSavesDirectory + sanitized + ".tres";
        var savesDir = DirAccess.Open("user://saves/");
        if (savesDir != null && savesDir.FileExists(sanitized + ".tres"))
        {
            savesDir.Remove(sanitized + ".tres");
            GameLogger.Info("SaveManager", $"Dev save state '{slotName}' deleted.");
        }
        else if (SlotFileExists(FixturesDirectory + sanitized + ".tres"))
        {
            GameLogger.Warn("SaveManager", $"Slot '{slotName}' is a committed fixture (read-only) — not deleted.");
        }
        else
        {
            GameLogger.Warn("SaveManager", $"DeleteSave: slot '{slotName}' not found.");
        }
    }

    /// <summary>Names of all dev save states in user://saves/ (sorted). Fixtures are not listed here.</summary>
    public System.Collections.Generic.List<string> ListSlots()
    {
        var slots = new System.Collections.Generic.List<string>();
        var dir = DirAccess.Open(DevSavesDirectory);
        if (dir == null)
            return slots;

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (dir.CurrentIsDir())
                continue;
            if (fileName.EndsWith(".tres"))
                slots.Add(fileName.GetBaseName());
        }
        dir.ListDirEnd();
        slots.Sort();
        return slots;
    }

    /// <summary>Names of committed fixture states under res://tests/savestates/ (sorted, read-only).</summary>
    public System.Collections.Generic.List<string> ListFixtures()
    {
        var fixtures = new System.Collections.Generic.List<string>();
        var dir = DirAccess.Open(FixturesDirectory);
        if (dir == null)
            return fixtures;

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (dir.CurrentIsDir())
                continue;
            if (fileName.EndsWith(".tres"))
                fixtures.Add(fileName.GetBaseName());
        }
        dir.ListDirEnd();
        fixtures.Sort();
        return fixtures;
    }

    /// <summary>
    /// Renames a dev save state in user://saves/ (user slots only — committed
    /// fixtures are read-only). Fails if the target name already exists.
    /// </summary>
    public bool RenameSlot(string fromSlot, string toSlot)
    {
        string fromSan = SanitizeSlotName(fromSlot);
        string toSan = SanitizeSlotName(toSlot);
        if (fromSan == toSan)
            return true;

        var dir = DirAccess.Open(DevSavesDirectory);
        if (dir == null || !dir.FileExists(fromSan + ".tres"))
        {
            GameLogger.Warn("SaveManager", $"RenameSlot: '{fromSlot}' not found in user://saves/ (fixtures are read-only).");
            return false;
        }
        if (SlotFileExists(DevSavesDirectory + toSan + ".tres"))
        {
            GameLogger.Warn("SaveManager", $"RenameSlot: target '{toSlot}' already exists.");
            return false;
        }

        var res = ResourceLoader.Load(DevSavesDirectory + fromSan + ".tres");
        if (res is not SaveFile)
        {
            GameLogger.Error("SaveManager", $"RenameSlot: '{fromSlot}' is corrupt — cannot rename.");
            return false;
        }

        ResourceSaver.Save(res, DevSavesDirectory + toSan + ".tres");
        dir.Remove(fromSan + ".tres");
        GameLogger.Info("SaveManager", $"RenameSlot: '{fromSlot}' → '{toSlot}'");
        return true;
    }

    /// <summary>
    /// Sanitizes a user-supplied slot name for safe use as a filename:
    /// alphanumerics, '-', '_' kept; everything else stripped; trimmed and capped.
    /// </summary>
    public static string SanitizeSlotName(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName))
            return "unnamed";

        var sb = new System.Text.StringBuilder();
        foreach (char c in slotName.Trim())
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                sb.Append(c);
            else
                sb.Append('_');
        }
        string sanitized = sb.ToString();
        if (string.IsNullOrEmpty(sanitized))
            return "unnamed";
        return sanitized.Length > 40 ? sanitized.Substring(0, 40) : sanitized;
    }

    // --- Save ---

    /// <summary>
    /// Saves the game state to a slot, storing the save point location for respawn/continue.
    /// Called by SavePoint.OnInteract() with the default slot; dev states pass a slotName.
    /// </summary>
    public void SaveGame(string scenePath, Vector2 position, string locationName, string slotName = "")
    {
        string targetPath = WriteSlotPath(slotName);
        GameLogger.Info("SaveManager", $"Saving game at {locationName} ({scenePath}) → slot '{(string.IsNullOrEmpty(slotName) ? "default" : slotName)}'");

        // Ensure the dev saves directory exists for named slots.
        if (!string.IsNullOrEmpty(slotName))
        {
            DirAccess.MakeDirRecursiveAbsolute(DevSavesDirectory);
        }

        SaveFile saveFile = new()
        {
            SchemaVersion = SaveFile.CurrentSchemaVersion,
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
        GameLogger.Info("SaveManager", $"Saved {savedCount} ISavable components to '{targetPath}'");

        ResourceSaver.Save(saveFile, targetPath);
        GameLogger.Info("SaveManager", $"Save file written to '{targetPath}'");
        EmitSignal(SignalName.SaveCompleted);
    }

    // --- Load ---

    /// <summary>
    /// Loads a saved game state from a slot.
    /// Deserialization runs by priority (Player=10 first, then Equipment=5, Inventory=0, WorldFlags=0).
    /// If any component's data is corrupt, that system logs an error independently and continues.
    /// </summary>
    /// <param name="slotName">Empty = default player save; otherwise a named dev state or fixture.</param>
    /// <returns>True if save was loaded and a level was switched; false if no save file exists or save is invalid.</returns>
    public bool LoadGame(string slotName = "")
    {
        string targetPath = ResolveSlotPath(slotName);
        GameLogger.Info("SaveManager", $"LoadGame called (slot '{(string.IsNullOrEmpty(slotName) ? "default" : slotName)}', path {targetPath}).");
        if (!SlotFileExists(targetPath))
        {
            GameLogger.Error("SaveManager", $"No save file found for slot '{slotName}' (file check failed).");
            return false;
        }

        GameLogger.Info("SaveManager", "Save file exists on disk. Loading...");
        var loadedResource = ResourceLoader.Load(targetPath);
        GameLogger.Info("SaveManager", $"ResourceLoader.Load returned: type={loadedResource?.GetType().Name}, is null? {loadedResource == null}");

        if (loadedResource is not SaveFile saveFile)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                GameLogger.Warn("SaveManager", $"Save file is old/corrupt format ({loadedResource?.GetType().Name}). Deleting and starting fresh.");
                DeleteSave();
                GameLogger.Info("SaveManager", "Old save deleted. Returning (no level load).");
            }
            else
            {
                // Dev states are precious — never delete on load failure. Report and keep.
                GameLogger.Error("SaveManager", $"Dev save state '{slotName}' is corrupt/old format ({loadedResource?.GetType().Name}) — kept on disk, NOT loaded.");
            }
            return false;
        }

        if (saveFile.SchemaVersion > SaveFile.CurrentSchemaVersion)
        {
            GameLogger.Error("SaveManager", $"Slot '{slotName}' has SchemaVersion {saveFile.SchemaVersion} (supported: {SaveFile.CurrentSchemaVersion}) — created by a newer build. Kept on disk, NOT loaded.");
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
