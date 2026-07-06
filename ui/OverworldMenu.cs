using Godot;

public partial class OverworldMenu : CanvasLayer
{
    private const string SettingsPath = "user://settings.cfg";

    private AnimationPlayer _animationPlayer;

    // Main menu
    private PanelContainer _mainPanel;
    private Button _resumeButton;
    private Button _inventoryButton;
    private Button _saveButton;
    private Button _settingsButton;
    private Button _quitButton;

    // Settings panel
    private PanelContainer _settingsPanel;
    private HSlider _musicSlider;
    private HSlider _sfxSlider;
    private Button _quitDesktopButton;
    private Button _settingsBackButton;

    private enum Panel { Main, Settings }
    private Panel _currentPanel = Panel.Main;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // Main menu
        _mainPanel = GetNode<PanelContainer>("MainPanel");
        _resumeButton = GetNode<Button>("MainPanel/VBoxContainer/ResumeButton");
        _inventoryButton = GetNode<Button>("MainPanel/VBoxContainer/InventoryButton");
        _saveButton = GetNode<Button>("MainPanel/VBoxContainer/SaveButton");
        _settingsButton = GetNode<Button>("MainPanel/VBoxContainer/SettingsButton");
        _quitButton = GetNode<Button>("MainPanel/VBoxContainer/QuitButton");

        _resumeButton.Connect("pressed", new Callable(this, nameof(OnResumePressed)));
        _inventoryButton.Connect("pressed", new Callable(this, nameof(OnInventoryPressed)));
        _saveButton.Connect("pressed", new Callable(this, nameof(OnSavePressed)));
        _settingsButton.Connect("pressed", new Callable(this, nameof(OnSettingsPressed)));
        _quitButton.Connect("pressed", new Callable(this, nameof(OnQuitPressed)));

        // Settings panel
        _settingsPanel = GetNode<PanelContainer>("SettingsPanel");
        _musicSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/MusicSlider");
        _sfxSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/SfxSlider");
        _quitDesktopButton = GetNode<Button>("SettingsPanel/VBoxContainer/QuitDesktopButton");
        _settingsBackButton = GetNode<Button>("SettingsPanel/VBoxContainer/BackButton");

        _musicSlider.Connect("value_changed", new Callable(this, nameof(OnMusicVolumeChanged)));
        _sfxSlider.Connect("value_changed", new Callable(this, nameof(OnSfxVolumeChanged)));
        _quitDesktopButton.Connect("pressed", new Callable(this, nameof(OnQuitDesktopPressed)));
        _settingsBackButton.Connect("pressed", new Callable(this, nameof(OnSettingsBackPressed)));

        LoadSettings();
        HideMenu();
    }

    // --- Panel navigation ---

    private void ShowPanel(Panel panel)
    {
        _mainPanel.Visible = panel == Panel.Main;
        _settingsPanel.Visible = panel == Panel.Settings;
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
        // In settings, Esc goes back to main menu instead of closing
        if (Input.IsActionJustPressed("menu_pause"))
        {
            if (_currentPanel == Panel.Settings)
                OnSettingsBackPressed();
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

    private void OnInventoryPressed()
    {
        // TODO: Items panel — Phase 3
    }

    private void OnSavePressed()
    {
        SaveLoadManager.Instance.SaveGame();
        // ponytail: no feedback beyond the save itself
    }

    private void OnSettingsPressed()
    {
        ShowPanel(Panel.Settings);
        _musicSlider.GrabFocus();
    }

    private void OnQuitPressed() => GetTree().Quit();

    // --- Settings ---

    private void OnMusicVolumeChanged(double value)
    {
        // ponytail: linear 0-100% mapped to -40dB to 0dB
        float db = (float)(value / 100.0 * 40.0 - 40.0);
        int busIdx = AudioServer.GetBusIndex("MUSIC");
        AudioServer.SetBusVolumeDb(busIdx, db);
    }

    private void OnSfxVolumeChanged(double value)
    {
        float db = (float)(value / 100.0 * 40.0 - 40.0);
        // ponytail: bus name has trailing space from original layout
        int busIdx = AudioServer.GetBusIndex("SFX ");
        AudioServer.SetBusVolumeDb(busIdx, db);
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
            return; // first launch, defaults are fine

        double musicVol = (double)config.GetValue("audio", "music_volume", 100.0);
        double sfxVol = (double)config.GetValue("audio", "sfx_volume", 100.0);

        _musicSlider.Value = musicVol;
        _sfxSlider.Value = sfxVol;

        // Apply without re-triggering the slider callbacks
        OnMusicVolumeChanged(musicVol);
        OnSfxVolumeChanged(sfxVol);
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("audio", "music_volume", _musicSlider.Value);
        config.SetValue("audio", "sfx_volume", _sfxSlider.Value);
        config.Save(SettingsPath);
    }

    // Backwards compat — old script had Load/Save game buttons wired here
    // Remove if nothing references them
    private void OnLoadPressed() { }
}
