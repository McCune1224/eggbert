using Godot;
using System;
using System.Collections.Generic;

public partial class DialogManager : Node2D
{
    PackedScene TextBoxScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/TextBox.tscn");

    List<string> DialogLines;
    int CurrentDialogLineIndex = 0;


    TextBox CurrentTextBox;
    Vector2 TextBoxPosition;

    public bool IsDialogActive = false;
    public bool CanAdvanceLine = false;

    public AudioStream SFX;


    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("advance_dialog") && IsDialogActive && CanAdvanceLine)
        {
            CurrentTextBox.QueueFree();
            CurrentDialogLineIndex++;
            if (CurrentDialogLineIndex >= DialogLines.Count)
            {
                IsDialogActive = false;
                CurrentDialogLineIndex = 0;
            }
            ShowTextBox(GetTree());
        }
    }

    public void StartDialog(SceneTree t, Vector2 position, List<string> lines, AudioStream speechSfx)
    {
        SFX = speechSfx;
        if (IsDialogActive) return;
        DialogLines = lines;
        TextBoxPosition = position;
        IsDialogActive = true;
        ShowTextBox(t);
    }

    public void ShowTextBox(SceneTree t)
    {
        CurrentTextBox = TextBoxScene.Instantiate<TextBox>();
        CurrentTextBox.Connect(nameof(CurrentTextBox.FinishedDisplaying), new Callable(this, nameof(OnTextBoxFinishedDisplaying)));
        // CurrentTextBox.Connect("FinishedDisplaying", new Callable(this, nameof(OnTextBoxFinishedDisplaying)));
        if (t == null)
        {
            GD.PushError("FUCK");
        }
        t.Root.AddChild(CurrentTextBox);
        // GetTree().Root.AddChild(CurrentTextBox);
        CurrentTextBox.GlobalPosition = TextBoxPosition;
        CurrentTextBox.DisplayText(DialogLines[CurrentDialogLineIndex], SFX);
        CanAdvanceLine = false;
    }

    public void OnTextBoxFinishedDisplaying()
    {
        CanAdvanceLine = true;
    }

    public void Reset()
    {
        IsDialogActive = false;
        CurrentDialogLineIndex = 0;
        DialogLines = new List<string>();
        if (CurrentTextBox != null)
        {
            CurrentTextBox.QueueFree();
            CurrentTextBox = null;
        }
    }
}
