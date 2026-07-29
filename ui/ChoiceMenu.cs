using Godot;
using System.Collections.Generic;

/// <summary>
/// Overlay choice menu for dialog response selection.
/// Arrow keys (up/down) navigate, E/Interact confirms the selection.
/// Choices are presented as a vertical list with a bobbing cursor indicator.
/// The selected index is returned via <see cref="ChoiceSelected"/> signal.
/// The caller receives the chosen index via <see cref="ChoiceSelected"/> signal
/// (or awaitable Task from DialogManager.PromptChoices) and maps it to a WorldFlag
/// in the cutscene/dialog caller.
/// </summary>
public partial class ChoiceMenu : CanvasLayer
{
	[Signal]
	public delegate void ChoiceSelectedEventHandler(int index);

	private List<Button> _buttons = new();
	private List<string> _originalTexts = new();
	private List<Sprite2D> _cursors = new();
	private int _selectedIndex;
	private Control _root;
	private VBoxContainer _choiceContainer;
	private PanelContainer _panel;
	private MarginContainer _innerMargin;
	private double _cursorBobTime;
	private static Font _yosterFont => FontCache.Yoster;
	private static Texture2D _cursorTexture => ResourceLoader.Load<Texture2D>("res://assets/ui/cursor_arrow.png");

	private const float CURSOR_BOB_SPEED = 4.0f;       // radians per second
	private const float CURSOR_BOB_AMPLITUDE = 2.0f;   // pixels
	private const int CURSOR_SLOT_SIZE = 12;            // px, must match cursor texture
	private const int PANEL_PADDING = 16;
	private const int CHOICE_SEPARATION = 8;


	public override void _Ready()
	{
		Layer = 129;

		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_root);

		// Full-screen dark backdrop.
		var backdrop = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.5f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(backdrop);

		// Centered, auto-sizing panel. PanelContainer propagates the inner
		// content's minimum size to itself, and the MenuPanel theme variation
		// paints the panel_9slice background.
		var centerBox = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		centerBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(centerBox);

		_panel = new PanelContainer
		{
			ThemeTypeVariation = "MenuPanel",
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		centerBox.AddChild(_panel);

		_innerMargin = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		_innerMargin.AddThemeConstantOverride("margin_left", PANEL_PADDING);
		_innerMargin.AddThemeConstantOverride("margin_right", PANEL_PADDING);
		_innerMargin.AddThemeConstantOverride("margin_top", PANEL_PADDING);
		_innerMargin.AddThemeConstantOverride("margin_bottom", PANEL_PADDING);
		_panel.AddChild(_innerMargin);

		_choiceContainer = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		_choiceContainer.AddThemeConstantOverride("separation", CHOICE_SEPARATION);
		_innerMargin.AddChild(_choiceContainer);
	}

	public void SetChoices(List<string> choices)
	{
		_selectedIndex = 0;
		_buttons.Clear();
		_originalTexts.Clear();
		_cursors.Clear();

		foreach (string choice in choices)
		{
			_originalTexts.Add(choice);

			// Row: fixed-size Control slot (reserves cursor width in HBoxContainer)
			// containing a Sprite2D for the bobbing draw.
			var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Stop };
			row.AddThemeConstantOverride("separation", 6);

			var slot = new Control
			{
				CustomMinimumSize = new Vector2(CURSOR_SLOT_SIZE, CURSOR_SLOT_SIZE),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			var cursor = new Sprite2D
			{
				Texture = _cursorTexture,
				Centered = true,
				Position = new Vector2(CURSOR_SLOT_SIZE / 2f, CURSOR_SLOT_SIZE / 2f)
			};
			slot.AddChild(cursor);
			row.AddChild(slot);
			_cursors.Add(cursor);

			var btn = new Button
			{
				Text = choice,
				Flat = false,
				ThemeTypeVariation = "MenuButton"
			};
			if (_yosterFont != null)
			{
				btn.AddThemeFontOverride("font", _yosterFont);
				btn.AddThemeFontSizeOverride("font_size", 14);
			}
			row.AddChild(btn);
			_buttons.Add(btn);

			btn.MouseEntered += () => SelectIndex(_buttons.IndexOf(btn));
			btn.Pressed += OnChoicePressed;
			_choiceContainer.AddChild(row);
		}

		UpdateSelectionDisplay();
		GameLogger.Debug("Dialog", $"ChoiceMenu: {choices.Count} choices presented");
	}

	public override void _Process(double delta)
	{
		// Bob the selected cursor. HBoxContainer owns the slot's transform,
		// so we animate the inner Sprite2D (child of the slot) — its position
		// is local to the slot, which the container doesn't touch.
		if (_selectedIndex < 0 || _selectedIndex >= _cursors.Count)
			return;
		_cursorBobTime += delta;
		var center = new Vector2(CURSOR_SLOT_SIZE / 2f, CURSOR_SLOT_SIZE / 2f);
		var offset = new Vector2(0, Mathf.Sin((float)_cursorBobTime * CURSOR_BOB_SPEED) * CURSOR_BOB_AMPLITUDE);
		for (int i = 0; i < _cursors.Count; i++)
		{
			if (_cursors[i] != null)
				_cursors[i].Position = center + offset;
		}
	}

	void SelectIndex(int index)
	{
		_selectedIndex = index;
		UpdateSelectionDisplay();
	}

	void UpdateSelectionDisplay()
	{
		for (int i = 0; i < _cursors.Count; i++)
			_cursors[i].Visible = i == _selectedIndex;
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
