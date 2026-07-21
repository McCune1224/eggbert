using System.Collections.Generic;
using Godot;

public enum QuestStatus
{
    Locked,
    Active,
    Completed
}

[Tool]
public partial class QuestManager : Node
{
    private const string PinnedQuestFlag = "quest_pinned_id";

    private static QuestManager _instance;
    private readonly HashSet<QuestDefinition> _validQuests = new();
    private bool _validationComplete;

    public static QuestManager Instance => _instance;

    [Export] public Godot.Collections.Array<QuestDefinition> Quests { get; set; } = new();

    [Signal]
    public delegate void QuestStateChangedEventHandler();

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        if (_instance != null && _instance != this)
        {
            GameLogger.Error("QuestManager", "Multiple instances of QuestManager detected!");
            QueueFree();
            return;
        }

        _instance = this;
        EnsureValidation();
        WorldFlags.Instance.StateChanged += OnWorldFlagsStateChanged;
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint() && WorldFlags.Instance != null)
            WorldFlags.Instance.StateChanged -= OnWorldFlagsStateChanged;

        if (_instance == this)
            _instance = null;
    }

    public QuestDefinition GetQuest(string questId)
    {
        EnsureValidation();
        if (string.IsNullOrEmpty(questId))
            return null;

        foreach (QuestDefinition quest in Quests)
        {
            if (_validQuests.Contains(quest) && quest.Id == questId)
                return quest;
        }

        return null;
    }

    public QuestStatus GetStatus(QuestDefinition quest)
    {
        if (quest == null || !IsValidQuest(quest))
            return QuestStatus.Locked;

        QuestObjective finalObjective = quest.Objectives[quest.Objectives.Count - 1];
        if (WorldFlags.Instance != null && WorldFlags.Instance.HasFlag(finalObjective.CompletionFlag))
            return QuestStatus.Completed;

        return string.IsNullOrEmpty(quest.StartFlag) || (WorldFlags.Instance != null && WorldFlags.Instance.HasFlag(quest.StartFlag))
            ? QuestStatus.Active
            : QuestStatus.Locked;
    }

    public QuestObjective GetCurrentObjective(QuestDefinition quest)
    {
        if (quest == null || GetStatus(quest) != QuestStatus.Active)
            return null;

        foreach (QuestObjective objective in quest.Objectives)
        {
            if (!WorldFlags.Instance.HasFlag(objective.CompletionFlag))
                return objective;
        }

        return null;
    }

    public QuestDefinition GetPinnedQuest()
    {
        EnsureValidation();
        if (WorldFlags.Instance == null)
            return null;

        string pinnedId = WorldFlags.Instance.GetFlag(PinnedQuestFlag, "").AsString();
        QuestDefinition pinned = GetQuest(pinnedId);
        return GetStatus(pinned) == QuestStatus.Active ? pinned : null;
    }

    public void PinQuest(string questId)
    {
        QuestDefinition quest = GetQuest(questId);
        if (GetStatus(quest) != QuestStatus.Active)
        {
            GameLogger.Warn("QuestManager", $"Cannot pin quest '{questId}': it is unknown, locked, or completed.");
            return;
        }

        WorldFlags.Instance.SetFlag(PinnedQuestFlag, quest.Id);
    }

    public void UnpinQuest()
    {
        WorldFlags.Instance.ClearFlag(PinnedQuestFlag);
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new List<string>();
        CollectValidationProblems(warnings);
        return warnings.ToArray();
    }

    private void OnWorldFlagsStateChanged()
    {
        EmitSignal(SignalName.QuestStateChanged);
    }

    private QuestDefinition FirstActiveQuest()
    {
        foreach (QuestDefinition quest in Quests)
        {
            if (GetStatus(quest) == QuestStatus.Active)
                return quest;
        }

        return null;
    }

    private bool IsValidQuest(QuestDefinition quest)
    {
        EnsureValidation();
        return _validQuests.Contains(quest);
    }

    private void EnsureValidation()
    {
        if (_validationComplete)
            return;

        _validQuests.Clear();
        var problems = new List<string>();
        var idCounts = new Dictionary<string, int>();

        foreach (QuestDefinition quest in Quests)
        {
            if (quest != null && !string.IsNullOrWhiteSpace(quest.Id))
                idCounts[quest.Id] = idCounts.GetValueOrDefault(quest.Id) + 1;
        }

        foreach (QuestDefinition quest in Quests)
        {
            if (IsDefinitionValid(quest, idCounts, out string problem))
                _validQuests.Add(quest);
            else
                problems.Add(problem);
        }

        _validationComplete = true;
        foreach (string problem in problems)
            GameLogger.Error("QuestManager", problem);
    }

    private void CollectValidationProblems(List<string> problems)
    {
        var idCounts = new Dictionary<string, int>();
        foreach (QuestDefinition quest in Quests)
        {
            if (quest != null && !string.IsNullOrWhiteSpace(quest.Id))
                idCounts[quest.Id] = idCounts.GetValueOrDefault(quest.Id) + 1;
        }

        foreach (QuestDefinition quest in Quests)
        {
            if (!IsDefinitionValid(quest, idCounts, out string problem))
                problems.Add(problem);
        }
    }

    private static bool IsDefinitionValid(QuestDefinition quest, Dictionary<string, int> idCounts, out string problem)
    {
        if (quest == null)
        {
            problem = "Quest registry contains a null definition.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(quest.Id) || string.IsNullOrWhiteSpace(quest.Title) || string.IsNullOrWhiteSpace(quest.Description))
        {
            problem = $"Quest definition has an empty ID, title, or description ('{quest.Id}').";
            return false;
        }

        if (idCounts[quest.Id] > 1)
        {
            problem = $"Quest definition '{quest.Id}' has a duplicate quest ID.";
            return false;
        }

        if (quest.Objectives == null || quest.Objectives.Count == 0)
        {
            problem = $"Quest definition '{quest.Id}' has no objectives.";
            return false;
        }

        var objectiveIds = new HashSet<string>();
        foreach (QuestObjective objective in quest.Objectives)
        {
            if (objective == null || string.IsNullOrWhiteSpace(objective.Id) || string.IsNullOrWhiteSpace(objective.Description) || string.IsNullOrWhiteSpace(objective.CompletionFlag))
            {
                problem = $"Quest definition '{quest.Id}' has a null or incomplete objective.";
                return false;
            }

            if (!objectiveIds.Add(objective.Id))
            {
                problem = $"Quest definition '{quest.Id}' has a duplicate objective ID '{objective.Id}'.";
                return false;
            }
        }

        problem = "";
        return true;
    }
}
