using Godot;
using Godot.Collections;
/// <summary>
/// NPC sub-component that can share a rumor text to the player.
/// Each rumor is identified by an index and retrieved by index
/// from the parent NPC's rumor array.
/// </summary>

[GlobalClass]
[Tool]
public partial class RumorComponent : Node
{
    [Export] public Array<string> Rumors { get; set; } = new();
    [Export] public string NpcId { get; set; } = "";

    public string GetNextRumor()
    {
        if (string.IsNullOrEmpty(NpcId)) NpcId = Name;

        int index = WorldFlags.Instance.GetFlag($"rumor_index_{NpcId}", 0).AsInt32();

        if (Rumors == null || Rumors.Count == 0)
        {
            GameLogger.Debug("RumorComponent", $"'{NpcId}': no rumors — returning '...'");
            return "...";
        }

        string rumor = Rumors[index % Rumors.Count];
        WorldFlags.Instance.SetFlag($"rumor_index_{NpcId}", index + 1);

        GameLogger.Debug("RumorComponent", $"'{NpcId}': rumor #{index} — '{rumor}'");
        return rumor;
    }
}
