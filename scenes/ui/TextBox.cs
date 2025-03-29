using Godot;
using System;

public partial class TextBox : MarginContainer
{
    const int MAX_WIDTH = 256;
    string CurrentText = "";
    int LetterIndex = 0;

    float LetterTime = 0.03f;
    float SpaceTime = 0.06f;
    float PunctuationTime = 0.02f;

    Label _label;
    Timer _timer;

    [Signal]
    public delegate void FinishedDisplayingEventHandler();

    public override void _Ready()
    {
        _label = GetNode<Label>("MarginContainer/Label");
        _timer = GetNode<Timer>("Timer");
    }

    public void DisplayText(string desiredText)
    {
        CurrentText = desiredText;
        _label.Text = desiredText;
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

        switch (CurrentText[LetterIndex])
        {
            case '!' or '.' or ',' or '?':
                _timer.Start(PunctuationTime);
                break;
            case ' ':
                _timer.Start(SpaceTime);
                break;
            default:
                _timer.Start(LetterTime);
                break;

        }
    }

}
