using Godot;

/// <summary>
/// Dev-only named save-state tooling: Ctrl+S captures the current game state to
/// the "quick" slot, Ctrl+L loads it back, Ctrl+M toggles the save-state menu
/// (list / capture / load / delete / rename user slots and repo fixtures).
///
/// Gated on nothing — this is a dev convenience for the demo build; per project
/// decision (2026-08) it ships as-is since the game is shared with friends.
/// Created in code — no scene file needed (same pattern as DebugOverlay).
/// </summary>
public partial class DevSaveStates : CanvasLayer
{
    private static DevSaveStates _instance;
    public static DevSaveStates Instance => _instance;

    private PanelContainer _panel;
    private VBoxContainer _root;
    private LineEdit _nameEdit;
    private ItemList _slotList;
    private Label _toast;
    private Tween _toastTween;
    private bool _menuVisible;
    private string _lastSlot = SaveManager.QuickSlotName;
    /// <summary>Slot currently selected in the list (rename source). Empty until a row is picked.</summary>
    private string _selectedSlot = "";

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
            QueueFree();

        Layer = 128; // above everything, same as DebugOverlay
        ProcessMode = ProcessModeEnum.Always; // hotkeys work even while paused

        BuildToast();
        BuildMenu();
        Visible = true; // layer always present; menu toggles inside
        _panel.Visible = false;
        _menuVisible = false;
    }

    private void BuildToast()
    {
        _toast = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(0, 8),
            ThemeTypeVariation = "HudLabel"
        };
        _toast.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _toast.Visible = false;
        AddChild(_toast);
    }

    private void BuildMenu()
    {
        _panel = new PanelContainer
        {
            ThemeTypeVariation = "MenuPanel",
            Visible = false
        };
        _panel.SetAnchorsPreset(Control.LayoutPreset.Center);

        _root = new VBoxContainer { CustomMinimumSize = new Vector2(360, 260) };
        _panel.AddChild(_root);

        var title = new Label { Text = "DEV SAVE STATES", ThemeTypeVariation = "MenuLabelTitle", HorizontalAlignment = HorizontalAlignment.Center };
        _root.AddChild(title);

        _nameEdit = new LineEdit
        {
            PlaceholderText = "slot name (empty = 'quick')",
            ThemeTypeVariation = "MenuOption"
        };
        _nameEdit.TextSubmitted += OnNameSubmitted;
        _root.AddChild(_nameEdit);

        _slotList = new ItemList
        {
            ThemeTypeVariation = "ItemListRetro",
            CustomMinimumSize = new Vector2(0, 140),
            AllowRmbSelect = false
        };
        _slotList.ItemSelected += OnSlotSelected;
        _root.AddChild(_slotList);

        var buttons = new HBoxContainer { CustomMinimumSize = new Vector2(0, 40) };
        buttons.AddChild(MakeButton("Capture", OnCapturePressed));
        buttons.AddChild(MakeButton("Load", OnLoadPressed));
        buttons.AddChild(MakeButton("Delete", OnDeletePressed));
        buttons.AddChild(MakeButton("Rename", OnRenamePressed));
        buttons.AddChild(MakeButton("Close", OnClosePressed));
        _root.AddChild(buttons);

        var hint = new Label
        {
            Text = "Ctrl+S capture quick · Ctrl+L load quick · Ctrl+M menu",
            ThemeTypeVariation = "MenuLabelSmall",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _root.AddChild(hint);

        AddChild(_panel);
    }

    private Button MakeButton(string text, System.Action onClick)
    {
        var button = new Button { Text = text, ThemeTypeVariation = "MenuButton" };
        button.Pressed += onClick;
        return button;
    }

    // --- Input ---

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("dev_save_state") &&
            !@event.IsActionPressed("dev_load_state") &&
            !@event.IsActionPressed("dev_save_menu"))
            return;

        if (!CanUseHotkeys())
        {
            GameLogger.Debug("DevSaveStates", "Hotkey ignored — dialog/cutscene/combat active.");
            return;
        }

        if (@event.IsActionPressed("dev_save_menu"))
        {
            ToggleMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("dev_save_state"))
        {
            // When the menu is open and the name field has focus, capture to the
            // typed name; otherwise capture to the quick slot.
            string slot = MenuNameFieldActive() ? _nameEdit.Text : SaveManager.QuickSlotName;
            CaptureCurrentState(slot);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("dev_load_state"))
        {
            string slot = MenuNameFieldActive() ? _nameEdit.Text : _lastSlot;
            LoadSlot(slot);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>True while the menu is open and the name field is focused (typing a slot name).</summary>
    private bool MenuNameFieldActive()
    {
        return _menuVisible && _nameEdit.HasFocus();
    }

    private bool CanUseHotkeys()
    {
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive)
            return false;
        if (CutsceneController.Instance != null && CutsceneController.Instance.IsPlaying)
            return false;
        if (GameController.Instance?.CurrentLevel is CombatArena)
            return false;
        return true;
    }

    // --- Actions ---

    private void CaptureCurrentState(string slotName)
    {
        var level = GameController.Instance?.CurrentLevel;
        if (level == null)
        {
            ShowToast("No level loaded — nothing to capture");
            return;
        }
        string scenePath = level.SceneFilePath;
        Vector2 pos = Player.Instance?.Position ?? Vector2.Zero;
        string locationName = (level as BaseLevel)?.LevelName ?? level.Name;

        string sanitized = SaveManager.SanitizeSlotName(slotName);
        SaveManager.Instance.SaveGame(scenePath, pos, locationName, sanitized);
        _lastSlot = sanitized;
        ShowToast($"Captured → '{sanitized}'");
        RefreshList();
    }

    private void LoadSlot(string slotName)
    {
        string sanitized = SaveManager.SanitizeSlotName(slotName);
        if (!SaveManager.Instance.HasSave(sanitized))
        {
            ShowToast($"No state '{sanitized}'");
            return;
        }
        ShowToast($"Loading '{sanitized}'…");
        bool loaded = SaveManager.Instance.LoadGame(sanitized);
        if (loaded)
        {
            _lastSlot = sanitized;
            CloseMenu();
        }
        else
        {
            ShowToast($"Load of '{sanitized}' failed");
        }
    }

    private void ToggleMenu()
    {
        if (_menuVisible)
            CloseMenu();
        else
            OpenMenu();
    }

    private void OpenMenu()
    {
        _menuVisible = true;
        _panel.Visible = true;
        RefreshList();
        _nameEdit.GrabFocus();
    }

    private void CloseMenu()
    {
        _menuVisible = false;
        _panel.Visible = false;
        _selectedSlot = "";
    }

    private void RefreshList()
    {
        _slotList.Clear();
        foreach (string slot in SaveManager.Instance.ListSlots())
            _slotList.AddItem(slot);
        foreach (string fixture in SaveManager.Instance.ListFixtures())
            _slotList.AddItem($"{fixture}  (fixture)");
    }

    private void OnSlotSelected(long index)
    {
        if (index < 0 || index >= _slotList.ItemCount)
            return;
        string label = _slotList.GetItemText((int)index);
        // Strip the " (fixture)" suffix for the name field.
        _selectedSlot = label.EndsWith("  (fixture)")
            ? label.Substring(0, label.Length - "  (fixture)".Length)
            : label;
        _nameEdit.Text = _selectedSlot;
    }

    private void OnNameSubmitted(string newText)
    {
        // Enter in the name field = capture.
        CaptureCurrentState(newText);
    }

    private void OnCapturePressed()
    {
        CaptureCurrentState(_nameEdit.Text);
    }

    private void OnLoadPressed()
    {
        LoadSlot(_nameEdit.Text);
    }

    private void OnDeletePressed()
    {
        string name = _nameEdit.Text;
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowToast("Type a slot name to delete");
            return;
        }
        string sanitized = SaveManager.SanitizeSlotName(name);
        if (SaveManager.Instance.ListSlots().Contains(sanitized))
        {
            SaveManager.Instance.DeleteSave(sanitized);
            ShowToast($"Deleted '{sanitized}'");
            RefreshList();
        }
        else
        {
            ShowToast($"'{sanitized}' is a fixture or missing — not deleted");
        }
    }

    private void OnRenamePressed()
    {
        // Source is the slot selected in the list; fall back to last used if
        // nothing was picked. Target is whatever is typed in the name field.
        string from = string.IsNullOrEmpty(_selectedSlot) ? _lastSlot : _selectedSlot;
        string to = SaveManager.SanitizeSlotName(_nameEdit.Text);
        if (string.IsNullOrWhiteSpace(_nameEdit.Text) || from == to)
        {
            ShowToast("Select a slot in the list, type the new name, press Rename");
            return;
        }
        if (SaveManager.Instance.RenameSlot(from, to))
        {
            _lastSlot = to;
            _selectedSlot = "";
            ShowToast($"Renamed '{from}' → '{to}'");
            RefreshList();
        }
        else
        {
            ShowToast("Rename failed (fixture or name taken)");
        }
    }

    private void OnClosePressed()
    {
        CloseMenu();
    }

    // --- Toast ---

    private void ShowToast(string message)
    {
        _toast.Text = message;
        _toast.Visible = true;
        _toast.Modulate = Colors.White;
        _toastTween?.Kill();
        _toastTween = CreateTween();
        _toastTween.TweenInterval(1.2f);
        _toastTween.TweenProperty(_toast, "modulate:a", 0.0f, 0.6f);
        _toastTween.TweenCallback(Callable.From(() => _toast.Visible = false));
    }
}
