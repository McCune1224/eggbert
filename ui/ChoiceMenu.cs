using Godot;
using System.Collections.Generic;

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
	private CenterContainer _centerBox;
	private NinePatchRect _panel;
	private double _cursorBobTime;
	private static Font _yosterFont => FontCache.Yoster;
	private static Texture2D _cursorTexture => ResourceLoader.Load<Texture2D>("res://assets/ui/cursor_arrow.png");
	private static Texture2D _panelTexture => ResourceLoader.Load<Texture2D>("res://assets/ui/panel_9slice.png");

	private const float CURSOR_BOB_SPEED = 4.0f;       // radians per second
	private const float CURSOR_BOB_AMPLITUDE = 2.0f;   // pixels
	private const int PANEL_PADDING = 16;
	private const int CHOICE_SEPARATION = 8;


	public override void _Ready()
	{
		Layer = 129;

		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_root);

		// Full-screen dark backdrop (kept per plan).
		var backdrop = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.5f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(backdrop);

		// Centered NinePatch panel for the choice container.
		_centerBox = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		_centerBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(_centerBox);

		_panel = new NinePatchRect
		{
			Texture = _panelTexture,
			RegionRect = new Rect2(0, 0, 48, 48),
			PatchMarginLeft = 8,
			PatchMarginRight = 8,
			PatchMarginTop = 8,
			PatchMarginBottom = 8,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		_centerBox.AddChild(_panel);

		_choiceContainer = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
		_choiceContainer.AddThemeConstantOverride("separation", CHOICE_SEPARATION);
		var innerMargin = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		innerMargin.AddThemeConstantOverride("margin_left", PANEL_PADDING);
		innerMargin.AddThemeConstantOverride("margin_right", PANEL_PADDING);
		innerMargin.AddThemeConstantOverride("margin_top", PANEL_PADDING);
		innerMargin.AddThemeConstantOverride("margin_bottom", PANEL_PADDING);
		innerMargin.AddChild(_choiceContainer);
		_panel.AddChild(innerMargin);
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

			// Button row: cursor (left) + button (fills rest).
			var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Stop };
			row.AddThemeConstantOverride("separation", 6);

			var cursor = new Sprite2D
			{
				Texture = _cursorTexture,
				Visible = false,
				Centered = true
			};
			row.AddChild(cursor);
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
		// Bob the selected cursor (matches DialogBubble's page-arrow pattern).
		if (_selectedIndex < 0 || _selectedIndex >= _cursors.Count)
			return;
		_cursorBobTime += delta;
		var offset = new Vector2(0, Mathf.Sin((float)_cursorBobTime * CURSOR_BOB_SPEED) * CURSOR_BOB_AMPLITUDE);
		foreach (var cursor in _cursors)
		{
			cursor.Position = new Vector2(cursor.Position.X, 0) + offset;
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
