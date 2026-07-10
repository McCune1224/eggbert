using Godot;

[GlobalClass]
public partial class SaveResource : Resource
{
    [Export]
    public SaveDataPlayer PlayerData { get; set; }

    [Export]
    public SaveDataWorldFlags WorldFlagsData { get; set; }

    [Export]
    public SaveDataInventory InventoryData { get; set; }

    [Export]
    public SaveDataEquipment EquipmentData { get; set; }

    public SaveResource() { }
}
