using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DialogManager : Node2D
{
    [Signal]
    public delegate void DialogFinishedEventHandler();
    private static DialogManager _instance;
    public static DialogManager Instance => _instance;

    public enum TextSpeed { Instant, Fast, Normal }
    public static TextSpeed CurrentTextSpeed = TextSpeed.Normal;

    private PackedScene TextBoxScene = ResourceLoader.Load<PackedScene>("res://ui/TextBox.tscn");

    private List<string> DialogLines;
    private int CurrentDialogLineIndex = 0;

    private TextBox CurrentTextBox;
    private DialogVoice _currentVoice;
    private ChoiceMenu _activeChoiceMenu;

    public bool IsDialogActive = false;

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of DialogManager detected!");
        }
    }

    public void StartDialog(List<string> lines, DialogVoice voice = null)
    {
        if (IsDialogActive) return;

        _currentVoice = voice ?? new DialogVoice();
        DialogLines = lines;
        IsDialogActive = true;
        Player.Instance.InInteraction = true;

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (CurrentDialogLineIndex >= DialogLines.Count)
        {
            IsDialogActive = false;
            Player.Instance.InInteraction = false;
            CurrentDialogLineIndex = 0;
            _currentVoice = null;
            EmitSignal(SignalName.DialogFinished);
            return;
        }

        CurrentTextBox = TextBoxScene.Instantiate<TextBox>();
        CurrentTextBox.LineComplete += OnCurrentLineComplete;

        GetTree().Root.AddChild(CurrentTextBox);
        CurrentTextBox.PlayText(DialogLines[CurrentDialogLineIndex], _currentVoice);
    }

    private void OnCurrentLineComplete()
    {
        if (CurrentTextBox != null && GodotObject.IsInstanceValid(CurrentTextBox))
        {
            CurrentTextBox.LineComplete -= OnCurrentLineComplete;
            CurrentTextBox.QueueFree();
            CurrentTextBox = null;
        }

        CurrentDialogLineIndex++;
        ShowNextLine();
    }

    public async Task<int> PromptChoices(List<string> choices)
    {
        _activeChoiceMenu = new ChoiceMenu();
        _activeChoiceMenu.SetChoices(choices);
        GetTree().Root.AddChild(_activeChoiceMenu);
        Variant[] result = await ToSignal(_activeChoiceMenu, ChoiceMenu.SignalName.ChoiceSelected);
        _activeChoiceMenu.QueueFree();
        _activeChoiceMenu = null;
        return (int)result[0];
    }

    public void Reset()
    {
        IsDialogActive = false;
        CurrentDialogLineIndex = 0;
        DialogLines = new List<string>();
        _currentVoice = null;
        if (Player.Instance != null)
            Player.Instance.InInteraction = false;
        if (CurrentTextBox != null && GodotObject.IsInstanceValid(CurrentTextBox))
        {
            CurrentTextBox.LineComplete -= OnCurrentLineComplete;
            CurrentTextBox.QueueFree();
            CurrentTextBox = null;
        }
        if (_activeChoiceMenu != null && GodotObject.IsInstanceValid(_activeChoiceMenu))
        {
            _activeChoiceMenu.QueueFree();
            _activeChoiceMenu = null;
        }
    }
}
