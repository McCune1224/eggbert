using Godot;
using System.IO;

// One-off dev utility: authors the sample external item .tres files used to
// prove the ItemDatabase.LoadExternalItems() path (the Item Editor plugin uses
// the same ResourceSaver approach). Run:
//   godot --headless --path . --script res://tests/GenerateSampleItems.cs
// It creates res://resources/items/*.tres, then loads them back to confirm the
// round-trip. Re-running is safe (overwrites the same files).

public partial class GenerateSampleItems : SceneTree
{
    public override void _Initialize()
    {
        string dir = "res://resources/items";
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(dir));

        Create("factory_coffee", "Factory Coffee", ItemCategory.Consumable,
            "Bitter break-room coffee. Restores 15 HP.", 15,
            "res://assets/items/icons/icon_0011.png");
        Create("grease_can", "Grease Can", ItemCategory.Consumable,
            "A dented can of machine grease. Surprisingly nourishing. Restores 20 HP.", 20,
            "res://assets/items/icons/icon_0006.png");

        GD.Print("[gen-items] done");
        Quit(0);
    }

    private static void Create(string id, string display, ItemCategory cat, string desc, int heal, string icon)
    {
        var item = new Item
        {
            Id = id,
            DisplayName = display,
            Category = cat,
            Description = desc,
            HealAmount = heal,
            Icon = ResourceLoader.Load<Texture2D>(icon)
        };

        string path = $"res://resources/items/{id}.tres";
        Error err = ResourceSaver.Save(item, path);
        GD.Print($"[gen-items] save {path} -> {err}");

        // Round-trip check: confirm the .tres loads back as a valid Item.
        var loaded = ResourceLoader.Load<Item>(path);
        if (loaded == null)
            GD.PrintErr($"[gen-items] FAIL: could not reload {path}");
        else
            GD.Print($"[gen-items] reload {loaded.Id} / '{loaded.DisplayName}' / heal={loaded.HealAmount} OK");
    }
}
