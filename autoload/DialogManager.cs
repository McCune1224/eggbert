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
    public static TextSpeed CurrentTextSpeed = TextSpeed.Fast;

    public DialogVoiceResource DefaultVoice { get; private set; }

    private List<string> DialogLines;
    private int CurrentDialogLineIndex = 0;

    private DialogBubble CurrentDialogBubble;
    private DialogVoiceResource _currentVoice;
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
            GameLogger.Error("Dialog", "Multiple instances of DialogManager detected!");
        }

        DefaultVoice = new DialogVoiceResource();
    }

    public void StartDialog(List<string> lines, DialogVoiceResource voice = null)
    {
        GameLogger.Debug("Dialog", $"Starting dialog ({lines.Count} lines)");
        if (IsDialogActive) return;

        _currentVoice = voice ?? DefaultVoice;
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

        CurrentDialogBubble = new DialogBubble();
        CurrentDialogBubble.LineComplete += OnCurrentLineComplete;

        GetTree().Root.AddChild(CurrentDialogBubble);
        CurrentDialogBubble.DisplayText(DialogLines[CurrentDialogLineIndex], _currentVoice ?? DefaultVoice);
    }

    private void OnCurrentLineComplete()
    {
        if (CurrentDialogBubble != null && GodotObject.IsInstanceValid(CurrentDialogBubble))
        {
            CurrentDialogBubble.LineComplete -= OnCurrentLineComplete;
            CurrentDialogBubble.QueueFree();
            CurrentDialogBubble = null;
        }

        CurrentDialogLineIndex++;
        ShowNextLine();
    }

    public async Task<int> PromptChoices(List<string> choices)
    {
        bool wasDialogActive = IsDialogActive;
        Player player = Player.Instance;
        bool wasInInteraction = player?.InInteraction ?? false;
        IsDialogActive = true;
        if (player != null)
            player.InInteraction = true;

        _activeChoiceMenu = new ChoiceMenu();
        GameLogger.Debug("Dialog", $"Prompting choices ({choices.Count} options)");
        GetTree().Root.AddChild(_activeChoiceMenu);
        _activeChoiceMenu.SetChoices(choices);
        try
        {
            Variant[] result = await ToSignal(_activeChoiceMenu, ChoiceMenu.SignalName.ChoiceSelected);
            int choice = (int)result[0];
            string selectedText = choice >= 0 && choice < choices.Count ? choices[choice] : "?";
            GameLogger.Info("Dialog", $"Choice selected: {choice} — '{selectedText}'");
            return choice;
        }
        finally
        {
            if (_activeChoiceMenu != null && GodotObject.IsInstanceValid(_activeChoiceMenu))
                _activeChoiceMenu.QueueFree();

            _activeChoiceMenu = null;
            IsDialogActive = wasDialogActive;
            if (player != null)
                player.InInteraction = wasInInteraction;
        }
    }

    public void Reset()
    {
        GameLogger.Debug("Dialog", "Dialog reset.");
        IsDialogActive = false;
        CurrentDialogLineIndex = 0;
        DialogLines = new List<string>();
        _currentVoice = null;
        if (Player.Instance != null)
            Player.Instance.InInteraction = false;
        if (CurrentDialogBubble != null && GodotObject.IsInstanceValid(CurrentDialogBubble))
        {
            CurrentDialogBubble.LineComplete -= OnCurrentLineComplete;
            CurrentDialogBubble.QueueFree();
            CurrentDialogBubble = null;
        }
        if (_activeChoiceMenu != null && GodotObject.IsInstanceValid(_activeChoiceMenu))
        {
            _activeChoiceMenu.QueueFree();
            _activeChoiceMenu = null;
        }
    }
}
