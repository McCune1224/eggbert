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
    public string TargetTransitionName = "";
    [ExportCategory("CollisionAreaSettings")]

    [Export(PropertyHint.Range, "1,12,1,or_greater")]
    public int Size;

    [Export]
    public TransitionSide Side;
    [Export]
    bool SnapToGrid = false;

    public override bool _Set(StringName property, Variant value)
    {
        if (property == "Size")
        {
            Size = ((int)value);
            Update_Area();
            return true;
        }
        else if (property == "Side")
        {
            Side = Enum.Parse<TransitionSide>(value.ToString());
            Update_Area();
            return true;
        }
        //FIXME: For whatever reason this won't snap to grid, but 
        //more a QOL thing so life will go on
        else if (property == "SnapToGrid" || property == "Snap to Grid")
        {
            if ((bool)value)
            {
                this.Position = new Vector2
                {
                    X = Mathf.Round(Position.X / 16) * 16,
                    Y = Mathf.Round(Position.Y / 16) * 16,
                };
            }
            return true;
        }
        return false;
    }



    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        Update_Area();
        if (Engine.IsEditorHint()) { return; }

        // this.Monitoring = true;
        // this.Monitorable = false;
        BodyEntered += SceneTransition;
        BodyExited += (Node2D body) => { body.GetNode("prompt").QueueFree(); };
    }


    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            Update_Area();
            return;
        }
        // if (this.HasOverlappingBodies())
        // {
        //
        //     if (Input.IsActionPressed("interact"))
        //     {
        //
        //         GameController.Instance.LoadOverworldScene(Level, Vector2.Zero);
        //     }
        // }

    }

    public void SceneTransition(Node2D body)
    {
        Vector2 newPlayerPosition = Vector2.Zero;

        //WARNING: Idk why I have GameController handling level transitions, Too Bad I'm Lazy!
        try
        {
            if (TargetTransitionName != "")
            {
                GameController.Instance.LoadLevel(Level, TargetTransitionName);
            }
            else
            {
                GameController.Instance.LoadLevel(Level, Vector2.Zero);
            }
        }
        catch (Exception)
        {
            GD.PrintErr("LevelTransition: SceneTransition: Failed to load level: " + Level);
            throw;
        }
        // Label prompt = new Label();
        // //add prompt to "prompt group"
        // prompt.Text = "Press 'E' to enter " + Level;
        // prompt.Name = "prompt";
        // //move the prompt to the top of the body
        // prompt.Position = new Vector2(0, -50);
        // body.AddChild(prompt);
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
            case TransitionSide.Left:
                newRectangleSize = new Vector2(16, Size * newRectangleSize.Y);
                newPosition = new Vector2(newPosition.X - 8, newPosition.Y);
                break;
            case TransitionSide.Right:
                newRectangleSize = new Vector2(16, Size * newRectangleSize.Y);
                newPosition = new Vector2(newPosition.X + 8, newPosition.Y);
                break;
            case TransitionSide.Up:
                newRectangleSize = new Vector2(Size * newRectangleSize.X, 16);
                newPosition = new Vector2(newPosition.X, newPosition.Y - 8);
                break;
            case TransitionSide.Down:
                newRectangleSize = new Vector2(Size * newRectangleSize.X, 16);
                newPosition = new Vector2(newPosition.X, newPosition.Y + 8);
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
