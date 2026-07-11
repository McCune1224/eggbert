using Godot;
using System.Collections.Generic;

public partial class TextBox : Control
{
    const int MAX_VISIBLE_LINES = 3;
    const float BOX_WIDTH = 580f;
    const float NORMAL_CPS = 40f;
    const float FAST_CPS = 80f;
    const float SLOW_CPS = 20f;

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

    enum TextBoxState { Idle, Typing, PageComplete }

    TextBoxState _state = TextBoxState.Idle;

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

    Label _textLabel;
    Control _namePlate;
    Label _nameLabel;
    Control _pageArrow;

    Font _font;
    int _fontSize = 12;

    [Signal]
    public delegate void LineCompleteEventHandler();

    static TextBox()
    {
        _systemVoice = ResourceLoader.Load<AudioStream>("res://assets/audio/sfx/meep.mp3");
    }

    public override void _Ready()
    {
        _textLabel = GetNode<Label>("TextContainer/TextLabel");
        _namePlate = GetNode<Control>("NamePlate");
        _nameLabel = GetNode<Label>("NamePlate/NameLabel");
        _pageArrow = GetNode<Control>("PageArrow");

        _voicePlayers[0] = GetNode<AudioStreamPlayer>("VoicePlayer0");
        _voicePlayers[0].Bus = "SFX";
        for (int i = 1; i < 3; i++)
        {
            _voicePlayers[i] = new AudioStreamPlayer();
            _voicePlayers[i].Bus = "SFX";
            AddChild(_voicePlayers[i]);
        }

        _font = _textLabel.Get("theme_override_fonts/font").As<Font>();
        if (_font == null)
            _font = _textLabel.GetThemeDefaultFont();
    }

    public override void _Process(double delta)
    {
        if (_pageArrow.Visible)
        {
            float t = (float)Time.GetTicksMsec() / 1000f;
            float arrowY = Mathf.Sin(t * 7f) * 3f;
            _pageArrow.GetNode<Sprite2D>("ArrowSprite").Position = new Vector2(0, arrowY);
        }

        if (_state != TextBoxState.Typing) return;

        float speedMul = Input.IsActionPressed("advance_dialog") ? 4f : 1f;

        if (_pendingPause > 0f)
        {
            _pendingPause -= (float)delta * speedMul;
            if (_pendingPause > 0f) return;
            _pendingPause = 0f;
        }

        Page page = _pages[_pageIndex];
        int globalNext = page.Start + _visibleCharCount;
        if (globalNext < _displayText.Length)
        {
            if (_charCps[globalNext] > 0f)
                _currentCps = _charCps[globalNext];
            else
                _currentCps = GetGlobalSpeedCps();
        }

        float effectiveCps = _currentCps * speedMul;
        _charAccumulator += effectiveCps * (float)delta;

        while (_charAccumulator >= 1f && _state == TextBoxState.Typing)
        {
            _charAccumulator -= 1f;
            if (!ShowNextChar()) break;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("advance_dialog")) return;

        switch (_state)
        {
            case TextBoxState.Typing:
                SnapToEnd();
                break;
            case TextBoxState.PageComplete:
                AdvancePage();
                break;
        }
    }

    public void PlayText(string line, DialogVoice voice)
    {
        _voiceStream = voice?.BlipStream ?? _systemVoice;
        _voiceBasePitch = voice?.BasePitch ?? 1f;

        foreach (var p in _voicePlayers)
            p.Stream = _voiceStream;

        if (!string.IsNullOrEmpty(voice?.SpeakerName))
        {
            _nameLabel.Text = voice.SpeakerName;
            _namePlate.Visible = true;
        }
        else
        {
            _namePlate.Visible = false;
        }

        var segments = DialogTagParser.Parse(line);
        BuildChars(segments);
        BuildPages();

        if (_pages.Count == 0) return;

        StartPage(0);
    }

    void BuildChars(List<TextSegment> segments)
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
            int end = FindPageEnd(pos, MAX_VISIBLE_LINES);
            if (end <= pos) break;
            _pages.Add(new Page { Start = pos, End = end });
            pos = end;
        }
    }

    int FindPageEnd(int start, int maxLines)
    {
        int pos = start;
        int lines = 0;

        while (pos < _displayText.Length && lines < maxLines)
        {
            int lineLen = FindFitLength(pos);
            pos += lineLen;
            lines++;
        }

        return pos;
    }

    int FindFitLength(int startIndex)
    {
        int remaining = _displayText.Length - startIndex;

        if (_font == null)
            return Mathf.Min(remaining, 60);

        int newlineIdx = _displayText.IndexOf('\n', startIndex);
        if (newlineIdx >= 0)
        {
            int lenToNewline = newlineIdx - startIndex;
            if (lenToNewline > 0)
            {
                string sub = _displayText.Substring(startIndex, lenToNewline);
                float w = _font.GetStringSize(sub, fontSize: _fontSize).X;
                if (w <= BOX_WIDTH)
                    return lenToNewline + 1;
            }
            else
            {
                return 1;
            }
        }

        int lo = 0, hi = remaining;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            string sub = _displayText.Substring(startIndex, mid);
            if (_font.GetStringSize(sub, fontSize: _fontSize).X <= BOX_WIDTH)
                lo = mid;
            else
                hi = mid - 1;
        }

        if (lo >= remaining) return remaining;

        int breakPos = lo;
        while (breakPos > 0 && _displayText[startIndex + breakPos] != ' ')
            breakPos--;

        return breakPos > 0 ? breakPos : lo;
    }

    void StartPage(int pageIndex)
    {
        _pageIndex = pageIndex;
        _visibleCharCount = 0;
        _charAccumulator = 0f;
        _pendingPause = 0f;
        _pageArrow.Visible = false;

        Page page = _pages[pageIndex];
        string pageText = _displayText.Substring(page.Start, page.End - page.Start);
        _textLabel.Text = pageText;
        _textLabel.VisibleCharacters = 0;

        _textLabel.MaxLinesVisible = MAX_VISIBLE_LINES;

        _currentCps = GetGlobalSpeedCps();

        _state = TextBoxState.Typing;
    }

    bool ShowNextChar()
    {
        Page page = _pages[_pageIndex];
        int globalIdx = page.Start + _visibleCharCount;

        if (globalIdx >= page.End)
        {
            OnPageComplete();
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

        char c = _displayText[globalIdx];
        _visibleCharCount++;
        _textLabel.VisibleCharacters = _visibleCharCount;

        PlayBlip(c);

        if (IsPunctuation(c))
        {
            _pendingPause = GetPunctuationPause(c);
            _charAccumulator = 0f;
            return false;
        }

        return true;
    }

    void SnapToEnd()
    {
        _state = TextBoxState.Idle;
        _charAccumulator = 0f;
        _pendingPause = 0f;

        Page page = _pages[_pageIndex];
        _visibleCharCount = page.End - page.Start;
        _textLabel.VisibleCharacters = _visibleCharCount;

        _state = TextBoxState.PageComplete;
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

    void OnPageComplete()
    {
        _state = TextBoxState.PageComplete;
        _pageArrow.Visible = true;
    }

    void PlayBlip(char c)
    {
        if (_voiceStream == null) return;

        if (c == ' ' || char.IsWhiteSpace(c))
            return;

        int idx = _voicePlayerIndex;
        _voicePlayerIndex = (_voicePlayerIndex + 1) % 3;

        var p = _voicePlayers[idx];

        float vowelMul = GetVowelMultiplier(c);
        if (vowelMul > 0f)
        {
            p.PitchScale = _voiceBasePitch * vowelMul;
        }
        else if (IsIntonationPunctuation(c))
        {
            p.PitchScale = _voiceBasePitch * GetPunctuationPitchMul(c);
        }
        else
        {
            p.PitchScale = _voiceBasePitch + (float)GD.RandRange(-0.05f, 0.05f);
        }

        p.Play(0);
    }

    static float GetVowelMultiplier(char c)
    {
        switch (c)
        {
            case 'a': case 'A': return VOWEL_A;
            case 'e': case 'E': return VOWEL_E;
            case 'i': case 'I': return VOWEL_I;
            case 'o': case 'O': return VOWEL_O;
            case 'u': case 'U': return VOWEL_U;
            case 'y': case 'Y': return VOWEL_Y;
            default: return 0f;
        }
    }

    static bool IsIntonationPunctuation(char c) => c == '?' || c == '!' || c == '.';

    static float GetPunctuationPitchMul(char c) => c switch
    {
        '?' => PITCH_QMARK,
        '!' => PITCH_EXCLAM,
        '.' => PITCH_PERIOD,
        _ => 1f
    };

    static bool IsPunctuation(char c) => c == '!' || c == '.' || c == ',' || c == '?' || c == ';' || c == ':';

    static float GetPunctuationPause(char c) => c switch
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
}
