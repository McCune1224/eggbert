using Godot;
using System;

public enum TransitionSide
{
    Up,
    Down,
    Left,
    Right
}

[Tool]
public partial class LevelTransition : Area2D
{
    CollisionShape2D _collisionShape;

    [Export(PropertyHint.File, "*.tscn")]
    public string Level { get; set; }

    [Export]
    public string TargetTransitionArea = "LevelTransition";
    [ExportCategory("CollisionAreaSettings")]

    [Export(PropertyHint.Range, "1,12,1,or_greater")]
    public int Size;
    public override bool _Set(StringName property, Variant value)
    {
        if (property == "Size")
        {
            Size = ((int)value);
            Update_Area();
            return true;
        }
        if (property == "Side")
        {
            Side = Enum.Parse<TransitionSide>(value.ToString());
            Update_Area();
            return true;
        }
        return false;
    }


    [Export]
    public TransitionSide Side = TransitionSide.Left;
    [Export]
    bool SnapToGrid = false;


    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        this.BodyEntered += SceneTransition;
        BodyExited += (Node2D body) => { body.GetNode("prompt").QueueFree(); };
        Update_Area();
    }


    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            Update_Area();
            return;
        }
        if (this.HasOverlappingBodies())
        {

            if (Input.IsActionPressed("interact"))
            {

                GameController.Instance.LoadOverworldScene(Level, Vector2.Zero);
            }
        }

    }
    public void SceneTransition(Node2D body)
    {
        Label prompt = new Label();
        //add prompt to "prompt group"
        prompt.Text = "Press 'E' to enter " + Level;
        prompt.Name = "prompt";
        //move the prompt to the top of the body
        prompt.Position = new Vector2(0, -50);
        body.AddChild(prompt);
    }


    public void Update_Area()
    {
        if (_collisionShape == null)
        {
            GD.PrintErr("CollisionShape2D not found.");
            return;
        }
        Vector2 newRectangleSize = new Vector2(16, 16);
        Vector2 newPosition = Vector2.Zero;

        switch (Side)
        {
            case TransitionSide.Up:
                newRectangleSize = new Vector2(16, Size * 16);
                newPosition = new Vector2(0, -Size * 8);
                break;
            case TransitionSide.Down:
                newRectangleSize = new Vector2(16, Size * 16);
                newPosition = new Vector2(0, Size * 8);
                break;
            case TransitionSide.Left:
                newRectangleSize = new Vector2(Size * 16, 16);
                newPosition = new Vector2(-Size * 8, 0);
                break;
            case TransitionSide.Right:
                newRectangleSize = new Vector2(Size * 16, 16);
                newPosition = new Vector2(Size * 8, 0);
                break;
        }

        _collisionShape.Position = newPosition;
        RectangleShape2D newRectangleShape = new RectangleShape2D();
        newRectangleShape.Size = newRectangleSize;
        _collisionShape.Shape = newRectangleShape;

        _collisionShape.NotifyPropertyListChanged();
        QueueRedraw();
    }

}
