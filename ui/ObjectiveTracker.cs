using Godot;

public partial class ObjectiveTracker : CanvasLayer
{
    private PanelContainer _panel;
    private Label _questTitleLabel;
    private Label _objectiveLabel;
    private bool _levelReady;
    private bool _showingCompletion;
    private int _displayVersion;
    private string _cachedQuestId = "";
    private string _cachedObjectiveId = "";
    private string _cachedObjectiveText = "";

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");
        _questTitleLabel = GetNode<Label>("Panel/VBoxContainer/QuestTitleLabel");
        _objectiveLabel = GetNode<Label>("Panel/VBoxContainer/ObjectiveLabel");
        if (FontCache.Yoster != null)
        {
            _questTitleLabel.AddThemeFontOverride("font", FontCache.Yoster);
            _objectiveLabel.AddThemeFontOverride("font", FontCache.Yoster);
        }
        _panel.Visible = false;

        GameController.Instance.LevelLoadStarted += OnLevelLoadStarted;
        GameController.Instance.LevelLoaded += OnLevelLoaded;
        QuestManager.Instance.QuestStateChanged += OnQuestStateChanged;
    }

    public override void _ExitTree()
    {
        _displayVersion++;
        if (GameController.Instance != null)
        {
            GameController.Instance.LevelLoadStarted -= OnLevelLoadStarted;
            GameController.Instance.LevelLoaded -= OnLevelLoaded;
        }

        if (QuestManager.Instance != null)
            QuestManager.Instance.QuestStateChanged -= OnQuestStateChanged;
    }

    private void OnLevelLoadStarted()
    {
        _displayVersion++;
        _levelReady = false;
        _showingCompletion = false;
        ClearCachedObjective();
        _panel.Visible = false;
    }

    private void OnLevelLoaded()
    {
        _levelReady = GameController.Instance.CurrentLevel is BaseLevel;
        Refresh();
    }

    private void OnQuestStateChanged()
    {
        if (_levelReady)
            Refresh();
    }

    public void Refresh()
    {
        if (!_levelReady)
        {
            _panel.Visible = false;
            return;
        }

        if (_showingCompletion)
            return;

        if (CachedObjectiveHasCompleted())
        {
            string completedText = _cachedObjectiveText;
            ClearCachedObjective();
            ShowCompletionNotice(completedText);
            return;
        }

        QuestDefinition quest = QuestManager.Instance.GetPinnedQuest();
        QuestObjective objective = QuestManager.Instance.GetCurrentObjective(quest);
        if (quest == null || objective == null)
        {
            ClearCachedObjective();
            _panel.Visible = false;
            return;
        }

        _questTitleLabel.Text = quest.Title;
        _objectiveLabel.Text = objective.Description;
        _cachedQuestId = quest.Id;
        _cachedObjectiveId = objective.Id;
        _cachedObjectiveText = objective.Description;
        _panel.Visible = true;
    }

    private bool CachedObjectiveHasCompleted()
    {
        if (string.IsNullOrEmpty(_cachedQuestId) || string.IsNullOrEmpty(_cachedObjectiveId))
            return false;

        QuestDefinition quest = QuestManager.Instance.GetQuest(_cachedQuestId);
        if (quest == null)
            return false;

        foreach (QuestObjective objective in quest.Objectives)
        {
            if (objective.Id == _cachedObjectiveId)
                return WorldFlags.Instance.HasFlag(objective.CompletionFlag);
        }

        return false;
    }

    private async void ShowCompletionNotice(string completedText)
    {
        int version = ++_displayVersion;
        _showingCompletion = true;
        _objectiveLabel.Text = $"✓ {completedText}";
        _panel.Visible = true;
        await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        if (version != _displayVersion || !IsInsideTree())
            return;

        _showingCompletion = false;
        Refresh();
    }

    private void ClearCachedObjective()
    {
        _cachedQuestId = "";
        _cachedObjectiveId = "";
        _cachedObjectiveText = "";
    }
}
