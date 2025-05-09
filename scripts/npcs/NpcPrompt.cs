using Godot;
using System;

public partial class NpcPrompt : Label
{
    public NpcPrompt(Vector2 promptPosition)
    {
        Text = "Press 'E' to interact";
        // Determine alignment based on promptPosition for Godot 4.3
        // Assumes this script is on a Label or derived Control node

        if (Mathf.Abs(promptPosition.Y) >= Mathf.Abs(promptPosition.X))
        {
            if (promptPosition.Y < 0)
            {
                // Above NPC
                HorizontalAlignment = HorizontalAlignment.Center;
                VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                // Below NPC
                HorizontalAlignment = HorizontalAlignment.Center;
                VerticalAlignment = VerticalAlignment.Bottom;
            }
        }
        else
        {
            if (promptPosition.X < 0)
            {
                // Left of NPC
                HorizontalAlignment = HorizontalAlignment.Left;
                VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                // Right of NPC
                HorizontalAlignment = HorizontalAlignment.Right;
                VerticalAlignment = VerticalAlignment.Center;
            }
        }
        Visible = false;
    }
}
