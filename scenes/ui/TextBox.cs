using Godot;
using System;
using System.Collections.Generic;

public partial class TextBox : MarginContainer
{
    const int MAX_WIDTH = 256;
    string CurrentText = "";
    int LetterIndex = 0;
    List<char> highPitchLetters = new() { 'a', 'e', 'i', 'o', 'u' };

    float LetterTime = 0.05f;
    float SpaceTime = 0.08f;
    float PunctuationTime = 0.3f;

    Label _label;
    Timer _timer;
    AudioStreamPlayer _audioPlayer;

    [Signal]
    public delegate void FinishedDisplayingEventHandler();

    public override void _Ready()
    {
        _label = this.GetNode<Label>("MarginContainer/Label");
        _timer = this.GetNode<Timer>("LetterDisplayTimer");
        _audioPlayer = this.GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        if (_label == null)
        {
        }
        if (_timer == null)
        {
        }
        _timer.Timeout += OnLetterDisplayTimerTimeout;
    }

    public void DisplayText(string desiredText, AudioStream sfx)
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
        _label.Text += CurrentText[LetterIndex];
        LetterIndex += 1;
        if (LetterIndex >= CurrentText.Length)
        {
            EmitSignal(SignalName.FinishedDisplaying);
            return;
        }

        char currentLetter = CurrentText[LetterIndex];
        switch (currentLetter)
        {
            case '!' or '.' or ',' or '?':
                _timer.Start(PunctuationTime);
                break;
            case ' ':
                _timer.Start(SpaceTime);
                break;
            default:
                _timer.Start(LetterTime);
                AudioStreamPlayer dupPlayer = (AudioStreamPlayer)_audioPlayer.Duplicate();
                RandomNumberGenerator rng = new();
                dupPlayer.PitchScale += rng.RandfRange(-0.1f, 0.1f);
                if (highPitchLetters.Contains(currentLetter))
                {
                    dupPlayer.PitchScale += 0.2f;
                }
                GetTree().Root.AddChild(dupPlayer);

                // Create a timer to stop the audio after LetterTime
                // Timer audioTimer = new Timer();
                // audioTimer.WaitTime = LetterTime;
                // audioTimer.OneShot = true;
                // GetTree().Root.AddChild(audioTimer);
                // audioTimer.Start();
                // audioTimer.Timeout += () =>
                // {
                //     dupPlayer.Stop();
                //     dupPlayer.QueueFree();
                //     audioTimer.QueueFree();
                // };
                dupPlayer.Play(1f);
                dupPlayer.Finished += () =>
                {
                    dupPlayer.QueueFree();
                };
                break;

        }
    }

    //_on_letter_display_timer_timeout Signal Implementation
    public void OnLetterDisplayTimerTimeout()
    {
        DisplayLetter();
    }

}
