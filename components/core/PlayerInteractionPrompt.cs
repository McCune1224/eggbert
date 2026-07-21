using Godot;
using System.Collections.Generic;

public partial class PlayerInteractionPrompt : Sprite2D
{
    private readonly HashSet<ulong> _availableSourceIds = new();

    public override void _Ready()
    {
        Texture = ResourceLoader.Load<Texture2D>("res://assets/ui/NPCPrompt.png");
        Position = new Vector2(0, -32);
        ZIndex = 1;
        Visible = false;
    }

    public void SetInteractableAvailable(Node source, bool isAvailable)
    {
        if (source == null)
            return;

        ulong sourceId = source.GetInstanceId();

        if (isAvailable)
        {
            if (_availableSourceIds.Add(sourceId))
                source.TreeExiting += () => OnSourceTreeExiting(sourceId);
        }
        else
        {
            _availableSourceIds.Remove(sourceId);
        }

        UpdateVisibility();
    }

    private void OnSourceTreeExiting(ulong sourceId)
    {
        if (_availableSourceIds.Remove(sourceId))
            UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        Visible = _availableSourceIds.Count > 0 && Settings.ShowInteractionPrompt;
    }
}
