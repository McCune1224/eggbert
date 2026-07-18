using Godot;

/// <summary>
/// An NPC that starts asleep. On interact, wakes with grumpy dialog.
/// Uses InteractableArea base class for player detection + prompt.
/// </summary>
[GlobalClass]
[Tool]
public partial class SleepingNPC : InteractableArea
{
    [ExportGroup("SleepingNPC")]
    [Export] public string[] WakeLines { get; set; }
    [Export] public string[] AwakeLines { get; set; }
    [Export] public string NpcId { get; set; } = "";

    private AnimatedSprite2D _zzzSprite;
    private bool _isAwake = false;

    public override void _Ready()
    {
        base._Ready();

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
