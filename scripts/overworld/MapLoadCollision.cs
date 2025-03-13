using Godot;
using Godot.Collections;
using System;

public partial class MapLoadCollision : Area2D
{

    [Export]
    private string _displayMapName = "";
    [ExportCategory("External References")]
    // In the Inspector, this property now gets a file picker with .tscn filter
    // [ExportFile(new string[] { "*.tscn" })]
    [Export(PropertyHint.File, "*.tscn")]
    public string MapLoadPath { get; set; }

    private GameController _instance;
    private Sprite2D _arrow;
    private bool _interactable = false;

    private NodePath GetOverworldPath()
    {
        return _instance.GetPath();
    }


    public override void _Ready()
    {
        if (_displayMapName == "") { _displayMapName = MapLoadPath.Split("/")[^1].Split(".")[0]; }
        Sprite2D arrow = GetNode<Sprite2D>("Sprite2D");
        BodyEntered += (Node2D body) =>
        {
            Label prompt = new Label();
            //add prompt to "prompt group"
            prompt.Text = "Press 'E' to enter " + _displayMapName;
            prompt.Name = "prompt";
            //move the prompt to the top of the body
            prompt.Position = new Vector2(0, -50);
            body.AddChild(prompt);
            _interactable = true;
        };
        BodyExited += (Node2D body) =>
        {
            body.GetNode("prompt").QueueFree();
            _interactable = false;
        };
    }

    public override void _Process(double delta)
    {
        if (_interactable && Input.IsActionPressed("interact"))
        {
            GameController.Instance.LoadOverworldScene(MapLoadPath);
        }
    }

}
