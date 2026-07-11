using Godot;
using System.Collections.Generic;

public partial class DialogBubble : CanvasLayer
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

    Control _dialogBar;
    Control _namePlate;
    Label _nameLabel;
    Label _textLabel;
    Control _pageArrow;
    Sprite2D _arrowSprite;

    [Signal]
    public delegate void LineCompleteEventHandler();

    static DialogBubble()
    {
        _systemVoice = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");
        _yosterFont = ResourceLoader.Load<Font>("res://assets/fonts/yoster.ttf");
        _chatboxTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/chatbox.png");
        _arrowTexture = ResourceLoader.Load<Texture2D>("res://assets/ui/Arrow.png");
    }

    public override void _Ready()
    {
        Layer = 128;

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        BuildDialogBar(root);
        BuildNamePlate(root);
        BuildVoicePlayers();
    }

    void BuildDialogBar(Control root)
    {
        _dialogBar = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _dialogBar.SetAnchor(Side.Left, 0);
        _dialogBar.SetAnchor(Side.Top, 1);
        _dialogBar.SetAnchor(Side.Right, 1);
        _dialogBar.SetAnchor(Side.Bottom, 1);
        _dialogBar.SetOffset(Side.Left, 0);
        _dialogBar.SetOffset(Side.Top, -100);
        _dialogBar.SetOffset(Side.Right, 0);
        _dialogBar.SetOffset(Side.Bottom, 0);
        _dialogBar.GrowHorizontal = Control.GrowDirection.Both;
        _dialogBar.GrowVertical = Control.GrowDirection.Begin;
        root.AddChild(_dialogBar);

        var bg = new NinePatchRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = _chatboxTexture,
            RegionRect = new Rect2(0, 0, 48, 48),
            PatchMarginLeft = 16,
            PatchMarginTop = 16,
            PatchMarginRight = 16,
            PatchMarginBottom = 16
        };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _dialogBar.AddChild(bg);

        var container = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        container.AddThemeConstantOverride("margin_left", 16);
        container.AddThemeConstantOverride("margin_top", 12);
        container.AddThemeConstantOverride("margin_right", 48);
        container.AddThemeConstantOverride("margin_bottom", 12);
        container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _dialogBar.AddChild(container);

        _textLabel = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.Arbitrary,
            MaxLinesVisible = MAX_VISIBLE_LINES
        };
        _textLabel.AddThemeColorOverride("font_color", new Color(0, 0, 0));
        _textLabel.AddThemeFontOverride("font", _yosterFont);
        _textLabel.AddThemeFontSizeOverride("font_size", 12);
        container.AddChild(_textLabel);

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

    void BuildNamePlate(Control root)
    {
        _namePlate = new Control
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _namePlate.SetPosition(new Vector2(16, -28));
        _namePlate.SetSize(new Vector2(200, 28));
        root.AddChild(_namePlate);

        var bg = new NinePatchRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = _chatboxTexture,
            RegionRect = new Rect2(0, 0, 48, 48),
            PatchMarginLeft = 8,
            PatchMarginTop = 8,
            PatchMarginRight = 8,
            PatchMarginBottom = 8
        };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _namePlate.AddChild(bg);

        _nameLabel = new Label
        {
            Position = new Vector2(12, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        _nameLabel.SetSize(new Vector2(176, 20));
        _nameLabel.AddThemeColorOverride("font_color", new Color(0, 0, 0));
        _nameLabel.AddThemeFontOverride("font", _yosterFont);
        _nameLabel.AddThemeFontSizeOverride("font_size", 10);
        _namePlate.AddChild(_nameLabel);
    }

    void BuildVoicePlayers()
    {
        for (int i = 0; i < 3; i++)
        {
            var player = new AudioStreamPlayer
            {
                Bus = "SFX"
            };
            AddChild(player);
            _voicePlayers[i] = player;
        }
    }

    // ================================================================
    //  Public API
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

    // ================================================================
    //  Typewriter
    // ================================================================

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
        var font = _yosterFont;
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
                    if (len > 0 && font.GetStringSize(_displayText.Substring(pos, len), fontSize: 12).X <= BOX_WIDTH)
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
                    if (font.GetStringSize(_displayText.Substring(pos, mid), fontSize: 12).X <= BOX_WIDTH)
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
    //  Audio
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
