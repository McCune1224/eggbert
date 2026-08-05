using Godot;

/// <summary>
/// One question for a QuizNpc: prompt lines, answer options, correct index,
/// per-question response lines, and an optional flag set when answered correctly.
/// </summary>
[GlobalClass]
public partial class QuizQuestion : Resource
{
    /// <summary>Lines spoken before the options are shown.</summary>
    [Export] public string[] PromptLines { get; set; }
    /// <summary>The answer choices the player can pick (2–4 recommended).</summary>
    [Export] public string[] Options { get; set; }
    /// <summary>Index of the correct option (0-based) within Options.</summary>
    [Export(PropertyHint.Range, "0,9,1")] public int CorrectIndex { get; set; } = 0;
    /// <summary>Lines spoken after answering correctly.</summary>
    [Export] public string[] CorrectResponseLines { get; set; }
    /// <summary>Lines spoken after answering wrong (before the quiz fail lines).</summary>
    [Export] public string[] WrongResponseLines { get; set; }
    /// <summary>World flag set to true when this question is answered correctly.</summary>
    [Export] public string CorrectFlag { get; set; } = "";
}