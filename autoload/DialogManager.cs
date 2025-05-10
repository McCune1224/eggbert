using Godot;
using System.Collections.Generic;

public partial class DialogManager : Node2D
{
    private static DialogManager _instance;
    public static DialogManager Instance => _instance;

    private PackedScene TextBoxScene = ResourceLoader.Load<PackedScene>("res://ui/TextBox.tscn");

    private List<string> DialogLines;
    private int CurrentDialogLineIndex = 0;

    private TextBox CurrentTextBox;
    private Vector2 TextBoxPosition;

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
                Player.Instance.InInteraction = true;
                CurrentDialogLineIndex = 0;
            }
            ShowTextBox();
        }
    }

    public void StartDialog(List<string> lines, AudioStream speechSfx)
    {
        SFX = speechSfx;
        if (IsDialogActive) return;
        DialogLines = lines;
        IsDialogActive = true;
        Player.Instance.InInteraction = true;
        ShowTextBox();
    }

    public void ShowTextBox()
    {
        CurrentTextBox = TextBoxScene.Instantiate<TextBox>();
        PlayerCamera pc = Player.Instance.Camera;
        Vector2 centerScreen = pc.GetScreenCenterPosition();
        CurrentTextBox.GlobalPosition = new Vector2(centerScreen.X, centerScreen.Y + (pc.GetViewportRect().Size.Y - 156));
        GD.Print(CurrentTextBox.GlobalPosition);
        CurrentTextBox.Connect(nameof(CurrentTextBox.FinishedDisplaying), new Callable(this, nameof(OnTextBoxFinishedDisplaying)));
        GetTree().Root.AddChild(CurrentTextBox);
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
