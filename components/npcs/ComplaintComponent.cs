using Godot;
/// <summary>
/// NPC sub-component that provides a complaint response when
/// the player checks/tattles on the NPC a certain number of times.
/// Retries after all templates are exhausted.
/// </summary>

[GlobalClass]
[Tool]
public partial class ComplaintComponent : Node
{
    /// <summary>Complaint lines, cycled in order. Each is shown with escalating "!" exaggeration per check.</summary>
    [ExportGroup("Complaints")]
    [Export] public string[] ComplaintTemplate { get; set; }
    /// <summary>Stable id used for the "complaint_count_&lt;id&gt;" WorldFlag. Defaults to the node name.</summary>
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
