using Godot;

public partial class OverworldMenu : CanvasLayer
{
    private const string SettingsPath = "user://settings.cfg";
    private static AudioStream _confirmSfx = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.ogg");

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
    private OptionButton _textSpeedOption;
    private Button _settingsBackButton;

    // Map panel
    private PanelContainer _mapPanel;
    private TextureRect _mapTexture;
    private VBoxContainer _warpList;
    private Button _mapBackButton;

    // Inventory panel
    private PanelContainer _inventoryPanel;
    private Button _keyTab;
    private Button _consumableTab;
    private Button _equipmentTab;
    private ItemList _itemList;
    private Label _descriptionLabel;
    private Button _useButton;
    private Button _inventoryBackButton;
    private ItemCategory _currentTab = ItemCategory.Key;
    private string _selectedItemId;

    private enum Panel { Main, Settings, Map, Inventory }
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

        SetupTextSpeedOption();

        _musicSlider.Connect("value_changed", new Callable(this, nameof(OnMusicVolumeChanged)));
        _sfxSlider.Connect("value_changed", new Callable(this, nameof(OnSfxVolumeChanged)));
        _fullscreenCheck.Connect("toggled", new Callable(this, nameof(OnFullscreenToggled)));
        _scaleOption.Connect("item_selected", new Callable(this, nameof(OnScaleChanged)));
        _textSpeedOption.Connect("item_selected", new Callable(this, nameof(OnTextSpeedChanged)));
        _settingsBackButton.Connect("pressed", new Callable(this, nameof(OnSettingsBackPressed)));

        // Map panel
        _mapPanel = GetNode<PanelContainer>("MapPanel");
        _mapTexture = GetNode<TextureRect>("MapPanel/VBoxContainer/MapTexture");
        _warpList = GetNode<VBoxContainer>("MapPanel/VBoxContainer/WarpList");
        _mapBackButton = GetNode<Button>("MapPanel/VBoxContainer/MapBackButton");
        _mapBackButton.Connect("pressed", new Callable(this, nameof(OnMapBackPressed)));

        // Inventory panel
        _inventoryPanel = GetNode<PanelContainer>("InventoryPanel");
        _keyTab = GetNode<Button>("InventoryPanel/VBoxContainer/TabBox/KeyTab");
        _consumableTab = GetNode<Button>("InventoryPanel/VBoxContainer/TabBox/ConsumableTab");
        _equipmentTab = GetNode<Button>("InventoryPanel/VBoxContainer/TabBox/EquipmentTab");
        _itemList = GetNode<ItemList>("InventoryPanel/VBoxContainer/ItemList");
        _descriptionLabel = GetNode<Label>("InventoryPanel/VBoxContainer/DescriptionLabel");
        _useButton = GetNode<Button>("InventoryPanel/VBoxContainer/ButtonRow/UseButton");
        _inventoryBackButton = GetNode<Button>("InventoryPanel/VBoxContainer/ButtonRow/InventoryBackButton");

        _keyTab.Connect("pressed", new Callable(this, nameof(OnKeyTabPressed)));
        _consumableTab.Connect("pressed", new Callable(this, nameof(OnConsumableTabPressed)));
        _equipmentTab.Connect("pressed", new Callable(this, nameof(OnEquipmentTabPressed)));
        _itemList.Connect("item_selected", new Callable(this, nameof(OnItemSelected)));
        _useButton.Connect("pressed", new Callable(this, nameof(OnUsePressed)));
        _inventoryBackButton.Connect("pressed", new Callable(this, nameof(OnInventoryBackPressed)));

        LoadSettings();
        HideMenu();
    }

    // --- Panel navigation ---

    private void ShowPanel(Panel panel)
    {
        _mainPanel.Visible = panel == Panel.Main;
        _settingsPanel.Visible = panel == Panel.Settings;
        _mapPanel.Visible = panel == Panel.Map;
        _inventoryPanel.Visible = panel == Panel.Inventory;
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
            else if (_currentPanel == Panel.Inventory)
                OnInventoryBackPressed();
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
        _currentTab = ItemCategory.Key;
        RefreshInventory();
        ShowPanel(Panel.Inventory);
        _keyTab.GrabFocus();
    }

    private void OnInventoryBackPressed()
    {
        ShowPanel(Panel.Main);
        _inventoryButton.GrabFocus();
    }

    private void OnKeyTabPressed() { _currentTab = ItemCategory.Key; RefreshInventory(); }
    private void OnConsumableTabPressed() { _currentTab = ItemCategory.Consumable; RefreshInventory(); }
    private void OnEquipmentTabPressed() { _currentTab = ItemCategory.Equipment; RefreshInventory(); }

    private void RefreshInventory()
    {
        _itemList.Clear();
        _descriptionLabel.Text = "";
        _selectedItemId = null;
        _useButton.Disabled = true;

        foreach (string id in Inventory.Instance.GetByCategory(_currentTab))
        {
            Item item = ItemDatabase.Get(id);
            if (item == null) continue;
            int count = Inventory.Instance.GetCount(id);
            string label = count > 1 ? $"{item.DisplayName} x{count}" : item.DisplayName;
            _itemList.AddItem(label);
            // ponytail: store id at same index via metadata
            _itemList.SetItemMetadata(_itemList.ItemCount - 1, id);
        }

        if (_itemList.ItemCount > 0)
        {
            _itemList.Select(0);
            OnItemSelected(0);
        }
    }

    private void OnItemSelected(long index)
    {
        if (index < 0 || index >= _itemList.ItemCount) return;
        _selectedItemId = (string)_itemList.GetItemMetadata((int)index);
        Item item = ItemDatabase.Get(_selectedItemId);
        if (item == null) return;
        _descriptionLabel.Text = item.Description;

        if (item.Category == ItemCategory.Consumable)
        {
            _useButton.Disabled = false;
            _useButton.Text = "Use";
        }
        else if (item.Category == ItemCategory.Equipment)
        {
            bool alreadyEquipped = Equipment.Instance.IsEquipped(item.Id);
            _useButton.Disabled = alreadyEquipped;
            _useButton.Text = alreadyEquipped ? "Equipped" : "Equip";
        }
        else
        {
            _useButton.Disabled = true;
            _useButton.Text = "Use";
        }
    }

    private void OnUsePressed()
    {
        if (_selectedItemId == null) return;
        Item item = ItemDatabase.Get(_selectedItemId);
        if (item == null) return;

        AudioManager.Instance.PlaySfx(_confirmSfx);

        if (item.Category == ItemCategory.Consumable)
        {
            var hc = Player.Instance.HealthComponent;
            if (hc != null && !hc.IsDead)
                hc.Heal(item.HealAmount);
            Inventory.Instance.Remove(_selectedItemId, 1);
        }
        else if (item.Category == ItemCategory.Equipment && !Equipment.Instance.IsEquipped(item.Id))
        {
            Equipment.Instance.Equip(item);
        }

        RefreshInventory();
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
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), db);
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

    private void OnTextSpeedChanged(long index)
    {
        DialogManager.CurrentTextSpeed = (DialogManager.TextSpeed)_textSpeedOption.GetItemId((int)index);
    }

    private void OnSettingsBackPressed()
    {
        SaveSettings();
        ShowPanel(Panel.Main);
        _resumeButton.GrabFocus();
    }

    private void SetupTextSpeedOption()
    {
        var vbox = _settingsPanel.GetNode<VBoxContainer>("VBoxContainer");
        var hbox = new HBoxContainer { Name = "TextSpeedBox" };
        var label = new Label { Text = "Text Speed:", CustomMinimumSize = new Vector2(120, 0) };
        hbox.AddChild(label);
        _textSpeedOption = new OptionButton();
        _textSpeedOption.AddItem("Normal", (int)DialogManager.TextSpeed.Normal);
        _textSpeedOption.AddItem("Fast", (int)DialogManager.TextSpeed.Fast);
        _textSpeedOption.AddItem("Instant", (int)DialogManager.TextSpeed.Instant);
        _textSpeedOption.Selected = (int)DialogManager.TextSpeed.Fast;
        hbox.AddChild(_textSpeedOption);
        // Insert before the back button (last child)
        var backButton = vbox.GetChild(vbox.GetChildCount() - 1);
        vbox.AddChild(hbox);
        vbox.MoveChild(hbox, vbox.GetChildCount() - 2);
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
        // ponytail: find closest matching scale option
        for (int i = 0; i < _scaleOption.ItemCount; i++)
        {
            if ((int)_scaleOption.GetItemId(i) == scale)
            {
                _scaleOption.Selected = i;
                break;
            }
        }
        // Restore text speed
        for (int i = 0; i < _textSpeedOption.ItemCount; i++)
        {
            if ((int)_textSpeedOption.GetItemId(i) == textSpeed)
            {
                _textSpeedOption.Selected = i;
                break;
            }
        }
        DialogManager.CurrentTextSpeed = (DialogManager.TextSpeed)textSpeed;

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
        config.SetValue("display", "text_speed", (int)_textSpeedOption.GetItemId(_textSpeedOption.Selected));
        config.Save(SettingsPath);
    }
}
