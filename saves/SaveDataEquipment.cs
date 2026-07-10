using Godot;

[GlobalClass]
public partial class SaveDataEquipment : Resource
{
    [Export] public string WeaponId { get; set; } = "";
    [Export] public string ArmorId { get; set; } = "";
    [Export] public string AccessoryId { get; set; } = "";
}
