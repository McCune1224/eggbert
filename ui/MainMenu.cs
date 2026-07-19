using Godot;
using System.Collections.Generic;

public partial class MainMenu : CanvasLayer
{
    private const string SettingsPath = "user://settings.cfg";

    private PanelContainer _menuPanel;
    private Button _newGameButton;
    private Button _continueButton;
    private Button _settingsButton;
    private Button _quitButton;

    // Settings sub-panel
    private PanelContainer _settingsPanel;
    private HSlider _musicSlider;
    private HSlider _sfxSlider;
    private CheckButton _fullscreenCheck;
    private OptionButton _scaleOption;
    private OptionButton _textSpeedOption;
    private Button _settingsBackButton;
    // Keybinding section
    private Dictionary<string, Button> _keybindButtons = new();
    private bool _awaitingRebind = false;
    private string _rebindingAction = null;

    private enum View { Menu, Settings }
    private View _currentView = View.Menu;

    public override void _Ready()
    {
        _menuPanel = GetNode<PanelContainer>("MenuPanel");
        _newGameButton = GetNode<Button>("MenuPanel/VBoxContainer/NewGameButton");
        _continueButton = GetNode<Button>("MenuPanel/VBoxContainer/ContinueButton");
        _settingsButton = GetNode<Button>("MenuPanel/VBoxContainer/SettingsButton");
        _quitButton = GetNode<Button>("MenuPanel/VBoxContainer/QuitButton");

        _settingsPanel = GetNode<PanelContainer>("SettingsPanel");
        _musicSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/MusicSlider");
        _sfxSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/SfxSlider");
        _fullscreenCheck = GetNode<CheckButton>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/FullscreenBox/FullscreenCheck");
        _scaleOption = GetNode<OptionButton>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/ScaleBox/ScaleOption");
        _settingsBackButton = GetNode<Button>("SettingsPanel/VBoxContainer/BackButton");
        // Create the text speed option picker before connecting signals (it's built
        // dynamically so the connection below doesn't null-ref).
        SetupTextSpeedOption();

        SetupKeybindSection();

        _newGameButton.Connect("pressed", new Callable(this, nameof(OnNewGamePressed)));
        _continueButton.Connect("pressed", new Callable(this, nameof(OnContinuePressed)));
        _settingsButton.Connect("pressed", new Callable(this, nameof(OnSettingsPressed)));
        _quitButton.Connect("pressed", new Callable(this, nameof(OnQuitPressed)));

        _musicSlider.Connect("value_changed", new Callable(this, nameof(OnMusicVolumeChanged)));
        _sfxSlider.Connect("value_changed", new Callable(this, nameof(OnSfxVolumeChanged)));
        _fullscreenCheck.Connect("toggled", new Callable(this, nameof(OnFullscreenToggled)));
        _scaleOption.Connect("item_selected", new Callable(this, nameof(OnScaleChanged)));
        _textSpeedOption.Connect("item_selected", new Callable(this, nameof(OnTextSpeedChanged)));
        _settingsBackButton.Connect("pressed", new Callable(this, nameof(OnSettingsBackPressed)));

        _scaleOption.AddItem("1x", 1);
        _scaleOption.AddItem("2x", 2);
        _scaleOption.AddItem("3x", 3);
        _scaleOption.AddItem("4x", 4);
        _scaleOption.Selected = 1;
        LoadSettings();
        UpdateContinueButton();
        GameLogger.Debug("MainMenu", "_Ready complete — Continue button state: " + SaveManager.Instance.HasSave());
    }

    public override void _Input(InputEvent @event)
    {
        // Rebinding capture — eat the event and apply the new key
        if (_awaitingRebind && @event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo)
        {
            Key key = keyEv.PhysicalKeycode;
            if (key != Key.None)
            {
                KeybindManager.RebindAction(_rebindingAction, key);
                _keybindButtons[_rebindingAction].Text = KeybindManager.GetCurrentKeyLabel(_rebindingAction);
            }
            else
            {
                _keybindButtons[_rebindingAction].Text = KeybindManager.GetCurrentKeyLabel(_rebindingAction);
            }
            _awaitingRebind = false;
            _rebindingAction = null;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("menu_pause"))
        {
            if (_currentView == View.Settings)
                OnSettingsBackPressed();
        }
    }

    private void ShowView(View view)
    {
        _menuPanel.Visible = view == View.Menu;
        _settingsPanel.Visible = view == View.Settings;
        _currentView = view;
    }

    private void UpdateContinueButton()
    {
        bool hasSave = SaveManager.Instance.HasSave();
        _continueButton.Disabled = !hasSave;
    }

    private async void OnNewGamePressed()
    {
        // Clean up old save data
        SaveManager.Instance.DeleteSave();

        WorldFlags.Instance.ClearAll();

        Visible = false;
        _newGameButton.Disabled = true;
        _continueButton.Disabled = true;
        _settingsButton.Disabled = true;
        _quitButton.Disabled = true;

        const string factoryOpeningPath = "res://levels/factory/maps/OpeningZone.tscn";
        GameController.Instance.LoadLevel(factoryOpeningPath, Vector2.Zero);
        await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);
        GameLogger.Info("MainMenu", "New game level loaded — hiding and freeing MainMenu overlay");
        QueueFree();
        GameLogger.Info("MainMenu", "New game started — save deleted, flags cleared, loading Overworld");
    }

    private async void OnContinuePressed()
    {
        if (!SaveManager.Instance.HasSave()) return;

        Visible = false;
        _newGameButton.Disabled = true;
        _continueButton.Disabled = true;
        _settingsButton.Disabled = true;
        _quitButton.Disabled = true;

        SaveManager.Instance.LoadGame();
        await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);
        QueueFree();
        GameLogger.Info("MainMenu", "Continue pressed — loading save game");
    }


    private void OnSettingsPressed()
    {
        ShowView(View.Settings);
        _musicSlider.GrabFocus();
    }

    private void OnSettingsBackPressed()
    {
        SaveSettings();
        ShowView(View.Menu);
        _settingsButton.GrabFocus();
    }

    private void OnQuitPressed()
    {
        GameLogger.Info("MainMenu", "Quit pressed");
        GetTree().Quit();
    }

    // --- Settings ---

    private void OnMusicVolumeChanged(double value)
    {
        float db = (float)(value / 100.0 * 40.0 - 40.0);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("MUSIC"), db);
        GameLogger.Debug("MainMenu", $"Music volume: {value:F0}% ({db:F1} dB)");
    }

    private void OnSfxVolumeChanged(double value)
    {
        float db = (float)(value / 100.0 * 40.0 - 40.0);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), db);
        GameLogger.Debug("MainMenu", $"SFX volume: {value:F0}% ({db:F1} dB)");
    }

    private void OnFullscreenToggled(bool pressed)
    {
        GameLogger.Debug("MainMenu", $"Fullscreen: {pressed}");
        GetWindow().Mode = pressed ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
    }

    private void OnScaleChanged(long index)
    {
        int scale = (int)_scaleOption.GetItemId((int)index);
        var size = new Vector2I(640 * scale, 360 * scale);
        DisplayServer.WindowSetSize(size);
        var screenSize = DisplayServer.ScreenGetSize();
        DisplayServer.WindowSetPosition(new Vector2I(
            (screenSize.X - size.X) / 2,
            (screenSize.Y - size.Y) / 2
        ));
        GameLogger.Debug("MainMenu", $"Scale changed: {scale}x ({size})");
    }

    private void OnTextSpeedChanged(long index)
    {
        DialogManager.CurrentTextSpeed = (DialogManager.TextSpeed)_textSpeedOption.GetItemId((int)index);
    }

    private void SetupTextSpeedOption()
    {
        var vbox = _settingsPanel.GetNode<VBoxContainer>("VBoxContainer/ScrollContainer/SettingsVBox");
        var hbox = new HBoxContainer { Name = "TextSpeedBox" };
        hbox.LayoutMode = 0;
        var label = new Label { Text = "Text Speed:", CustomMinimumSize = new Vector2(120, 0) };
        hbox.AddChild(label);
        _textSpeedOption = new OptionButton();
        _textSpeedOption.AddItem("Normal", (int)DialogManager.TextSpeed.Normal);
        _textSpeedOption.AddItem("Fast", (int)DialogManager.TextSpeed.Fast);
        _textSpeedOption.AddItem("Instant", (int)DialogManager.TextSpeed.Instant);
        _textSpeedOption.Selected = (int)DialogManager.TextSpeed.Fast;
        hbox.AddChild(_textSpeedOption);
        vbox.AddChild(hbox);
    }

    // --- Keybindings ---

    private void SetupKeybindSection()
    {
        // Fix the container chain to use Container layout so children get arranged.
        // The .tscn has these as LayoutMode.Fixed but we need ContainerSized
        // for ScrollContainer children to be properly arranged by their parents.
        var panelVBox = _settingsPanel.GetNode<VBoxContainer>("VBoxContainer");
        panelVBox.LayoutMode = 2;
        var scroll = _settingsPanel.GetNode<ScrollContainer>("VBoxContainer/ScrollContainer");
        scroll.LayoutMode = 2;
        var backBtn = _settingsPanel.GetNode<Button>("VBoxContainer/BackButton");
        backBtn.LayoutMode = 2;

        var vbox = _settingsPanel.GetNode<VBoxContainer>("VBoxContainer/ScrollContainer/SettingsVBox");
        vbox.LayoutMode = 2;

        var title = new Label { Text = "Controls:" };
        title.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(title);

        var grid = new GridContainer();
        grid.Columns = 2;
        grid.LayoutMode = 2;
        vbox.AddChild(grid);

        foreach (string action in KeybindManager.RebindableActions)
        {
            var label = new Label
            {
                Text = KeybindManager.GetActionDisplayName(action),
                CustomMinimumSize = new Vector2(120, 24),
            };
            label.LayoutMode = 2;
            label.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 1.0f));
            grid.AddChild(label);

            var btn = new Button
            {
                Text = KeybindManager.GetCurrentKeyLabel(action),
                CustomMinimumSize = new Vector2(80, 24),
            };
            btn.LayoutMode = 2;
            string captured = action;
            btn.Pressed += () => StartRebind(captured);
            _keybindButtons[action] = btn;
            grid.AddChild(btn);
        }

        var resetBtn = new Button { Text = "Reset Controls" };
        resetBtn.AddThemeFontSizeOverride("font_size", 14);
        resetBtn.Pressed += OnResetKeybindsPressed;
        vbox.AddChild(resetBtn);
    }

    private void StartRebind(string action)
    {
        // Restore previous button label if switching mid-rebind
        if (_awaitingRebind && _rebindingAction != null && _keybindButtons.ContainsKey(_rebindingAction))
            _keybindButtons[_rebindingAction].Text = KeybindManager.GetCurrentKeyLabel(_rebindingAction);

        _awaitingRebind = true;
        _rebindingAction = action;
        _keybindButtons[action].Text = "...";
    }

    private void OnResetKeybindsPressed()
    {
        KeybindManager.ResetAllBindings();
        foreach (var kv in _keybindButtons)
            kv.Value.Text = KeybindManager.GetCurrentKeyLabel(kv.Key);
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
        int textSpeed = (int)config.GetValue("display", "text_speed", (int)DialogManager.TextSpeed.Fast);

        _musicSlider.Value = musicVol;
        _sfxSlider.Value = sfxVol;
        _fullscreenCheck.ButtonPressed = fullscreen;
        for (int i = 0; i < _scaleOption.ItemCount; i++)
        {
            if ((int)_scaleOption.GetItemId(i) == scale)
            {
                _scaleOption.Selected = i;
                break;
            }
        }
        for (int i = 0; i < _textSpeedOption.ItemCount; i++)
        {
            if ((int)_textSpeedOption.GetItemId(i) == textSpeed)
            {
                _textSpeedOption.Selected = i;
                break;
            }
        }
        DialogManager.CurrentTextSpeed = (DialogManager.TextSpeed)textSpeed;

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
        config.SetValue("display", "text_speed", (int)_textSpeedOption.GetItemId(_textSpeedOption.Selected));
        KeybindManager.SaveBindings();
        config.Save(SettingsPath);
    }
}
