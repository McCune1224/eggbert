using Godot;

public partial class WarpPoint : Area2D
{
    [Export] public string WarpId = "";
    [Export] public string PromptText = "Press E to unlock warp";

    private ComponentPromptCollision _prompt;
    private bool _unlocked = false;

    public override void _Ready()
    {
        _unlocked = WarpDatabase.IsUnlocked(WarpId);
        _prompt = GetNode<ComponentPromptCollision>("ComponentPromptCollision");
        if (_unlocked)
            _prompt.HidePrompt();
    }

    public override void _Process(double delta)
    {
        if (_unlocked) return;
        if (_prompt.isPromptVisible() && Input.IsActionJustPressed("interact"))
        {
            _unlocked = true;
            WarpDatabase.Unlock(WarpId);
            _prompt.HidePrompt();
            if (WarpDatabase.All.TryGetValue(WarpId, out var dest))
                DialogManager.Instance.StartDialog(
                    new System.Collections.Generic.List<string> { $"Warp unlocked: {dest.Name}" });
        }
    }
}
