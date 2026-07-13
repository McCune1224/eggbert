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

    public bool IsMet(WorldFlags flags, int lastChoiceIndex) => Type switch
    {
        ConditionType.Always => true,
        ConditionType.FlagSet => flags.HasFlag(FlagKey),
        ConditionType.FlagNotSet => !flags.HasFlag(FlagKey),
        ConditionType.ChoiceEquals => lastChoiceIndex == ChoiceIndex,
        _ => true
    };
}
