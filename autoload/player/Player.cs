using Godot;
using Godot.Collections;

public partial class Player : CharacterBody2D
{
    private static Player _instance;
    public static Player Instance => _instance;

    public readonly float PlayerSpeed = 150.0f;

    private AnimationPlayer _animationPlayer;
    private CollisionShape2D _collisionShape;
    public PlayerCamera Camera { get; private set; }

    private string _facedDirection = "down";
    private bool _inInteraction = false;
    public bool InInteraction
    {
        get => _inInteraction;
        set => _inInteraction = value;
    }

    public Array<Node2D> GetCollidingBodies()
    {
        var colliders = new Array<Node2D>();
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            colliders.Add((Node2D)collision.GetCollider());
        }
        return colliders;
    }

    public override void _Ready()
    {
        AddToGroup("player");
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of OverworldPlayer detected!");
        }

        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        Camera = GetNode<PlayerCamera>("PlayerCamera");

        _animationPlayer.Play("idle forward");
    }

    public override void _Process(double delta)
    {
        if (!_inInteraction)
        {
            HandleMovement(delta);
        }
    }

    private void HandleMovement(double delta)
    {
        Vector2 direction = Input.GetVector("player_left", "player_right", "player_up", "player_down");
        if (direction.Length() > 1.0f)
        {
            direction = direction.Normalized();
        }
        Velocity = direction * PlayerSpeed;
        MoveAndSlide();
        UpdateAnimation(direction);
    }

    private void UpdateAnimation(Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            string currentAnim = _animationPlayer.CurrentAnimation;
            if (currentAnim.StartsWith("walk"))
            {
                _animationPlayer.Play("idle " + currentAnim.Substring(5));
            }
        }
        else
        {
            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
            {
                _animationPlayer.Play(direction.X < 0 ? "walk left" : "walk right");
            }
            else
            {
                _animationPlayer.Play(direction.Y < 0 ? "walk back" : "walk forward");
            }
        }
    }

    public void SetInitialPosition(Vector2 position)
    {
        Position = position;
    }

    public void StartInteraction()
    {
        _inInteraction = true;
    }

    public void EndInteraction()
    {
        _inInteraction = false;
    }

    public void OnSaveGame(Godot.Collections.Array<SavedData> saveDataList)
    {
        SavedData newData = new();
        newData.ScenePath = SceneFilePath;
        saveDataList.Add(newData);
    }
}
