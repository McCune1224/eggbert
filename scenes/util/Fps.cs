using Godot;
using System;

public partial class Fps : RichTextLabel
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        string logString = "DLT: " + delta.ToString("F4") + "\nFPS: " + Engine.GetFramesPerSecond().ToString("F0");
        this.Text = logString;
    }
}
