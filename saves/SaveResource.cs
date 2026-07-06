using Godot;

[GlobalClass]
public partial class SaveResource : Resource
{
    [Export]
    public SaveDataPlayer PlayerData { get; set; }

    [Export]
    public SaveDataWorldFlags WorldFlagsData { get; set; }

    public SaveResource() { }
}
