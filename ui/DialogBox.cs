using Godot;
using System.Collections.Generic;

public partial class DialogBox : Control
{
    const int MAX_VISIBLE_LINES = 3;
    const float BOX_WIDTH = 580f;
    const float NORMAL_CPS = 40f;
    const float FAST_CPS = 80f;

    const float VOWEL_A = 1.00f;
    const float VOWEL_E = 1.10f;
    const float VOWEL_I = 1.20f;
    const float VOWEL_O = 0.90f;
    const float VOWEL_U = 0.85f;
    const float VOWEL_Y = 1.05f;

    const float PAUSE_PERIOD = 0.20f;
    const float PAUSE_QMARK = 0.15f;
    const float PAUSE_EXCLAM = 0.15f;
    const float PAUSE_COMMA = 0.12f;

    const float PITCH_QMARK = 1.30f;
    const float PITCH_EXCLAM = 1.20f;
    const float PITCH_PERIOD = 0.70f;

    enum State { Idle, Typing, PageComplete }

    State _state = State.Idle;
    string _displayText = "";
    List<float> _charPauses = new();
    List<float> _charCps = new();

    struct Page { public int Start; public int End; }
    List<Page> _pages = new();
    int _pageIndex = 0;
    int _visibleCharCount = 0;

    float _currentCps = NORMAL_CPS;
    float _charAccumulator = 0f;
    float _pendingPause = 0f;

    AudioStreamPlayer[] _voicePlayers = new AudioStreamPlayer[3];
    int _voicePlayerIndex = 0;
    AudioStream _voiceStream;
    float _voiceBasePitch = 1f;

    static AudioStream _systemVoice;
    static Font _yosterFont;
    static Texture2D _chatboxTexture;
    static Texture2D _arrowTexture;

    Label _textLabel;
    Control _namePlate;
    Label _nameLabel;
    Control _pageArrow;
    Sprite2D _arrowSprite;

    [Signal]
    public delegate void LineCompleteEventHandler();

    static DialogBox()
    {
        _systemVoice = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");
        _yosterFont = ResourceLoader.Load<Font>("res://assets/fonts/yoster.ttf");
        _chatboxTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/chatbox.png");
        _arrowTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/Arrow.png");
    }

    public override void _Ready()
    {
        // Layout: bottom bar, full width, 100 px tall
        AnchorLeft = 0;
        AnchorTop = 1;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetLeft = 0;
        OffsetTop = -100;
        OffsetRight = 0;
        OffsetBottom = 0;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Begin;
        MouseFilter = MouseFilterEnum.Ignore;

        BuildBackground();
        BuildNamePlate();
        BuildTextContainer();
        BuildPageArrow();
        BuildVoicePlayers();
    }

    void BuildBackground()
    {
        var bg = new NinePatchRect
        {
            Name = "Background",
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = _chatboxTexture,
            RegionRect = new Rect2(0, 0, 48, 48),
            PatchMarginLeft = 16,
            PatchMarginTop = 16,
            PatchMarginRight = 16,
            PatchMarginBottom = 16
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);
    }

    void BuildNamePlate()
    {
        _namePlate = new Control
        {
            Name = "NamePlate",
            Position = new Vector2(16, -28),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _namePlate.SetSize(new Vector2(200, 28));

        var bg = new NinePatchRect
        {
            Name = "NamePlateBg",
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = _chatboxTexture,
            RegionRect = new Rect2(0, 0, 48, 48),
            PatchMarginLeft = 8,
            PatchMarginTop = 8,
            PatchMarginRight = 8,
            PatchMarginBottom = 8
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        _namePlate.AddChild(bg);

        _nameLabel = new Label
        {
            Name = "NameLabel",
            Position = new Vector2(12, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        _nameLabel.SetSize(new Vector2(176, 20));
        _nameLabel.AddThemeColorOverride("font_color", new Color(0, 0, 0));
        _nameLabel.AddThemeFontOverride("font", _yosterFont);
        _nameLabel.AddThemeFontSizeOverride("font_size", 10);
        _namePlate.AddChild(_nameLabel);

        AddChild(_namePlate);
    }

    void BuildTextContainer()
    {
        var container = new MarginContainer
        {
            Name = "TextContainer",
            MouseFilter = MouseFilterEnum.Ignore
        };
        container.AddThemeConstantOverride("margin_left", 16);
        container.AddThemeConstantOverride("margin_top", 12);
        container.AddThemeConstantOverride("margin_right", 48);
        container.AddThemeConstantOverride("margin_bottom", 12);
        container.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(container);

        _textLabel = new Label
        {
            Name = "TextLabel",
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.Arbitrary,
            MaxLinesVisible = MAX_VISIBLE_LINES
        };
        _textLabel.AddThemeColorOverride("font_color", new Color(0, 0, 0));
        _textLabel.AddThemeFontOverride("font", _yosterFont);
        _textLabel.AddThemeFontSizeOverride("font_size", 12);
        container.AddChild(_textLabel);
    }

    void BuildPageArrow()
    {
        _pageArrow = new Control
        {
            Name = "PageArrow",
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _pageArrow.SetAnchorsPreset(LayoutPreset.RightWide);

        _arrowSprite = new Sprite2D
        {
            Name = "ArrowSprite",
            Texture = _arrowTexture,
            Scale = new Vector2(0.6655f, 0.625f)
        };
        _pageArrow.AddChild(_arrowSprite);

        AddChild(_pageArrow);
    }

    void BuildVoicePlayers()
    {
        for (int i = 0; i < 3; i++)
        {
            var player = new AudioStreamPlayer
            {
                Name = $"VoicePlayer{i}",
                Bus = "SFX"
            };
            AddChild(player);
            _voicePlayers[i] = player;
        }
    }

    // ================================================================
    //  Typewriter engine
    // ================================================================

    public void DisplayText(string text, DialogVoice voice)
    {
        _voiceStream = voice?.BlipStream ?? _systemVoice;
        _voiceBasePitch = voice?.BasePitch ?? 1f;

        foreach (var p in _voicePlayers)
            p.Stream = _voiceStream;

        _nameLabel.Text = voice?.SpeakerName ?? "";
        _namePlate.Visible = !string.IsNullOrEmpty(voice?.SpeakerName);

        var segments = DialogTagParser.Parse(text);
        BuildCharData(segments);
        BuildPages();

        if (_pages.Count > 0)
            StartPage(0);
    }

    void BuildCharData(List<TextSegment> segments)
    {
        _displayText = "";
        _charPauses.Clear();
        _charCps.Clear();

        foreach (var seg in segments)
        {
            for (int j = 0; j < seg.Text.Length; j++)
            {
                _displayText += seg.Text[j];
                _charPauses.Add(j == 0 ? seg.PauseBefore : 0f);
                _charCps.Add(seg.CpsOverride);
            }
        }
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

    int FindPageEnd(int start)
    {
        int pos = start;
        int lines = 0;
        while (pos < _displayText.Length && lines < MAX_VISIBLE_LINES)
        {
            int len = FindLineWidth(pos);
            pos += len;
            lines++;
        }
        return pos;
    }

    int FindLineWidth(int start)
    {
        int remaining = _displayText.Length - start;
        var font = _yosterFont;
        if (font == null)
            return Mathf.Min(remaining, 60);

        int newlineIdx = _displayText.IndexOf('\n', start);
        if (newlineIdx >= 0)
        {
            int len = newlineIdx - start;
            if (len > 0 && font.GetStringSize(_displayText.Substring(start, len), fontSize: 12).X <= BOX_WIDTH)
                return len + 1;
            if (len <= 0)
                return 1;
        }

        int lo = 0, hi = remaining;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (font.GetStringSize(_displayText.Substring(start, mid), fontSize: 12).X <= BOX_WIDTH)
                lo = mid;
            else
                hi = mid - 1;
        }

        if (lo >= remaining) return remaining;
        int breakPos = lo;
        while (breakPos > 0 && _displayText[start + breakPos] != ' ')
            breakPos--;
        return breakPos > 0 ? breakPos : lo;
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
    }

    bool ShowNextChar()
    {
        var page = _pages[_pageIndex];
        int globalIdx = page.Start + _visibleCharCount;

        if (globalIdx >= page.End)
        {
            _state = State.PageComplete;
            _pageArrow.Visible = true;
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
            EmitSignal(SignalName.LineComplete);
            return;
        }
        StartPage(_pageIndex);
    }

    // ================================================================
    //  Audio chirps
    // ================================================================

    void PlayBlip(char c)
    {
        if (_voiceStream == null) return;
        if (char.IsWhiteSpace(c)) return;

        var p = _voicePlayers[_voicePlayerIndex];
        _voicePlayerIndex = (_voicePlayerIndex + 1) % 3;

        float mul = VowelMultiplier(c);
        if (mul > 0f)
            p.PitchScale = _voiceBasePitch * mul;
        else if (IsIntonation(c))
            p.PitchScale = _voiceBasePitch * IntonationPitch(c);
        else
            p.PitchScale = _voiceBasePitch + (float)GD.RandRange(-0.05f, 0.05f);

        p.Play(0f);
    }

    static float VowelMultiplier(char c) => c switch
    {
        'a' or 'A' => VOWEL_A,
        'e' or 'E' => VOWEL_E,
        'i' or 'I' => VOWEL_I,
        'o' or 'O' => VOWEL_O,
        'u' or 'U' => VOWEL_U,
        'y' or 'Y' => VOWEL_Y,
        _ => 0f
    };

    static bool IsIntonation(char c) => c is '?' or '!' or '.';

    static float IntonationPitch(char c) => c switch
    {
        '?' => PITCH_QMARK,
        '!' => PITCH_EXCLAM,
        '.' => PITCH_PERIOD,
        _ => 1f
    };

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
        if (!@event.IsActionPressed("advance_dialog")) return;

        switch (_state)
        {
            case State.Typing:
                SnapToEnd();
                break;
            case State.PageComplete:
                AdvancePage();
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (_pageArrow.Visible)
        {
            float t = (float)Time.GetTicksMsec() / 1000f;
            _arrowSprite.Position = new Vector2(0, Mathf.Sin(t * 7f) * 3f);
        }

        if (_state != State.Typing) return;

        float speedMul = Input.IsActionPressed("advance_dialog") ? 4f : 1f;

        if (_pendingPause > 0f)
        {
            _pendingPause -= (float)delta * speedMul;
            if (_pendingPause > 0f) return;
            _pendingPause = 0f;
        }

        float effectiveCps = _currentCps * speedMul;
        _charAccumulator += effectiveCps * (float)delta;

        while (_charAccumulator >= 1f && _state == State.Typing)
        {
            _charAccumulator -= 1f;
            if (!ShowNextChar()) break;
        }
    }
}
