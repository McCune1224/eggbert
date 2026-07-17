using Godot;

/// <summary>
/// One question for a QuizNpc: prompt lines, answer options, correct index,
/// per-question response lines, and an optional flag set when answered correctly.
/// </summary>
[GlobalClass]
public partial class QuizQuestion : Resource
{
    [Export] public string[] PromptLines { get; set; }
    [Export] public string[] Options { get; set; }
    [Export] public int CorrectIndex { get; set; } = 0;
    [Export] public string[] CorrectResponseLines { get; set; }
    [Export] public string[] WrongResponseLines { get; set; }
    /// <summary>World flag set to true when this question is answered correctly.</summary>
    [Export] public string CorrectFlag { get; set; } = "";
}