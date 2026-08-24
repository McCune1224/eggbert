using Godot;

/// <summary>
/// Reusable factory NPC. The dialog branch is assigned at runtime from
/// <see cref="DialogBranchPath"/> instead of as a typed scene property, so the
/// editor's import scan (which runs before the C# assembly is loaded) never
/// attempts the Resource→DialogBranch cast. Set DialogBranchPath per instance
/// in the Inspector; it is resolved in _Ready at runtime only.
/// </summary>
public partial class GenericFactoryWorker : StaticBody2D
{
    /// <summary>res:// path to a DialogBranch .tres (e.g. .../PipDialog.tres). Empty = no dialog.</summary>
    [Export] public string DialogBranchPath = "";

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        if (string.IsNullOrEmpty(DialogBranchPath))
            return;

        var trigger = GetNodeOrNull<DialogBranchTrigger>("DialogBranchTrigger");
        var branch = ResourceLoader.Load<DialogBranch>(DialogBranchPath);

        if (trigger == null)
        {
            GameLogger.Warn("GenericFactoryWorker", $"'{Name}': no DialogBranchTrigger child — dialog not wired");
            return;
        }
        if (branch == null)
        {
            GameLogger.Warn("GenericFactoryWorker", $"'{Name}': could not load DialogBranch at '{DialogBranchPath}'");
            return;
        }

        trigger.DialogBranch = branch;
        GameLogger.Info("GenericFactoryWorker", $"'{Name}': dialog wired from {DialogBranchPath}");
    }
}
