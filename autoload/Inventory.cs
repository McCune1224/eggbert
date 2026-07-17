using Godot;
using System.Collections.Generic;

/// <summary>
/// Player inventory autoload. Holds item counts by Id, split by category for UI.
/// ISavable — persisted via SaveManager. Overworld-only usage per DESIGN.md.
/// </summary>
public partial class Inventory : Node, ISavable
{
    private static Inventory _instance;
    public static Inventory Instance => _instance;

    // id → count. Key items and equipment are always 1; consumables stack.
    private System.Collections.Generic.Dictionary<string, int> _stacks = new();

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
        {
            GameLogger.Error("Inventory", "Multiple Inventory instances detected!");
            QueueFree();
            return;
        }
        AddToGroup("persist");
        // ponytail: seed test items every boot; Load() overwrites if a save exists, so this only sticks for new games.
        SeedTestItems();
    }

    // --- API ---

    public void Add(string id, int count = 1)
    {
        if (!ItemDatabase.All.ContainsKey(id))
        {
            GameLogger.Error("Inventory", $"Inventory.Add: unknown item id '{id}'");
            return;
        }
        _stacks[id] = _stacks.TryGetValue(id, out int c) ? c + count : count;
        GameLogger.Debug("Inventory", $"Add: {id} x{count} (total: {_stacks[id]})");
    }

    public bool Remove(string id, int count = 1)
    {
        if (!_stacks.TryGetValue(id, out int c) || c < count)
            return false;
        if (c == count) _stacks.Remove(id);
        else _stacks[id] = c - count;
        GameLogger.Debug("Inventory", $"Remove: {id} x{count}");
        return true;
    }
    public bool Has(string id) => _stacks.TryGetValue(id, out int c) && c > 0;
    public int GetCount(string id) => _stacks.TryGetValue(id, out int c) ? c : 0;

    /// <summary>All item ids in a category that the player currently holds.</summary>
    public List<string> GetByCategory(ItemCategory category)
    {
        var result = new List<string>();
        foreach (var kvp in _stacks)
        {
            if (kvp.Value <= 0) continue;
            Item item = ItemDatabase.Get(kvp.Key);
            if (item != null && item.Category == category)
                result.Add(kvp.Key);
        }
        return result;
    }

    // --- Seeding (test items for new game) ---

    public void SeedTestItems()
    {
        Add("rusty_key");
        Add("cell_key");
        Add("hardboiled_egg", 2);
        Add("scrambled_egg", 1);
        Add("eggshell_helm");
    }
    // --- ISavable ---

    public string SaveKey => "inventory";

    public Godot.Collections.Dictionary<string, Variant> Serialize()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var kvp in _stacks)
            dict[kvp.Key] = kvp.Value;

        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["stacks"] = dict
        };
    }

    public void Deserialize(Godot.Collections.Dictionary<string, Variant> data)
    {
        _stacks.Clear();
        if (!data.TryGetValue("stacks", out var stacksVar)) return;
        var stacks = stacksVar.AsGodotDictionary();
        foreach (var kvp in stacks)
            _stacks[kvp.Key.AsString()] = kvp.Value.AsInt32();
    }

    public int GetLoadPriority() => 0;
}