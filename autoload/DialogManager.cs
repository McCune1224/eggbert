using Godot;
using System;
using System.Collections.Generic;

public partial class DialogManager : Node2D
{
    private static DialogManager _instance;
    public static DialogManager Instance => _instance;


    PackedScene TextBoxScene = ResourceLoader.Load<PackedScene>("res://ui/TextBox.tscn");


    List<string> DialogLines;
    int CurrentDialogLineIndex = 0;


    TextBox CurrentTextBox;
    Vector2 TextBoxPosition;

    public bool IsDialogActive = false;
    public bool CanAdvanceLine = false;

    public AudioStream SFX;


    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of OverworldManager detected!");
        }

        // DialogManagerScene = ResourceLoader.Load<PackedScene>("res://scripts/ui/DialogManager.tscn");
    }

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
            ShowTextBox();
        }
    }

    public void StartDialog(Vector2 position, List<string> lines, AudioStream speechSfx)
    {
        SFX = speechSfx;
        if (IsDialogActive) return;
        DialogLines = lines;
        TextBoxPosition = position;
        IsDialogActive = true;
        ShowTextBox();
    }

    public void ShowTextBox()
    {
        CurrentTextBox = TextBoxScene.Instantiate<TextBox>();
        CurrentTextBox.Connect(nameof(CurrentTextBox.FinishedDisplaying), new Callable(this, nameof(OnTextBoxFinishedDisplaying)));
        // CurrentTextBox.Connect("FinishedDisplaying", new Callable(this, nameof(OnTextBoxFinishedDisplaying)));
        GetTree().Root.AddChild(CurrentTextBox);
        // t.Root.AddChild(CurrentTextBox);
        // GetTree().Root.AddChild(CurrentTextBox);
        CurrentTextBox.GlobalPosition = TextBoxPosition;
        CurrentTextBox.PlayText(DialogLines[CurrentDialogLineIndex], SFX);
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
