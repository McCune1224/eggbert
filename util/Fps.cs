using Godot;

public partial class Fps : RichTextLabel
{
    // Called when the node enters the scene tree for the first time.

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        string logString = "DLT: " + delta.ToString("F4") + "\nFPS: " + Engine.GetFramesPerSecond().ToString("F0");
        Text = logString;
    }
}
