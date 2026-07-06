using Godot;
using System.Text;

/// <summary>
/// F2-toggle debug overlay showing game state (WorldFlags, player, etc.).
/// Created in code — no scene file needed.
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    private static DebugOverlay _instance;
    private Label _label;
    private bool _visible = false;

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
            QueueFree();

        Layer = 128; // above everything

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Position = new Vector2(4, 4),
            Theme = new Theme()
        };
        _label.AddThemeFontSizeOverride("font_size", 10);
        _label.AddThemeColorOverride("font_color", Colors.White);
        _label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _label.AddThemeConstantOverride("outline_size", 1);
        AddChild(_label);

        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_toggle"))
        {
            _visible = !_visible;
            Visible = _visible;
        }
    }

    public override void _Process(double delta)
    {
        if (!_visible) return;

        var sb = new StringBuilder();

        // Player
        var player = Player.Instance;
        if (player != null)
        {
            sb.AppendLine($"Player pos: {player.Position.Round()}");
            sb.AppendLine($"Player interaction locked: {player.InInteraction}");
        }

        // Level
        if (GameController.Instance?.CurrentLevel != null)
            sb.AppendLine($"Level: {GameController.Instance.CurrentLevel.SceneFilePath}");

        // Cutscene
        sb.AppendLine($"Cutscene playing: {CutsceneController.Instance?.IsPlaying}");

        // Dialog
        sb.AppendLine($"Dialog active: {DialogManager.Instance?.IsDialogActive}");

        // WorldFlags
        var wf = WorldFlags.Instance;
        if (wf != null)
        {
            var saveRes = new SaveResource();
            wf.Save(saveRes);
            int count = saveRes.WorldFlagsData?.Flags?.Count ?? 0;
            sb.AppendLine($"WorldFlags ({count}):");
            if (saveRes.WorldFlagsData?.Flags != null)
                foreach (var kvp in saveRes.WorldFlagsData.Flags)
                    sb.AppendLine($"  {kvp.Key} = {kvp.Value}");
        }

        _label.Text = sb.ToString();
    }
}
