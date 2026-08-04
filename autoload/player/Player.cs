using Godot;
using Godot.Collections;

public partial class Player : CharacterBody2D, ISavable
{
    public const float PlayerSpeed = 150.0f;
    public const float SprintScale = 1.7f;

    private bool _inInteraction = false;
    /// <summary>
    /// Prevents movement/input processing during cutscenes and dialogs.
    /// Set to true by StartInteraction(), false by EndInteraction().
    /// </summary>
     public bool InInteraction
     {
         get => _inInteraction;
         set => _inInteraction = value;
     }

    public Vector2 FacingDirection { get; private set; } = Vector2.Down;

    private static Player _instance;
    public static Player Instance => _instance;


    public AnimationPlayer AnimationPlayer { get; private set; }
    private CollisionShape2D _collisionShape;
    private Dash _dash;

    public PlayerCamera Camera { get; private set; }
    public HealthComponent HealthComponent { get; private set; }
    public ParryComponent Parry { get; private set; }

    public PlayerInteractionPrompt InteractionPrompt { get; private set; }

    private readonly System.Collections.Generic.List<ConveyorTile> _activeConveyors = new();

    /// <summary>
    /// Registers this player as standing on a conveyor tile. Called by ConveyorTile.BodyEntered.
    /// </summary>
    public void RegisterConveyor(ConveyorTile conveyor)
    {
        if (!_activeConveyors.Contains(conveyor))
            _activeConveyors.Add(conveyor);
    }

    /// <summary>
    /// Unregisters this player from a conveyor tile. Called by ConveyorTile.BodyExited.
    /// </summary>
    public void UnregisterConveyor(ConveyorTile conveyor)
    {
        _activeConveyors.Remove(conveyor);
    }

    /// <summary>
    /// Sums the velocity from all conveyor tiles the player is currently standing on.
    /// Sprinting overrides the conveyor effect (returns zero).
    /// </summary>
    private Vector2 GetConveyorVelocity()
    {
        if (Input.IsActionPressed("player_sprint") && _activeConveyors.Count > 0)
            return Vector2.Zero;

        Vector2 total = Vector2.Zero;
        foreach (var conveyor in _activeConveyors)
            total += conveyor.GetConveyorVelocity(this);
        return total;
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
        AddToGroup("persist");
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GameLogger.Error("Player", "Multiple instances of Player detected!");
        }

        CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.InteractableLayer;

        AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _dash = GetNode<Dash>("Dash");
        Camera = GetNode<PlayerCamera>("PlayerCamera");
        InteractionPrompt = GetNode<PlayerInteractionPrompt>("InteractionPrompt");

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
        GameLogger.Debug("Player", $"_Ready: HealthComponent HP={HealthComponent.MaxHP}, Parry={(Parry != null ? "present" : "null")}");
    }

    public override void _Process(double delta)
    {
        // Movement is gated by InInteraction to prevent input during cutscenes/dialogs
         if (!_inInteraction)
        {
            HandleMovement(delta);
        }
    }

    /// <summary>
    /// Reads input vector, applies sprint/dash modifiers, moves the player,
    /// and pushes adjacent PushBlock obstacles in the slide direction.
    /// </summary>
     private void HandleMovement(double delta)
    {
        Vector2 direction = Input.GetVector("player_left", "player_right", "player_up", "player_down");
        if (direction.Length() > 1.0f)
        {
            direction = direction.Normalized();
        }

        if (direction != Vector2.Zero)
            FacingDirection = direction;

        float speed = PlayerSpeed * (1 + Equipment.Instance.TotalSpeedBoost / 100f);
        Velocity = direction * Mathf.Max(PlayerSpeed * 0.5f, speed);
            // Dash: overrides movement direction and speed for a brief burst
             if (Input.IsActionJustPressed("dash"))
        {
            GameLogger.Debug("Player", $"Dash pressed — direction={direction}, dashing={_dash.IsDashing()}");
            _dash.StartDash(direction);
        }
        else if (Input.IsActionPressed("player_sprint"))
        {
            if (!_dash.IsDashing()) Velocity *= SprintScale;
        }

        if (_dash.IsDashing())
        {
            Velocity *= _dash.DashScale;
        }

        // Apply conveyor velocity (additive). Sprinting overrides conveyor effect.
        Velocity += GetConveyorVelocity();

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

        UpdateAnimation(direction);
    }

    /// <summary>
    /// Updates the player's animation state based on movement direction.
    /// Plays idle variants when stopped; walk_left/right/back/forward when moving.
    /// </summary>
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
            GameLogger.Debug("Player", "Debug combat triggered — OatmealArena");
            CombatController.Instance?.EnterCombat(
                "res://combat/arena/OatmealArena.tscn",
                Vector2.Zero
            );
        }

        if (@event.IsActionPressed("debug_start_combat_eggroller"))
        {
            GameLogger.Debug("Player", "Debug combat triggered — EggrollerArena");
            CombatController.Instance?.EnterCombat(
                "res://combat/arena/EggrollerArena.tscn",
                Vector2.Zero
            );
        }

        if (@event.IsActionPressed("check"))
        {
            GameLogger.Debug("Player", "Check action pressed");
            PerformCheck();
        }
    }

    private void PerformCheck()
    {
        if (DialogManager.Instance.IsDialogActive)
        {
            GameLogger.Debug("Player", "Check skipped — dialog active");
            return;
        }
        if (CutsceneController.Instance.IsPlaying)
        {
            GameLogger.Debug("Player", "Check skipped — cutscene playing");
            return;
        }

        // Scan for the nearest CheckableComponent in the facing direction
        var space = GetWorld2D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters2D();

        // Use a rectangle slightly larger than the player, offset in facing direction
        var rect = new RectangleShape2D
        {
            Size = new Vector2(48, 48)
        };
        query.Shape = rect;
        query.CollisionMask = CollisionConfig.InteractableLayer;
        query.Transform = new Transform2D(0f, Position + FacingDirection * 40);
        query.CollideWithAreas = true;
        query.CollideWithBodies = false;

        var results = space.IntersectShape(query);
        if (results.Count == 0)
        {
            GameLogger.Debug("Player", $"Check: no interactables found at {Position + FacingDirection * 40}");
            return;
        }

        CheckableComponent closest = null;
        float closestDist = float.MaxValue;

        foreach (var result in results)
        {
            if (result["collider"].Obj is CheckableComponent checkable &&
                !string.IsNullOrEmpty(checkable.CheckLine))
            {
                float dist = GlobalPosition.DistanceSquaredTo(checkable.GlobalPosition);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = checkable;
                }
            }
        }

        if (closest != null)
        {
            GameLogger.Debug("Player", $"Check: found '{closest.Name}' — '{closest.CheckLine}'");
            var lines = new System.Collections.Generic.List<string> { closest.CheckLine };
            DialogManager.Instance.StartDialog(lines, closest.GetNodeOrNull<DialogVoiceResource>("Voice"));
        }
        else
        {
            GameLogger.Debug("Player", $"Check: {results.Count} area(s) found but none with CheckLine");
        }
    }

    public void StartInteraction()
    {
        GameLogger.Debug("Player", "Interaction started (movement locked)");
        _inInteraction = true;
    }

    public void EndInteraction()
    {
        GameLogger.Debug("Player", "Interaction ended (movement unlocked)");
        _inInteraction = false;
    }



    // --- ISavable ---

    public string SaveKey => "player";
    /// <summary>Serializes player state to a dictionary for save storage.</summary>
    /// <returns>Dictionary with position, health, facing direction, and level scene path.</returns>
     public Dictionary<string, Variant> Serialize()
    {
        string levelPath = GameController.Instance.CurrentLevel?.SceneFilePath ?? "res://levels/overworld/maps/Overworld.tscn";
        GameLogger.Debug("Player", $"Serialize: pos={Position}, hp={HealthComponent.CurrentHP}, scene={levelPath}");
        return new Dictionary<string, Variant>
        {
            ["position_x"] = Position.X,
            ["position_y"] = Position.Y,
            ["health"] = HealthComponent.CurrentHP,
            ["facing_dir"] = FacingDirection.ToString(),
            ["level_scene_path"] = levelPath
        };
    }

    /// <summary>Restores player state from saved dictionary data.</summary>
    /// <param name="data">Serialized dictionary with position, health, and level scene path.</param>
     public void Deserialize(Dictionary<string, Variant> data)
    {
        GameLogger.Debug("Player", $"Deserialize: data keys=[{string.Join(",", data.Keys)}]");
        int health = 100;
        if (data.TryGetValue("health", out var healthVar))
            health = healthVar.AsInt32();
        HealthComponent.CurrentHP = health;
        GameLogger.Debug("Player", $"Deserialize: health={health}");

        string scenePath = "";
        if (data.TryGetValue("level_scene_path", out var pathVar))
            scenePath = pathVar.AsString();

        float posX = 0f, posY = 0f;
        if (data.TryGetValue("position_x", out var xVar))
            posX = (float)xVar.AsDouble();
        if (data.TryGetValue("position_y", out var yVar))
            posY = (float)yVar.AsDouble();
        Vector2 position = new(posX, posY);
        GameLogger.Debug("Player", $"Deserialize: scenePath='{scenePath}', position={position}");

        // Safety: guard against empty/invalid paths
        if (string.IsNullOrEmpty(scenePath))
        {
            GameLogger.Warn("Player", "Deserialize: no valid scene path in save, using default.");
            scenePath = "res://levels/overworld/maps/Overworld.tscn";
        }

        GameLogger.Info("Player", $"Deserialize: calling LoadLevel(scenePath='{scenePath}', pos={position})");
        GameController.Instance.LoadLevel(scenePath, position);
        GameLogger.Info("Player", $"Deserialize: LoadLevel returned (async, level may not be loaded yet).");
    }

    /// <summary>Returns the load priority. Player (10) loads before Equipment (5), Inventory (0), WorldFlags (0).</summary>
    /// <returns>Load priority value; higher values load first.</returns>
     public int GetLoadPriority() => 10;

    private bool _deathInProgress = false;

    /// <summary>
    /// Handles player death: defers if in combat arena, shows "You collapsed..." dialog,
    /// then reloads from the last save via SaveManager. Prevents double-triggering with _deathInProgress guard.
    /// </summary>
     private async void OnPlayerDied()
    {
        if (_deathInProgress) return;
        _deathInProgress = true;
        GameLogger.Info("Player", "Player died — reloading from last save.");

        if (GameController.Instance?.CurrentLevel is CombatArena arena)
        {
            GameLogger.Debug("Player", "In combat arena — deferring death handling to CombatArena.OnPlayerDied");
            _deathInProgress = false;
            return;
        }

        HealthComponent.Died -= OnPlayerDied;

        var lines = new System.Collections.Generic.List<string> { "You collapsed..." };
        DialogManager.Instance.StartDialog(lines);
        await ToSignal(DialogManager.Instance, DialogManager.SignalName.DialogFinished);

        // Full reload from last save point (SaveManager.LoadGame triggers level load + restore)
        bool loaded = SaveManager.Instance.LoadGame();
        GameLogger.Info("Player", $"Save reload {(loaded ? "succeeded" : "FAILED — no save restored")}");

        HealthComponent.Died += OnPlayerDied;
        _deathInProgress = false;
    }


}
