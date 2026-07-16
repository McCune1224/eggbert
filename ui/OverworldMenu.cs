using Godot;
using System.Collections.Generic;

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
    // Keybinding section
    private Dictionary<string, Button> _keybindButtons = new();
    private bool _awaitingRebind = false;
    private string _rebindingAction = null;

    // Map panel
    private PanelContainer _mapPanel;
    private TextureRect _mapTexture;
    private GridContainer _warpGrid;
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
    private TextureRect _iconRect;
    private Label _nameLabel;
    private Label _countLabel;
    private Label _statsLabel;
    private Label _hpLabel;
    // Help panel
    private PanelContainer _helpPanel;
    private Button _helpButton;
    private Button _helpBackButton;
    private VBoxContainer _helpVBox;
    private Button _mainMenuButton;

    private enum Panel { Main, Settings, Map, Inventory, Help }
    private Panel _currentPanel = Panel.Main;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // Main menu
        _mainPanel = GetNode<PanelContainer>("MainPanel");
        _resumeButton = GetNode<Button>("MainPanel/VBoxContainer/ResumeButton");
        _mapButton = GetNode<Button>("MainPanel/VBoxContainer/GridRow1/MapButton");
        _inventoryButton = GetNode<Button>("MainPanel/VBoxContainer/GridRow1/InventoryButton");
        _saveButton = GetNode<Button>("MainPanel/VBoxContainer/GridRow2/SaveButton");
        _settingsButton = GetNode<Button>("MainPanel/VBoxContainer/GridRow3/SettingsButton");
        _quitButton = GetNode<Button>("MainPanel/VBoxContainer/QuitButton");
        _mainMenuButton = GetNode<Button>("MainPanel/VBoxContainer/GridRow3/MainMenuButton");

        _resumeButton.Connect("pressed", new Callable(this, nameof(OnResumePressed)));
        _mapButton.Connect("pressed", new Callable(this, nameof(OnMapPressed)));
        _inventoryButton.Connect("pressed", new Callable(this, nameof(OnInventoryPressed)));
        _saveButton.Connect("pressed", new Callable(this, nameof(OnSavePressed)));
        _settingsButton.Connect("pressed", new Callable(this, nameof(OnSettingsPressed)));
        _quitButton.Connect("pressed", new Callable(this, nameof(OnQuitPressed)));
        _mainMenuButton.Connect("pressed", new Callable(this, nameof(OnMainMenuPressed)));

        // Settings panel
        _settingsPanel = GetNode<PanelContainer>("SettingsPanel");
        _musicSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/MusicBox/MusicSlider");
        _sfxSlider = GetNode<HSlider>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/SfxBox/SfxSlider");
        _fullscreenCheck = GetNode<CheckButton>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/FullscreenBox/FullscreenCheck");
        _scaleOption = GetNode<OptionButton>("SettingsPanel/VBoxContainer/ScrollContainer/SettingsVBox/ScaleBox/ScaleOption");
        _settingsBackButton = GetNode<Button>("SettingsPanel/VBoxContainer/BackButton");

        _scaleOption.AddItem("1x", 1);
        _scaleOption.AddItem("2x", 2);
        _scaleOption.AddItem("3x", 3);
        _scaleOption.AddItem("4x", 4);
        _scaleOption.Selected = 1;

        SetupTextSpeedOption();

        SetupKeybindSection();

        _musicSlider.Connect("value_changed", new Callable(this, nameof(OnMusicVolumeChanged)));
        _sfxSlider.Connect("value_changed", new Callable(this, nameof(OnSfxVolumeChanged)));
        _fullscreenCheck.Connect("toggled", new Callable(this, nameof(OnFullscreenToggled)));
        _scaleOption.Connect("item_selected", new Callable(this, nameof(OnScaleChanged)));
        _textSpeedOption.Connect("item_selected", new Callable(this, nameof(OnTextSpeedChanged)));
        _settingsBackButton.Connect("pressed", new Callable(this, nameof(OnSettingsBackPressed)));

        // Map panel
        _mapPanel = GetNode<PanelContainer>("MapPanel");
        _mapTexture = GetNode<TextureRect>("MapPanel/VBoxContainer/MapTexture");
        _warpGrid = GetNode<GridContainer>("MapPanel/VBoxContainer/WarpGrid");
        _mapBackButton = GetNode<Button>("MapPanel/VBoxContainer/MapBackButton");
        _mapBackButton.Connect("pressed", new Callable(this, nameof(OnMapBackPressed)));

        // Inventory panel
        _inventoryPanel = GetNode<PanelContainer>("InventoryPanel");
        _keyTab = GetNode<Button>("InventoryPanel/VBoxContainer/TabBox/KeyTab");
        _consumableTab = GetNode<Button>("InventoryPanel/VBoxContainer/TabBox/ConsumableTab");
        _equipmentTab = GetNode<Button>("InventoryPanel/VBoxContainer/TabBox/EquipmentTab");
        _itemList = GetNode<ItemList>("InventoryPanel/VBoxContainer/ContentRow/ItemList");
        _descriptionLabel = GetNode<Label>("InventoryPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/DescriptionLabel");
        _useButton = GetNode<Button>("InventoryPanel/VBoxContainer/ButtonRow/UseButton");
        _inventoryBackButton = GetNode<Button>("InventoryPanel/VBoxContainer/ButtonRow/InventoryBackButton");
        _iconRect = GetNode<TextureRect>("InventoryPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/DetailHeader/IconRect");
        _nameLabel = GetNode<Label>("InventoryPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/DetailHeader/NameLabel");
        _countLabel = GetNode<Label>("InventoryPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/CountLabel");
        _statsLabel = GetNode<Label>("InventoryPanel/VBoxContainer/ContentRow/DetailPanel/DetailVBox/StatsLabel");
        _hpLabel = GetNode<Label>("InventoryPanel/VBoxContainer/HpRow/HpLabel");

        _keyTab.Connect("pressed", new Callable(this, nameof(OnKeyTabPressed)));
        
        // Help panel
        _helpPanel = GetNode<PanelContainer>("HelpPanel");
        _helpButton = GetNode<Button>("MainPanel/VBoxContainer/GridRow2/HelpButton");
        _helpBackButton = GetNode<Button>("HelpPanel/VBoxContainer/HelpBackButton");
        _helpVBox = GetNode<VBoxContainer>("HelpPanel/VBoxContainer/ScrollContainer/HelpVBox");
        
        _helpButton.Connect("pressed", new Callable(this, nameof(OnHelpPressed)));
        _helpBackButton.Connect("pressed", new Callable(this, nameof(OnHelpBackPressed)));
        
        SetupHelpContent();
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
        _helpPanel.Visible = panel == Panel.Help;
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
        CutsceneController.Instance.Stop();
        DialogManager.Instance.Reset();
        GetTree().Paused = true;
        ShowMenu();
    }

    private void ShowMenu()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
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

        if (Input.IsActionJustPressed("menu_pause"))
        {
            if (_currentPanel == Panel.Settings)
                OnSettingsBackPressed();
            else if (_currentPanel == Panel.Map)
                OnMapBackPressed();
            else if (_currentPanel == Panel.Inventory)
                OnInventoryBackPressed();
            else if (_currentPanel == Panel.Help)
                OnHelpBackPressed();
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
        foreach (Node child in _warpGrid.GetChildren())
            child.QueueFree();

        var unlocked = WarpDatabase.GetUnlocked();
        if (unlocked.Count == 0)
        {
            var lbl = new Label { Text = "No warps discovered" };
            lbl.LayoutMode = 0;
            lbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            _warpGrid.AddChild(lbl);
            return;
        }

        foreach (var warp in unlocked)
        {
            var btn = new Button { Text = warp.Name };
            btn.LayoutMode = 0;
            string levelPath = warp.LevelPath;
            Vector2 pos = warp.Position;
            btn.Pressed += () => WarpTo(levelPath, pos);
            _warpGrid.AddChild(btn);
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
        UpdateHpLabel();
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
        _iconRect.Texture = null;
        _nameLabel.Text = "";
        _countLabel.Text = "";
        _descriptionLabel.Text = "";
        _statsLabel.Text = "";
        _selectedItemId = null;
        _useButton.Disabled = true;

        foreach (string id in Inventory.Instance.GetByCategory(_currentTab))
        {
            Item item = ItemDatabase.Get(id);
            if (item == null) continue;
            int count = Inventory.Instance.GetCount(id);
            string label = count > 1 ? $"{item.DisplayName} x{count}" : item.DisplayName;
            _itemList.AddItem(label, item.Icon);
            _itemList.SetItemMetadata(_itemList.ItemCount - 1, id);
        }

        if (_itemList.ItemCount > 0)
        {
            _itemList.Select(0);
            OnItemSelected(0);
        }
        UpdateHpLabel();
    }

    private void OnItemSelected(long index)
    {
        if (index < 0 || index >= _itemList.ItemCount) return;
        _selectedItemId = (string)_itemList.GetItemMetadata((int)index);
        Item item = ItemDatabase.Get(_selectedItemId);
        if (item == null) return;

        _iconRect.Texture = item.Icon;
        _nameLabel.Text = item.DisplayName;
        int count = Inventory.Instance.GetCount(item.Id);
        _countLabel.Text = count > 1 ? $"x{count}" : "";

        // Show alternate description if item has been used
        bool wasUsed = WorldFlags.Instance.HasFlag($"item_{item.Id}_used");
        _descriptionLabel.Text = (wasUsed && !string.IsNullOrEmpty(item.DescriptionUsed))
            ? item.DescriptionUsed : item.Description;
        if (item.Category == ItemCategory.Key)
        {
            _statsLabel.Text = "";
            _useButton.Disabled = true;
            _useButton.Text = "Use";
        }
        else if (item.Category == ItemCategory.Consumable)
        {
            _statsLabel.Text = item.HealAmount > 0 ? $"Restores {item.HealAmount} HP" : "";
            _useButton.Disabled = false;
            _useButton.Text = "Use";
        }
        else if (item.Category == ItemCategory.Equipment)
        {
            var boosts = new System.Collections.Generic.List<string>();
            if (item.MaxHPBoost > 0) boosts.Add($"+{item.MaxHPBoost} HP");
            if (item.AttackBoost > 0) boosts.Add($"+{item.AttackBoost} ATK");
            if (item.DefenseBoost > 0) boosts.Add($"+{item.DefenseBoost} DEF");
            if (item.SpeedBoost > 0) boosts.Add($"+{item.SpeedBoost} SPD");
            _statsLabel.Text = string.Join(", ", boosts);

            // Show stat preview for unequipped items
            if (!Equipment.Instance.IsEquipped(item.Id))
            {
                string preview = Equipment.Instance.PreviewDeltas(item);
                if (!string.IsNullOrEmpty(preview))
                    _statsLabel.Text += $"\n[{preview}]";
            }

            if (Equipment.Instance.IsEquipped(item.Id))
            {
                _statsLabel.Text += " (Equipped)";
                _useButton.Disabled = false;
                _useButton.Text = "Unequip";
            }
            else
            {
                _useButton.Disabled = false;
                _useButton.Text = "Equip";
            }
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
            WorldFlags.Instance.SetFlag($"item_{item.Id}_used", true);
        }
        else if (item.Category == ItemCategory.Equipment)
        {
            if (Equipment.Instance.IsEquipped(item.Id))
                Equipment.Instance.Unequip(item.Slot);
            else
                Equipment.Instance.Equip(item);
            WorldFlags.Instance.SetFlag($"item_{item.Id}_used", true);
        }

        RefreshInventory();
    }

    private void UpdateHpLabel()
    {
        var hc = Player.Instance.HealthComponent;
        if (hc != null)
            _hpLabel.Text = $"HP {hc.CurrentHP}/{hc.MaxHP}";
    }

    // --- Help ---

    private void SetupHelpContent()
    {
        foreach (Node child in _helpVBox.GetChildren())
            child.QueueFree();

        string currentSection = "";
        foreach (string action in KeybindManager.RebindableActions)
        {
            string section = action switch
            {
                "player_up" or "player_down" or "player_left" or "player_right" => "Movement",
                "interact" or "player_sprint" or "dash" or "combat_parry" => "Actions",
                "menu_pause" => "Menu",
                _ => null
            };
            if (section != null && section != currentSection)
            {
                currentSection = section;
                var sectionLabel = new Label { Text = section + ":", ThemeTypeVariation = "" };
                sectionLabel.AddThemeFontSizeOverride("font_size", 14);
                sectionLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.75f, 1.0f));
                _helpVBox.AddChild(sectionLabel);
            }

            var row = new HBoxContainer();
            var nameLabel = new Label { Text = KeybindManager.GetActionDisplayName(action), CustomMinimumSize = new Vector2(140, 0) };
            nameLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 1.0f));
            row.AddChild(nameLabel);
            var keyLabel = new Label { Text = KeybindManager.GetCurrentKeyLabel(action) };
            keyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 0.9f));
            row.AddChild(keyLabel);
            _helpVBox.AddChild(row);
        }
    }

    private void OnHelpPressed()
    {
        AudioManager.Instance.PlaySfx(_confirmSfx);
        SetupHelpContent();
        ShowPanel(Panel.Help);
        _helpBackButton.GrabFocus();
    }

    private void OnHelpBackPressed()
    {
        ShowPanel(Panel.Main);
        _helpButton.GrabFocus();
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

    private void OnMainMenuPressed()
    {
        AudioManager.Instance.PlaySfx(_confirmSfx);
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://ui/MainMenu.tscn");
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
        var vbox = _settingsPanel.GetNode<VBoxContainer>("VBoxContainer/ScrollContainer/SettingsVBox");

        var title = new Label { Text = "Controls:" };
        title.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(title);

        var grid = new GridContainer();
        grid.Columns = 2;
        grid.CustomMinimumSize = new Vector2(380, 0);
        vbox.AddChild(grid);

        foreach (string action in KeybindManager.RebindableActions)
        {
            var label = new Label
            {
                Text = KeybindManager.GetActionDisplayName(action),
                CustomMinimumSize = new Vector2(160, 24),
            };
            label.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 1.0f));
            grid.AddChild(label);

            var btn = new Button
            {
                Text = KeybindManager.GetCurrentKeyLabel(action),
                CustomMinimumSize = new Vector2(100, 24),
            };
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
        KeybindManager.SaveBindings();
        config.Save(SettingsPath);
    }
}
