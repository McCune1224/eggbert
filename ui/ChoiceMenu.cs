using Godot;
using System.Collections.Generic;

public partial class ChoiceMenu : Control
{
    [Signal]
    public delegate void ChoiceSelectedEventHandler(int index);

    private VBoxContainer _choiceContainer;
    private List<Button> _buttons = new();
    private int _selectedIndex;
    private Panel _bgPanel;

    public override void _Ready()
    {
        AnchorLeft = 0;
        AnchorRight = 1;
        AnchorTop = 0;
        AnchorBottom = 1;
        MouseFilter = MouseFilterEnum.Ignore;

        _bgPanel = new Panel
        {
            MouseFilter = MouseFilterEnum.Pass,
            SelfModulate = new Color(0, 0, 0, 0.7f)
        };
        _bgPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_bgPanel);

        var centerBox = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        centerBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(centerBox);

        _choiceContainer = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        _choiceContainer.AddThemeConstantOverride("separation", 8);
        centerBox.AddChild(_choiceContainer);
    }

    public void SetChoices(List<string> choices)
    {
        int fontSize = 16;
        var font = ResourceLoader.Load<Font>("res://assets/fonts/yoster.ttf");

        for (int i = 0; i < choices.Count; i++)
        {
            var btn = new Button
            {
                Text = choices[i],
                Flat = true,
                MouseFilter = MouseFilterEnum.Pass,
                CustomMinimumSize = new Vector2(200, 0)
            };
            btn.AddThemeFontSizeOverride("font_size", fontSize);
            if (font != null)
                btn.AddThemeFontOverride("font", font);
            btn.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            btn.AddThemeColorOverride("font_hover_color", new Color(1, 1, 0.5f));
            btn.AddThemeColorOverride("font_pressed_color", new Color(1, 1, 0.5f));
            btn.AddThemeColorOverride("font_focus_color", new Color(1, 1, 0.5f));

            int capture = i;
            btn.Pressed += () => EmitSignal(SignalName.ChoiceSelected, capture);
            _choiceContainer.AddChild(btn);
            _buttons.Add(btn);
        }

        if (_buttons.Count > 0)
        {
            _selectedIndex = 0;
            _buttons[0].GrabFocus();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("player_up") || @event.IsActionPressed("ui_up"))
        {
            _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
            _buttons[_selectedIndex].GrabFocus();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("player_down") || @event.IsActionPressed("ui_down"))
        {
            _selectedIndex = Mathf.Min(_buttons.Count - 1, _selectedIndex + 1);
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
