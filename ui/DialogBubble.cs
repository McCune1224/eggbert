using Godot;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Displays dialog text with a typewriter effect, supporting three text speeds
/// (Instant / Fast / Normal via <see cref="DialogManager.TextSpeed"/>).
/// Fast-forward on held input; skip to end of current page on action press.
/// Manages a typed-character CPS rate, per-char pauses, voice blips (max 16
/// concurrent one-shot AudioStreamPlayers), and page-completion state.
/// </summary>
public partial class DialogBubble : CanvasLayer
{
	const int MAX_VISIBLE_LINES = 3;
	const float FULL_BOX_WIDTH = 576f;
	const float BOX_WIDTH_WITH_PORTRAIT = 456f;
	const float NORMAL_CPS = 40f;
	const float FAST_CPS = 80f;
	const int MAX_ACTIVE_BLIPS = 16;

	const float PAUSE_PERIOD = 0.20f;
	const float PAUSE_QMARK = 0.15f;
	const float PAUSE_EXCLAM = 0.15f;
	const float PAUSE_COMMA = 0.12f;

	const int PORTRAIT_SIZE = 64;
	const int PORTRAIT_FRAME_WIDTH = 76;

	enum State { Idle, Typing, PageComplete }

	struct ActiveBlip
	{
		public AudioStreamPlayer Player;
		public double StartTime;
	}

	State _state = State.Idle;
	string _displayText = "";
	List<float> _charPauses = new();
	List<float> _charCps = new();

	struct Page { public int Start; public int End; }
	List<Page> _pages = new();
	int _pageIndex = 0;
	int _visibleCharCount = 0;
	float _charAccumulator = 0f;
	float _pendingPause = 0f;
	float _currentCps = NORMAL_CPS;
	double _lastAdvanceTime = 0d;
	const double ADVANCE_COOLDOWN = 0.15d;

	DialogVoiceResource _voice;
	List<ActiveBlip> _activeBlips = new(MAX_ACTIVE_BLIPS);

	static AudioStream _chime;
	static Font _yosterFont;
	static Texture2D _panelTexture;
	static Texture2D _panelHighlightTexture;
	static Texture2D _arrowTexture;

	Control _dialogBar;
	PanelContainer _dialogPanel;
	MarginContainer _textMargin;
	Label _textLabel;
	Control _namePlate;
	NinePatchRect _namePlateBg;
	Label _nameLabel;
	Control _pageArrow;
	Sprite2D _arrowSprite;
	Control _portraitContainer;
	NinePatchRect _portraitFrame;
	TextureRect _portraitTexture;
	HBoxContainer _contentBox;

	[Signal]
	public delegate void LineCompleteEventHandler();

	static DialogBubble()
	{
		_chime = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/retro/SoundClick.wav");
		_yosterFont = FontCache.Yoster;
		_panelTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/panel_9slice.png");
		_panelHighlightTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/panel_highlight.png");
		_arrowTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/Arrow.png");
	}

	public override void _Ready()
	{
		Layer = 128;

		BuildDialogBar();
		BuildNamePlate();
	}

	void BuildDialogBar()
	{
		// Root control — anchored at the bottom, ~110px tall
		_dialogBar = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_dialogBar.SetAnchor(Side.Left, 0);
		_dialogBar.SetAnchor(Side.Top, 1);
		_dialogBar.SetAnchor(Side.Right, 1);
		_dialogBar.SetAnchor(Side.Bottom, 1);
		_dialogBar.SetOffset(Side.Left, 0);
		_dialogBar.SetOffset(Side.Top, -110);
		_dialogBar.SetOffset(Side.Right, 0);
		_dialogBar.SetOffset(Side.Bottom, 0);
		_dialogBar.GrowHorizontal = Control.GrowDirection.Both;
		_dialogBar.GrowVertical = Control.GrowDirection.Begin;
		AddChild(_dialogBar);

		// Themed panel background — uses MenuPanel style (panel_9slice.png, dark/gold)
		_dialogPanel = new PanelContainer
		{
			ThemeTypeVariation = "MenuPanel",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_dialogPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_dialogBar.AddChild(_dialogPanel);

		// Horizontal split: portrait | text
		_contentBox = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		_contentBox.AddThemeConstantOverride("separation", 8);
		_dialogPanel.AddChild(_contentBox);

		// --- Portrait side (optional, hidden by default) ---
		_portraitContainer = new Control
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(PORTRAIT_FRAME_WIDTH, PORTRAIT_SIZE)
		};
		_portraitContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		_contentBox.AddChild(_portraitContainer);

		// Frame around the portrait (panel_highlight.png = golden-bordered panel)
		_portraitFrame = new NinePatchRect
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Texture = _panelHighlightTexture,
			RegionRect = new Rect2(0, 0, 48, 48),
			PatchMarginLeft = 6,
			PatchMarginTop = 6,
			PatchMarginRight = 6,
			PatchMarginBottom = 6
		};
		_portraitFrame.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_portraitContainer.AddChild(_portraitFrame);

		// The actual portrait image — fills the frame with 6px inset from the border
		_portraitTexture = new TextureRect
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			CustomMinimumSize = new Vector2(PORTRAIT_SIZE, PORTRAIT_SIZE)
		};
		_portraitTexture.SetAnchor(Side.Left, 0);
		_portraitTexture.SetAnchor(Side.Top, 0);
		_portraitTexture.SetAnchor(Side.Right, 1);
		_portraitTexture.SetAnchor(Side.Bottom, 1);
		_portraitTexture.SetOffset(Side.Left, 6);
		_portraitTexture.SetOffset(Side.Top, 6);
		_portraitTexture.SetOffset(Side.Right, -6);
		_portraitTexture.SetOffset(Side.Bottom, -6);
		_portraitContainer.AddChild(_portraitTexture);

		// --- Text side ---
		_textMargin = new MarginContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		_textMargin.AddThemeConstantOverride("margin_left", 4);
		_textMargin.AddThemeConstantOverride("margin_top", 4);
		_textMargin.AddThemeConstantOverride("margin_right", 36);
		_textMargin.AddThemeConstantOverride("margin_bottom", 4);
		_contentBox.AddChild(_textMargin);

		_textLabel = new Label
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MaxLinesVisible = MAX_VISIBLE_LINES,
			ThemeTypeVariation = "MenuLabel",
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center
		};
		_textLabel.AddThemeColorOverride("font_color", new Color(0.83137256f, 0.76862746f, 0.627451f, 1f));
		_textLabel.AddThemeFontSizeOverride("font_size", 12);
		_textMargin.AddChild(_textLabel);

		// --- Page arrow (bottom-right) ---
		_pageArrow = new Control
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_pageArrow.SetAnchorsPreset(Control.LayoutPreset.RightWide);
		_pageArrow.SetOffset(Side.Left, -32);
		_pageArrow.SetOffset(Side.Top, -24);
		_pageArrow.SetOffset(Side.Right, -12);
		_pageArrow.SetOffset(Side.Bottom, -8);
		_dialogBar.AddChild(_pageArrow);

		_arrowSprite = new Sprite2D
		{
			Texture = _arrowTexture,
			Scale = new Vector2(0.6655f, 0.625f)
		};
		_pageArrow.AddChild(_arrowSprite);
	}

	void BuildNamePlate()
	{
		_namePlate = new Control
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_namePlate.Position = new Vector2(16, -40);
		_namePlate.SetSize(new Vector2(200, 28));
		_dialogBar.AddChild(_namePlate);

		_namePlateBg = new NinePatchRect
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Texture = _panelTexture,
			RegionRect = new Rect2(0, 0, 48, 48),
			PatchMarginLeft = 8,
			PatchMarginTop = 8,
			PatchMarginRight = 8,
			PatchMarginBottom = 8
		};
		_namePlateBg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_namePlate.AddChild(_namePlateBg);

		_nameLabel = new Label
		{
			Position = new Vector2(12, 4),
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "MenuLabel"
		};
		_nameLabel.SetSize(new Vector2(176, 20));
		_nameLabel.AddThemeColorOverride("font_color", new Color(0.83137256f, 0.76862746f, 0.627451f, 1f));
		_nameLabel.AddThemeFontSizeOverride("font_size", 10);
		_namePlate.AddChild(_nameLabel);
	}

	// ================================================================
	//  Public API
	// ================================================================

	public void DisplayText(string text, DialogVoiceResource voice)
	{
		GameLogger.Debug("Dialog", $"DisplayText: speaker='{voice?.SpeakerName}', len={text.Length}, pages pending");
		_voice = voice;

		string speaker = voice?.SpeakerName ?? "";
		_nameLabel.Text = speaker;

		bool showPortrait = voice?.Portrait != null;
		_portraitContainer.Visible = showPortrait;
		if (showPortrait)
		{
			_portraitTexture.Texture = voice.Portrait;
		}

		bool showName = !string.IsNullOrEmpty(speaker);
		if (showName && !_namePlate.Visible)
		{
			_namePlate.Position = new Vector2(16, -56);
			_namePlate.Visible = true;
			var tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(_namePlate, "position", new Vector2(16, -40), 0.25f);
		}
		else
		{
			_namePlate.Visible = showName;
		}

		// Narrator styling
		bool isNarrator = speaker == "Narrator";
		if (isNarrator)
		{
			_textLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.6f, 0.45f, 1f));
		}
		else
		{
			_textLabel.AddThemeColorOverride("font_color", new Color(0.83137256f, 0.76862746f, 0.627451f, 1f));
		}

		// Append to persistent dialog log
		DialogLog.AppendLine(voice?.SpeakerName, text);

		var segments = DialogTagParser.Parse(text);
		BuildCharData(segments);
		BuildPages();

		if (_pages.Count > 0)
			StartPage(0);
	}

	// ================================================================
	//  Typewriter
	// ================================================================

	void BuildCharData(List<TextSegment> segments)
	{
		var sb = new StringBuilder();
		_charPauses.Clear();
		_charCps.Clear();

		foreach (var seg in segments)
		{
			for (int j = 0; j < seg.Text.Length; j++)
			{
				sb.Append(seg.Text[j]);
				_charPauses.Add(j == 0 ? seg.PauseBefore : 0f);
				_charCps.Add(seg.CpsOverride);
			}
		}
		_displayText = sb.ToString();
	}

	void BuildPages()
	{
		_pages.Clear();
		int pos = 0;
		while (pos < _displayText.Length)
		{
			int end = FindPageEnd(pos);
			if (end <= pos) break;
			_pages.Add(new Page { Start = pos, End = end });
			pos = end;
		}
	}

	float GetBoxWidth()
	{
		return _voice?.Portrait != null ? BOX_WIDTH_WITH_PORTRAIT : FULL_BOX_WIDTH;
	}

	int FindPageEnd(int start)
	{
		int pos = start;
		int lines = 0;
		var font = _yosterFont;
		float boxWidth = GetBoxWidth();
		while (pos < _displayText.Length && lines < MAX_VISIBLE_LINES)
		{
			int remaining = _displayText.Length - pos;
			int lineLen;
			if (font == null)
			{
				lineLen = Mathf.Min(remaining, 60);
			}
			else
			{
				int newlineIdx = _displayText.IndexOf('\n', pos);
				if (newlineIdx >= 0)
				{
					int len = newlineIdx - pos;
					if (len > 0 && font.GetStringSize(_displayText.Substring(pos, len), fontSize: 12).X <= boxWidth)
					{
						lineLen = len + 1;
						pos += lineLen;
						lines++;
						continue;
					}
					if (len <= 0)
					{
						lineLen = 1;
						pos += lineLen;
						lines++;
						continue;
					}
				}

				int lo = 0, hi = remaining;
				while (lo < hi)
				{
					int mid = (lo + hi + 1) / 2;
					if (font.GetStringSize(_displayText.Substring(pos, mid), fontSize: 12).X <= boxWidth)
						lo = mid;
					else
						hi = mid - 1;
				}

				if (lo >= remaining) { lineLen = remaining; }
				else
				{
					int breakPos = lo;
					while (breakPos > 0 && _displayText[pos + breakPos] != ' ')
						breakPos--;
					lineLen = breakPos > 0 ? breakPos : lo;
				}
			}
			pos += lineLen;
			lines++;
		}
		return pos;
	}

	void StartPage(int index)
	{
		_pageIndex = index;
		_visibleCharCount = 0;
		_charAccumulator = 0f;
		_pendingPause = 0f;
		_pageArrow.Visible = false;

		var page = _pages[index];
		_textLabel.Text = _displayText.Substring(page.Start, page.End - page.Start);
		_textLabel.VisibleCharacters = 0;
		_currentCps = GetGlobalSpeedCps();
		_state = State.Typing;
		GameLogger.Debug("Dialog", $"StartPage: page {index}/{_pages.Count} — chars [{_pages[index].Start}..{_pages[index].End})");
	}

	bool ShowNextChar()
	{
		var page = _pages[_pageIndex];
		int globalIdx = page.Start + _visibleCharCount;

		if (globalIdx >= page.End)
		{
			_state = State.PageComplete;
			_pageArrow.Visible = true;
			GameLogger.Debug("Dialog", $"Page {_pageIndex} complete — waiting for advance");
			return false;
		}

		if (_charPauses[globalIdx] > 0f)
		{
			_pendingPause = _charPauses[globalIdx];
			_charPauses[globalIdx] = 0f;
			_charAccumulator = 0f;
			return false;
		}

		if (_charCps[globalIdx] > 0f)
			_currentCps = _charCps[globalIdx];
		else
			_currentCps = GetGlobalSpeedCps();

		char c = _displayText[globalIdx];
		_visibleCharCount++;
		_textLabel.VisibleCharacters = _visibleCharCount;

		if (!char.IsWhiteSpace(c))
			PlayBlip(c);

		if (IsPunctuation(c))
		{
			_pendingPause = PunctuationPause(c);
			_charAccumulator = 0f;
			return false;
		}

		return true;
	}

	void SnapToEnd()
	{
		_state = State.Idle;
		_charAccumulator = 0f;
		_pendingPause = 0f;

		var page = _pages[_pageIndex];
		_visibleCharCount = page.End - page.Start;
		_textLabel.VisibleCharacters = _visibleCharCount;

		_state = State.PageComplete;
		_pageArrow.Visible = true;
	}

	void AdvancePage()
	{
		_pageIndex++;
		if (_pageIndex >= _pages.Count)
		{
			if (_chime != null)
				AudioManager.Instance.PlaySfx(_chime, -6f);
			EmitSignal(SignalName.LineComplete);
			return;
		}
		if (_chime != null)
			AudioManager.Instance.PlaySfx(_chime, -6f);
		StartPage(_pageIndex);
	}

	// ================================================================
	//  Audio — one-shot blips with _Process cleanup
	// ================================================================

	void PlayBlip(char c)
	{
		if (_voice == null) return;
		if (_activeBlips.Count >= MAX_ACTIVE_BLIPS) return;

		float pitch = CalculatePitch(c);
		float volDb = _voice.VolumeDb;
		volDb += (float)GD.RandRange(-_voice.VolumeVariance, _voice.VolumeVariance);

		if (DialogVoiceResource.IsIntonation(c))
			volDb += 3f;

		var p = new AudioStreamPlayer
		{
			Stream = _voice.GetBlipStream(),
			PitchScale = pitch,
			VolumeDb = volDb,
			Bus = "SFX"
		};
		AddChild(p);
		_activeBlips.Add(new ActiveBlip { Player = p, StartTime = Time.GetTicksMsec() / 1000.0 });
		p.Play(_voice.StartOffset);
	}

	float CalculatePitch(char c)
	{
		float mul = _voice.GetVowelPitch(c);
		if (mul > 0f)
			return _voice.BasePitch * mul;
		if (DialogVoiceResource.IsIntonation(c))
			return _voice.BasePitch * _voice.GetPunctuationPitch(c);
		return _voice.BasePitch + (float)GD.RandRange(-_voice.ConsonantPitchVariance, _voice.ConsonantPitchVariance);
	}

	static bool IsPunctuation(char c) => c is '!' or '.' or ',' or '?' or ';' or ':';

	static float PunctuationPause(char c) => c switch
	{
		'.' => PAUSE_PERIOD,
		'?' => PAUSE_QMARK,
		'!' => PAUSE_EXCLAM,
		',' => PAUSE_COMMA,
		_ => 0f
	};

	static float GetGlobalSpeedCps()
	{
		return DialogManager.CurrentTextSpeed switch
		{
			DialogManager.TextSpeed.Instant => 1_000_000f,
			DialogManager.TextSpeed.Fast => FAST_CPS,
			_ => NORMAL_CPS
		};
	}

	// ================================================================
	//  Input / process
	// ================================================================

	public override void _Input(InputEvent @event)
	{
		if (!@event.IsActionPressed("interact")) return;

		double now = Time.GetTicksMsec() / 1000.0;
		if (now - _lastAdvanceTime < ADVANCE_COOLDOWN) return;

		switch (_state)
		{
			case State.Typing:
				SnapToEnd();
				break;
			case State.PageComplete:
				AdvancePage();
				break;
		}

		_lastAdvanceTime = now;
		GetViewport().SetInputAsHandled();
	}

	public override void _Process(double delta)
	{
		double now = Time.GetTicksMsec() / 1000.0;
		float blipDuration = _voice?.BlipDuration ?? 0.08f;
		for (int i = _activeBlips.Count - 1; i >= 0; i--)
		{
			var ab = _activeBlips[i];
			if (now - ab.StartTime >= blipDuration)
			{
				if (GodotObject.IsInstanceValid(ab.Player))
				{
					ab.Player.Stop();
					ab.Player.QueueFree();
				}
				_activeBlips.RemoveAt(i);
			}
		}

		if (_pageArrow.Visible)
		{
			float t = (float)Time.GetTicksMsec() / 1000f;
			_arrowSprite.Position = new Vector2(0, Mathf.Sin(t * 7f) * 4f);
			float pulse = 1f + Mathf.Sin(t * 3f) * 0.06f;
			_arrowSprite.Scale = new Vector2(pulse * 0.6655f, pulse * 0.625f);
		}

		if (_state != State.Typing) return;

		if (_pendingPause > 0f)
		{
			_pendingPause -= (float)delta;
			if (_pendingPause > 0f) return;
			_pendingPause = 0f;
		}

		_charAccumulator += _currentCps * (float)delta;

		while (_charAccumulator >= 1f && _state == State.Typing)
		{
			_charAccumulator -= 1f;
			if (!ShowNextChar()) break;
		}
	}
}
