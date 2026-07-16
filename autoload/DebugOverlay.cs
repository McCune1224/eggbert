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

    private StringBuilder _sb = new();
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
        if (!_visible)
            return;

        _sb.Clear();

        // FPS / perf
        var perf = Performance.GetMonitor(Performance.Monitor.TimeFps);
        _sb.AppendLine($"FPS: {perf}  Nodes: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}");

        // Player
        var player = Player.Instance;
        if (player != null)
        {
            _sb.AppendLine($"Player pos: {player.Position.Round()}  vel: {player.Velocity.Round()}");
            _sb.AppendLine($"Player interaction locked: {player.InInteraction}  mask: {player.CollisionMask}");
        }

        // Level
        var level = GameController.Instance?.CurrentLevel;
        if (level != null)
        {
            string name = level.Name;
            string path = level.SceneFilePath;
            if (!string.IsNullOrEmpty(path))
                name = path.GetFile().GetBaseName();
            _sb.AppendLine($"Level: {name}  children: {level.GetChildCount()}");
        }

        // Cutscene
        _sb.AppendLine($"Cutscene playing: {CutsceneController.Instance?.IsPlaying}");

        // Dialog
        _sb.AppendLine($"Dialog active: {DialogManager.Instance?.IsDialogActive}");

        // Audio
        var audio = AudioManager.Instance;
        if (audio != null)
        {
            float musicDb = AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("MUSIC"));
            float sfxDb = AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX"));
            _sb.AppendLine($"Audio: music={musicDb:F1}dB  sfx={sfxDb:F1}dB");
        }

        // WorldFlags (compact — just flag count + names)
        var wf = WorldFlags.Instance;
        if (wf != null)
        {
            var allFlags = wf.GetAllFlags();
            _sb.AppendLine($"WorldFlags ({allFlags.Count}):");
            foreach (var kvp in allFlags)
                _sb.AppendLine($"  {kvp.Key} = {kvp.Value}");
        }

        _label.Text = _sb.ToString();
    }
}
