using Godot;

[GlobalClass]
[Tool]
public partial class ComplaintComponent : Node
{
    [Export] public string[] ComplaintTemplate { get; set; }
    [Export] public string NpcId { get; set; } = "";

    public string GetComplaint()
    {
        if (string.IsNullOrEmpty(NpcId)) NpcId = Name;

        int count = WorldFlags.Instance.GetFlag($"complaint_count_{NpcId}", 0).AsInt32();
        WorldFlags.Instance.SetFlag($"complaint_count_{NpcId}", count + 1);

        if (ComplaintTemplate == null || ComplaintTemplate.Length == 0)
        {
            GameLogger.Debug("ComplaintComponent", $"'{NpcId}': no templates — returning '...'");
            return "...";
        }

        string baseComplaint = ComplaintTemplate[count % ComplaintTemplate.Length];

        // Exaggerate: multiply by count
        string exaggeration = new string('!', System.Math.Min(count + 1, 10));

        GameLogger.Debug("ComplaintComponent", $"'{NpcId}': complaint #{count} — '{baseComplaint}'");
        return $"{baseComplaint}{exaggeration}";
    }
}
