using Godot;

/// <summary>
/// Determines whether a cutscene step or dialog node should execute.
/// FlagSet — requires the named world flag to be true (used for gating steps after a prior event).
/// FlagNotSet — requires the named world flag to be false (used for first-time triggers).
/// ChoiceEquals — requires the last prompt choice index to match the configured value (used for branching on player selection).
/// Always — no condition; the step executes regardless (the default).
/// </summary>
public enum ConditionType
{
    Always,
    FlagSet,
    FlagNotSet,
    ChoiceEquals
}

/// <summary>
/// A reusable condition evaluated against <see cref="WorldFlags"/> and an optional choice index.
/// Attach to <see cref="CutsceneStep.Condition"/> or <see cref="DialogNode.Condition"/> to gate execution.
/// </summary>
[GlobalClass]
public partial class CutsceneCondition : Resource
{
    /// <summary>
    /// The type of condition to evaluate. Determines which of <see cref="FlagKey"/> or <see cref="ChoiceIndex"/> is used.
    /// </summary>
    [Export] public ConditionType Type { get; set; } = ConditionType.Always;
    /// <summary>
    /// The world flag key checked by FlagSet (requires true) and FlagNotSet (requires false). Ignored by Always and ChoiceEquals.
    /// </summary>
    [Export] public string FlagKey { get; set; }
    /// <summary>
    /// The choice index checked by ChoiceEquals. Set to -1 (default) to disable choice-based gating.
    /// </summary>
    [Export] public int ChoiceIndex { get; set; } = -1;

    /// <summary>
    /// Evaluates this condition against the current world state and last player choice.
    /// </summary>
    public bool IsMet(WorldFlags flags, int lastChoiceIndex)
    {
        bool result = Type switch
        {
            ConditionType.Always => true,
            ConditionType.FlagSet => flags.HasFlag(FlagKey),
            ConditionType.FlagNotSet => !flags.HasFlag(FlagKey),
            ConditionType.ChoiceEquals => lastChoiceIndex == ChoiceIndex,
            _ => true
        };

        string detail = Type switch
        {
            ConditionType.FlagSet => $"flag '{FlagKey}'={(flags.HasFlag(FlagKey) ? "set" : "not set")}",
            ConditionType.FlagNotSet => $"flag '{FlagKey}'={(flags.HasFlag(FlagKey) ? "set" : "not set")}",
            ConditionType.ChoiceEquals => $"lastChoice={lastChoiceIndex} == expected={ChoiceIndex}",
            _ => ""
        };
        GameLogger.Debug("Cutscene", $"Condition [{Type}]: {detail} → {(result ? "PASS" : "FAIL")}");

        return result;
    }
}
