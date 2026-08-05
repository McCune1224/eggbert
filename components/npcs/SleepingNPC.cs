using Godot;

/// <summary>
/// An NPC that starts asleep. On interact, wakes with grumpy dialog.
/// Uses InteractableArea base class for player detection + prompt.
/// </summary>
[GlobalClass]
[Tool]
public partial class SleepingNPC : InteractableArea
{
    /// <summary>Grumpy lines spoken the first time the NPC is woken up.</summary>
    [Export] public string[] WakeLines { get; set; }
    /// <summary>Lines spoken on later interactions after the NPC is awake.</summary>
    [Export] public string[] AwakeLines { get; set; }
    /// <summary>Stable id for the "woke_&lt;id&gt;" WorldFlag. Defaults to the node name when empty.</summary>
    [Export] public string NpcId { get; set; } = "";

    private AnimatedSprite2D _zzzSprite;
    private bool _isAwake = false;

    public override void _Ready()
    {
        base._Ready();

        if (Engine.IsEditorHint())
            return;

        if (string.IsNullOrEmpty(NpcId))
            NpcId = Name;

        _isAwake = WorldFlags.Instance.HasFlag("woke_" + NpcId);

        _zzzSprite = GetNodeOrNull<AnimatedSprite2D>("Zzz");
        if (_zzzSprite != null)
            _zzzSprite.Visible = !_isAwake;

        GameLogger.Debug("SleepingNPC", $"'{Name}': _Ready — awake={_isAwake}, id='{NpcId}'");
    }

    protected override void OnInteract()
    {
        if (!_isAwake)
        {
            _isAwake = true;
            WorldFlags.Instance.SetFlag("woke_" + NpcId, true);

            if (_zzzSprite != null)
                _zzzSprite.Visible = false;

            GameLogger.Info("SleepingNPC", $"'{Name}': woken up! (flag='woke_{NpcId}')");

            if (WakeLines != null && WakeLines.Length > 0)
            {
                ShowDialog(WakeLines);
                return;
            }
        }

        ShowDialog(AwakeLines);
    }
}
