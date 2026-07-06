
using Godot;
using System;
using System.Collections.Generic;

public partial class TextBox : MarginContainer
{
    const int MAX_WIDTH = 256;
    string CurrentText = "";
    int LetterIndex = 0;
    List<char> highPitchLetters = new() { 'a', 'e', 'i', 'o', 'u' };

    // float LetterTime = 0.04f;
    // float SpaceTime = 0.06f;
    // float PunctuationTime = 0.25f;
    bool _playingText = false;
    float BaseLetterTime = 0.04f;
    float BaseSpaceTime = 0.06f;
    float BasePunctuationTime = 0.20f;

    Label _label;
    Timer _letterDisplayTimer;
    Control _nextIndicator;
    AudioStreamPlayer _audioPlayer;

    [Signal]
    public delegate void FinishedDisplayingEventHandler();

    public override void _Ready()
    {
        _label = this.GetNode<Label>("MarginContainer/Label");

        _letterDisplayTimer = this.GetNode<Timer>("LetterDisplayTimer");
        _letterDisplayTimer.Timeout += DisplayLetter;

        _nextIndicator = this.GetNode<Control>("NinePatchRect/Control");

        _audioPlayer = this.GetNode<AudioStreamPlayer>("AudioStreamPlayer");

    }

    public override void _Process(double delta)
    {
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("advance_dialog") && _playingText)
        {
            BaseLetterTime /= 2;
            BaseSpaceTime /= 2;
            BasePunctuationTime /= 2;
        }
    }

    public void PlayText(string desiredText, AudioStream sfx)
    {
        CurrentText = desiredText;
        _label.Text = desiredText;
        _audioPlayer.Stream = sfx;
        Vector2 newMinSize = GetMinimumSize();
        newMinSize.X = Mathf.Min(Size.X, MAX_WIDTH);
        CustomMinimumSize = newMinSize;

        if (Size.X > MAX_WIDTH)
        {
            _label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            newMinSize.Y = Size.Y;
            CustomMinimumSize = newMinSize;
        }

        Vector2 newGlobalPosition = new Vector2
        {
            X = GlobalPosition.X - (Size.X / 2),
            Y = GlobalPosition.Y - (Size.Y + 24),
        };
        GlobalPosition = newGlobalPosition;
        _label.Text = "";
        DisplayLetter();
    }

    public void DisplayLetter()
    {
        _playingText = true;
        _label.Text += CurrentText[LetterIndex];
        LetterIndex += 1;

        if (LetterIndex >= CurrentText.Length)
        {
            EmitSignal(SignalName.FinishedDisplaying);
            _nextIndicator.Visible = true;
            _playingText = false;
            return;
        }

        char currentLetter = CurrentText[LetterIndex];
        switch (currentLetter)
        {
            case '!' or '.' or ',' or '?':
                _letterDisplayTimer.Start(BasePunctuationTime);
                break;
            case ' ':
                _letterDisplayTimer.Start(BaseSpaceTime);
                break;
            default:
                _letterDisplayTimer.Start(BaseLetterTime);
                AudioStreamPlayer dupPlayer = (AudioStreamPlayer)_audioPlayer.Duplicate();
                RandomNumberGenerator rng = new();
                dupPlayer.PitchScale += rng.RandfRange(-0.1f, 0.1f);
                if (highPitchLetters.Contains(currentLetter))
                {
                    dupPlayer.PitchScale += 0.2f;
                }
                GetTree().Root.AddChild(dupPlayer);
                dupPlayer.Play(1f);
                dupPlayer.Finished += () => dupPlayer.QueueFree();
                break;
        }
    }

}

