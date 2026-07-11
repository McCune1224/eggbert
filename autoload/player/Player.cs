using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D, ISavable
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
    public HealthComponent HealthComponent { get; private set; }
    public ParryComponent Parry { get; private set; }

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

        CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.InteractableLayer;

        AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _dash = GetNode<Dash>("Dash");
        Camera = GetNode<PlayerCamera>("PlayerCamera");

        HealthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (HealthComponent == null)
        {
            HealthComponent = new HealthComponent { Name = "HealthComponent" };
            AddChild(HealthComponent);
        }
        HealthComponent.Died += OnPlayerDied;
        HealthComponent.Damaged += (amount, source) => Camera?.Shake(6f, 0.3f);

        Parry = GetNodeOrNull<ParryComponent>("ParryComponent");
        if (Parry == null)
        {
            Parry = new ParryComponent { Name = "ParryComponent" };
            AddChild(Parry);
        }
        Parry.Parried += () => Camera?.Shake(4f, 0.15f);

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

        // Push blocks in slide direction
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var slide = GetSlideCollision(i);
            if (slide.GetCollider() is PushBlock block)
            {
                Vector2 pushDir = direction.Normalized();
                if (pushDir == Vector2.Zero) pushDir = -slide.GetNormal();
                block.TryPush(pushDir);
            }
        }

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

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_start_combat"))
        {
            CombatController.Instance?.EnterCombat(
                "res://combat/arena/OatmealArena.tscn",
                Vector2.Zero
            );
        }
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
        SaveDataPlayer saveData = new();

        saveData.Position = Position;
        saveData.Health = HealthComponent.CurrentHP;
        saveData.LevelScenePath = GameController.Instance.CurrentLevel.SceneFilePath;

        newSave.PlayerData = saveData;
        return newSave;
    }

    public void Load(SaveResource saveData)
    {
        if (saveData.PlayerData != null)
        {
            HealthComponent.CurrentHP = saveData.PlayerData.Health;
            GameController.Instance.LoadLevel(saveData.PlayerData.LevelScenePath, saveData.PlayerData.Position);
        }
    }

    private bool _deathInProgress = false;

    private async void OnPlayerDied()
    {
        if (_deathInProgress) return;
        _deathInProgress = true;

        if (GameController.Instance?.CurrentLevel is CombatArena)
        {
            _deathInProgress = false;
            return;
        }

        HealthComponent.Died -= OnPlayerDied;

        var lines = new System.Collections.Generic.List<string> { "You collapsed..." };
        DialogManager.Instance.StartDialog(lines);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        await FadeTransition.Instance.PlayFadeOut();

        GameController.Instance.LoadLevel(
            GameController.Instance.CheckpointLevelPath,
            GameController.Instance.CheckpointPosition,
            true
        );
        await ToSignal(GameController.Instance, GameController.SignalName.LevelLoaded);

        HealthComponent.Revive(50);
        HealthComponent.Died += OnPlayerDied;

        _deathInProgress = false;
    }

    public int GetLoadPriority()
    {
        // Player should load first
        return 10;
    }

}
