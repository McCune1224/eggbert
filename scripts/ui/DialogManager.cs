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

    bool IsDialogActive = false;
    bool CanAdvanceLine = false;


    public void _Process()
    {
        GD.Print("Processing");
        if (IsDialogActive && CanAdvanceLine)
        {
            GD.Print("Ready for next box");
        }
        if (Input.IsActionPressed("advance_dialog") && IsDialogActive && CanAdvanceLine)
        {
            CurrentTextBox.QueueFree();
            CurrentDialogLineIndex++;
            if (CurrentDialogLineIndex >= DialogLines.Count)
            {
                IsDialogActive = false;
                CurrentDialogLineIndex = 0;
            }
        }
        ShowTextBox(GetTree());
    }

    public void StartDialog(SceneTree t, Vector2 position, List<string> lines)
    {
        if (IsDialogActive) return;

        DialogLines = lines;
        TextBoxPosition = position;
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
        CurrentTextBox.DisplayText(DialogLines[CurrentDialogLineIndex]);
        CanAdvanceLine = false;
    }

    public void OnTextBoxFinishedDisplaying()
    {
        GD.Print("NEXT LINE READY");
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
