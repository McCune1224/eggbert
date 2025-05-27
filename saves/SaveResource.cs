using Godot;

[GlobalClass]
public partial class SaveResource : Resource
{
    [Export]
    public SaveDataPlayer PlayerData { get; set; }

    public SaveResource() { }

}
