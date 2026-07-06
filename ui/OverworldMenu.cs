using Godot;

public partial class OverworldMenu : CanvasLayer
{
    private const string SettingsPath = "user://settings.cfg";
    private static AudioStream _confirmSfx = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");

    private AnimationPlayer _animationPlayer;

    // Main menu
    private PanelContainer _mainPanel;
    private Button _resumeButton;
    private Button _mapButton;
    private Button _inventoryButton;
    private Button _saveButton;
    private Button _settingsButton;
    private Button _quitButton;

    // Settings panel
    private PanelContainer _settingsPanel;
    private HSlider _musicSlider;
    private HSlider _sfxSlider;
    private CheckButton _fullscreenCheck;
    private OptionButton _scaleOption;
    private Button _settingsBackButton;

    // Map panel
    private PanelContainer _mapPanel;
    private TextureRect _mapTexture;
    private VBoxContainer _warpList;
    private Button _mapBackButton;

    private enum Panel { Main, Settings, Map }
    private Panel _currentPanel = Panel.Main;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // Main menu
        _mainPanel = GetNode<PanelContainer>("MainPanel");
        _resumeButton = GetNode<Button>("MainPanel/VBoxContainer/ResumeButton");
        _mapButton = GetNode<Button>("MainPanel/VBoxContainer/MapButton");
        _inventoryButton = GetNode<Button>("MainPanel/VBoxContainer/InventoryButton");
        _saveButton = GetNode<Button>("MainPanel/VBoxContainer/SaveButton");
        _settingsButton = GetNode<Button>("MainPanel/VBoxContainer/SettingsButton");
        _quitButton = GetNode<Button>("MainPanel/VBoxContainer/QuitButton");

        _resumeButton.Connect("pressed", new Callable(this, nameof(OnResumePressed)));
        _mapButton.Connect("pressed", new Callable(this, nameof(OnMapPressed)));
        _inventoryButton.Connect("pressed", new Callable(this, nameof(OnInventoryPressed)));
        _saveButton.Connect("pressed", new Callable(this, nameof(OnSavePressed)));
        _settingsButton.Connect("pressed", new Callable(this, nameof(OnSettingsPressed)));
        _quitButton.Connect("pressed", new Callable(this, nameof(OnQuitPressed)));

        // Settings panel
        _settingsPanel = GetNode<PanelContainer>("SettingsPanel");
        _musicSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/MusicSlider");
        _sfxSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/SfxSlider");
        _fullscreenCheck = GetNode<CheckButton>("SettingsPanel/VBoxContainer/FullscreenBox/FullscreenCheck");
        _scaleOption = GetNode<OptionButton>("SettingsPanel/VBoxContainer/ScaleBox/ScaleOption");
        _settingsBackButton = GetNode<Button>("SettingsPanel/VBoxContainer/BackButton");

        _scaleOption.AddItem("1x", 1);
        _scaleOption.AddItem("2x", 2);
        _scaleOption.AddItem("3x", 3);
        _scaleOption.AddItem("4x", 4);
        _scaleOption.Selected = 1;

        _musicSlider.Connect("value_changed", new Callable(this, nameof(OnMusicVolumeChanged)));
        _sfxSlider.Connect("value_changed", new Callable(this, nameof(OnSfxVolumeChanged)));
        _fullscreenCheck.Connect("toggled", new Callable(this, nameof(OnFullscreenToggled)));
        _scaleOption.Connect("item_selected", new Callable(this, nameof(OnScaleChanged)));
        _settingsBackButton.Connect("pressed", new Callable(this, nameof(OnSettingsBackPressed)));

        // Map panel
        _mapPanel = GetNode<PanelContainer>("MapPanel");
        _mapTexture = GetNode<TextureRect>("MapPanel/VBoxContainer/MapTexture");
        _warpList = GetNode<VBoxContainer>("MapPanel/VBoxContainer/WarpList");
        _mapBackButton = GetNode<Button>("MapPanel/VBoxContainer/MapBackButton");
        _mapBackButton.Connect("pressed", new Callable(this, nameof(OnMapBackPressed)));

        LoadSettings();
        HideMenu();
    }

    // --- Panel navigation ---

    private void ShowPanel(Panel panel)
    {
        _mainPanel.Visible = panel == Panel.Main;
        _settingsPanel.Visible = panel == Panel.Settings;
        _mapPanel.Visible = panel == Panel.Map;
        _currentPanel = panel;
    }

    // --- Menu lifecycle ---

    private void Resume()
    {
        GetTree().Paused = false;
        HideMenu();
    }

    private void Pause()
    {
        GetTree().Paused = true;
        ShowMenu();
    }

    private void ShowMenu()
    {
        Visible = true;
        ShowPanel(Panel.Main);
        if (_animationPlayer != null && _animationPlayer.HasAnimation("show_menu"))
            _animationPlayer.Play("show_menu");
        _resumeButton.GrabFocus();
    }

    private void HideMenu()
    {
        if (_animationPlayer != null && _animationPlayer.HasAnimation("hide_menu"))
            _animationPlayer.Play("hide_menu");
        else
            Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("menu_pause"))
        {
            if (_currentPanel == Panel.Settings)
                OnSettingsBackPressed();
            else if (_currentPanel == Panel.Map)
                OnMapBackPressed();
            else
                ToggleEscape();
        }
    }

    private void ToggleEscape()
    {
        if (GetTree().Paused)
            Resume();
        else
            Pause();
    }

    // --- Main menu button handlers ---

    private void OnResumePressed() => Resume();

    private void OnMapPressed()
    {
        AudioManager.Instance.PlaySfx(_confirmSfx);
        RefreshWarpList();
        ShowPanel(Panel.Map);
        _mapBackButton.GrabFocus();
    }

    private void OnMapBackPressed()
    {
        ShowPanel(Panel.Main);
        _mapButton.GrabFocus();
    }

    private void RefreshWarpList()
    {
        // Clear old buttons
        foreach (Node child in _warpList.GetChildren())
            child.QueueFree();

        var unlocked = WarpDatabase.GetUnlocked();
        if (unlocked.Count == 0)
        {
            var lbl = new Label { Text = "No warps discovered" };
            lbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            _warpList.AddChild(lbl);
            return;
        }

        foreach (var warp in unlocked)
        {
            var btn = new Button { Text = warp.Name };
            string levelPath = warp.LevelPath;
            Vector2 pos = warp.Position;
            btn.Pressed += () => WarpTo(levelPath, pos);
            _warpList.AddChild(btn);
        }
    }

    private void WarpTo(string levelPath, Vector2 position)
    {
        Resume();
        GameController.Instance.LoadLevel(levelPath, position);
    }

    private void OnInventoryPressed()
    {
        AudioManager.Instance.PlaySfx(_confirmSfx);
    }

    private void OnSavePressed()
    {
        AudioManager.Instance.PlaySfx(_confirmSfx);
        SaveLoadManager.Instance.SaveGame();
    }

    private void OnSettingsPressed()
    {
        AudioManager.Instance.PlaySfx(_confirmSfx);
        ShowPanel(Panel.Settings);
        _musicSlider.GrabFocus();
    }

    private void OnQuitPressed()
    {
        AudioManager.Instance.PlaySfx(_confirmSfx);
        GetTree().Quit();
    }

    // --- Settings ---

    private void OnMusicVolumeChanged(double value)
    {
        float db = (float)(value / 100.0 * 40.0 - 40.0);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("MUSIC"), db);
    }

    private void OnSfxVolumeChanged(double value)
    {
        float db = (float)(value / 100.0 * 40.0 - 40.0);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX "), db);
    }

    private void OnFullscreenToggled(bool pressed)
    {
        GetWindow().Mode = pressed ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
    }

    private void OnScaleChanged(long index)
    {
        int scale = (int)_scaleOption.GetItemId((int)index);
        var size = new Vector2I(640 * scale, 360 * scale);
        DisplayServer.WindowSetSize(size);
        // ponytail: center on screen after resize
        var screenSize = DisplayServer.ScreenGetSize();
        DisplayServer.WindowSetPosition(new Vector2I(
            (screenSize.X - size.X) / 2,
            (screenSize.Y - size.Y) / 2
        ));
    }

    private void OnQuitDesktopPressed()
    {
        SaveSettings();
        GetTree().Quit();
    }

    private void OnSettingsBackPressed()
    {
        SaveSettings();
        ShowPanel(Panel.Main);
        _resumeButton.GrabFocus();
    }

    // --- Persistence ---

    private void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
            return;

        double musicVol = (double)config.GetValue("audio", "music_volume", 100.0);
        double sfxVol = (double)config.GetValue("audio", "sfx_volume", 100.0);
        bool fullscreen = (bool)config.GetValue("display", "fullscreen", false);
        int scale = (int)config.GetValue("display", "window_scale", 1);

        _musicSlider.Value = musicVol;
        _sfxSlider.Value = sfxVol;
        _fullscreenCheck.ButtonPressed = fullscreen;
        // ponytail: find closest matching scale option
        for (int i = 0; i < _scaleOption.ItemCount; i++)
        {
            if ((int)_scaleOption.GetItemId(i) == scale)
            {
                _scaleOption.Selected = i;
                break;
            }
        }

        // Apply without re-triggering callbacks
        OnMusicVolumeChanged(musicVol);
        OnSfxVolumeChanged(sfxVol);
        if (fullscreen) OnFullscreenToggled(true);
        OnScaleChanged(_scaleOption.Selected);
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("audio", "music_volume", _musicSlider.Value);
        config.SetValue("audio", "sfx_volume", _sfxSlider.Value);
        config.SetValue("display", "fullscreen", _fullscreenCheck.ButtonPressed);
        config.SetValue("display", "window_scale", (int)_scaleOption.GetItemId(_scaleOption.Selected));
        config.Save(SettingsPath);
    }

    private void OnLoadPressed() { }
}
