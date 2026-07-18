using Godot;

public enum ConditionType
{
    Always,
    FlagSet,
    FlagNotSet,
    ChoiceEquals
}

[GlobalClass]
public partial class CutsceneCondition : Resource
{
    [Export] public ConditionType Type { get; set; } = ConditionType.Always;
    [Export] public string FlagKey { get; set; }
    [Export] public int ChoiceIndex { get; set; } = -1;

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
