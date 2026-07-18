using Godot;
using System.Collections.Generic;

public partial class ChoiceMenu : CanvasLayer
{
    [Signal]
    public delegate void ChoiceSelectedEventHandler(int index);

    private List<Button> _buttons = new();
    private List<string> _originalTexts = new();
    private int _selectedIndex;
    private Control _root;
    private VBoxContainer _choiceContainer;
    private static Font _yosterFont => FontCache.Yoster;


    public override void _Ready()
    {
        Layer = 129;

        _root = new Control();
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_root);

        var bgPanel = new Panel
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(0, 0, 0, 0.5f)
        };
        bgPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(bgPanel);

        var centerBox = new CenterContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        centerBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(centerBox);

        _choiceContainer = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        _choiceContainer.AddThemeConstantOverride("separation", 8);
        centerBox.AddChild(_choiceContainer);
    }

    public void SetChoices(List<string> choices)
    {
        _selectedIndex = 0;
        _buttons.Clear();
        _originalTexts.Clear();

        foreach (string choice in choices)
        {
            _originalTexts.Add(choice);
            var btn = new Button
            {
                Text = choice,
                Flat = false
            };
            if (_yosterFont != null)
            {
                btn.AddThemeFontOverride("font", _yosterFont);
                btn.AddThemeFontSizeOverride("font_size", 14);
            }
            btn.MouseEntered += () => SelectIndex(_buttons.IndexOf(btn));
            btn.Pressed += OnChoicePressed;
            _buttons.Add(btn);
            _choiceContainer.AddChild(btn);
        }

        UpdateSelectionDisplay();
        GameLogger.Debug("Dialog", $"ChoiceMenu: {choices.Count} choices presented");
    }

    void SelectIndex(int index)
    {
        _selectedIndex = index;
        UpdateSelectionDisplay();
    }

    void UpdateSelectionDisplay()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            string prefix = i == _selectedIndex ? "> " : "  ";
            _buttons[i].Text = prefix + _originalTexts[i];
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_up"))
        {
            SelectIndex(Mathf.Max(0, _selectedIndex - 1));
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_down"))
        {
            SelectIndex(Mathf.Min(_buttons.Count - 1, _selectedIndex + 1));
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
        {
            if (_buttons.Count > 0)
                OnChoicePressed();
            GetViewport().SetInputAsHandled();
        }
    }

    void OnChoicePressed()
    {
        string chosen = _originalTexts[_selectedIndex];
        GameLogger.Info("Dialog", $"ChoiceMenu: choice {_selectedIndex} selected — '{chosen}'");
        EmitSignal(SignalName.ChoiceSelected, _selectedIndex);
    }
}
