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
            MouseFilter = Control.MouseFilterEnum.Pass,
            SelfModulate = new Color(0, 0, 0, 0.7f)
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
        _originalTexts = new List<string>(choices);

        for (int i = 0; i < choices.Count; i++)
        {
            var btn = new Button
            {
                Text = "  " + choices[i],
                Flat = true,
                MouseFilter = Control.MouseFilterEnum.Pass,
                CustomMinimumSize = new Vector2(240, 24),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
            };
            btn.AddThemeFontSizeOverride("font_size", 16);
            if (_yosterFont != null)
                btn.AddThemeFontOverride("font", _yosterFont);
            btn.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            btn.AddThemeColorOverride("font_hover_color", new Color(1, 1, 0.5f));
            btn.AddThemeColorOverride("font_focus_color", new Color(1, 1, 0.5f));
            btn.AddThemeColorOverride("font_pressed_color", new Color(1, 1, 0.5f));

            int capture = i;
            btn.MouseEntered += () => SelectIndex(capture);
            btn.Pressed += () => EmitSignal(SignalName.ChoiceSelected, capture);
            _choiceContainer.AddChild(btn);
            _buttons.Add(btn);
        }

        if (_buttons.Count > 0)
        {
            _selectedIndex = 0;
            UpdateSelectionDisplay();
            _buttons[0].GrabFocus();
        }
    }

    void SelectIndex(int index)
    {
        _selectedIndex = index;
        UpdateSelectionDisplay();
        _buttons[index].GrabFocus();
    }

    void UpdateSelectionDisplay()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            string marker = i == _selectedIndex ? "> " : "  ";
            _buttons[i].Text = marker + _originalTexts[i];
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("player_up") || @event.IsActionPressed("ui_up"))
        {
            _selectedIndex = (_selectedIndex - 1 + _buttons.Count) % _buttons.Count;
            UpdateSelectionDisplay();
            _buttons[_selectedIndex].GrabFocus();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("player_down") || @event.IsActionPressed("ui_down"))
        {
            _selectedIndex = (_selectedIndex + 1) % _buttons.Count;
            UpdateSelectionDisplay();
            _buttons[_selectedIndex].GrabFocus();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("interact") || @event.IsActionPressed("ui_accept"))
        {
            _buttons[_selectedIndex].EmitSignal(Button.SignalName.Pressed);
            GetViewport().SetInputAsHandled();
        }
    }
}
