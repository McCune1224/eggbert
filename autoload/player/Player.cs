using Godot;
using Godot.Collections;

public partial class Player : CharacterBody2D, IPersistable
{
    public readonly float PlayerSpeed = 150.0f;
    public readonly float SprintScale = 1.7f; // Sprinting increases speed by 70%
    private bool _inInteraction = false;
    public bool InInteraction
    {
        get => _inInteraction;
        set => _inInteraction = value;
    }

    private static Player _instance;
    public static Player Instance => _instance;


    public AnimationPlayer AnimationPlayer { get; private set; }
    private CollisionShape2D _collisionShape;
    private Dash _dash;

    public PlayerCamera Camera { get; private set; }



    private string _facedDirection = "down";

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
        AddToGroup("persist");
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GD.PrintErr("Multiple instances of OverworldPlayer detected!");
        }

        AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _dash = GetNode<Dash>("Dash");
        Camera = GetNode<PlayerCamera>("PlayerCamera");

        AnimationPlayer.Play("idle forward");
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
        if (Input.IsActionJustPressed("dash"))
        {
            Vector2 dashDirection = _dash.StartDash(direction);
            if (dashDirection != Vector2.Zero)
            {
                GD.Print("Dashing in direction: " + dashDirection);
            }
        }
        else if (Input.IsActionPressed("player_sprint"))
        {
            if (!_dash.IsDashing()) Velocity *= SprintScale;
            ;
        }

        if (_dash.IsDashing())
        {
            Velocity *= _dash.DashScale;
        }
        MoveAndSlide();
        KinematicCollision2D coll = GetLastSlideCollision();
        UpdateAnimation(direction);
    }

    private void UpdateAnimation(Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            string currentAnim = AnimationPlayer.CurrentAnimation;
            if (currentAnim.StartsWith("walk"))
            {
                AnimationPlayer.Play("idle " + currentAnim.Substring(5));
            }
        }
        else
        {
            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
            {
                AnimationPlayer.Play(direction.X < 0 ? "walk left" : "walk right");
            }
            else
            {
                AnimationPlayer.Play(direction.Y < 0 ? "walk back" : "walk forward");
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



    public SaveResource Save(SaveResource newSave)
    {
        //TODO: Make health system? Using this as a placeholder for now.
        int temporaryHealth = 100;


        SaveDataPlayer saveData = new();

        saveData.Position = Position;
        saveData.Health = temporaryHealth;
        saveData.LevelScenePath = GameController.Instance.CurrentLevel.SceneFilePath;

        newSave.PlayerData = saveData;
        return newSave;
    }

    public void Load(SaveResource saveData)
    {
        if (saveData.PlayerData != null)
        {
            GameController.Instance.LoadLevel(saveData.PlayerData.LevelScenePath, saveData.PlayerData.Position);
        }
    }

    public int GetLoadPriority()
    {
        // Player should load first
        return 10;
    }

}
