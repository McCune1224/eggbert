using Godot;
using System;

public partial class Dash : Node2D
{
    // [Export]
    // private float defaultDashSpeed = 300.0f; // Speed of the dash
    [Export]
    public float DashScale = 3f; // Scale factor for the dash effect
    [Export]
    private float defaultDashDuration = 0.2f; // Duration of the dash in seconds
    [Export]
    private float _dashCooldown = 0.4f; // Cooldown time after a dash
    private bool _canDash = true;
    private Vector2 _dashDirection = Vector2.Zero; // Direction of the dash

    private Timer _durationTimer;
    private Timer _ghostTimer;
    private PackedScene _dashGhostAnmiationPlayerScene;

    public override void _Ready()
    {
        _durationTimer = GetNode<Timer>("DurationTimer");
        _durationTimer.Timeout += StopDash;

        _ghostTimer = GetNode<Timer>("GhostTimer");
        _ghostTimer.Timeout += () =>
        {
            GD.Print("Ghost timer completed");
            InstantiateDashGhost();
        };

        _dashGhostAnmiationPlayerScene = ResourceLoader.Load<PackedScene>("res://autoload/player/animations/DashGhost.tscn");

    }

    private void InstantiateDashGhost()
    {
        Sprite2D newGhost = _dashGhostAnmiationPlayerScene.Instantiate<Sprite2D>();
        // Sprite2D currentPlayerSprite = Player.Instance.AnimationPlayer;
        Sprite2D playerSprite = Player.Instance.GetNode<Sprite2D>("Sprite2D");

        newGhost.Modulate = new Color(1, 1, 1, 0.5f); // Set alpha to 50% for semi-transparency (white color)
        newGhost.GlobalPosition = Player.Instance.GlobalPosition;
        newGhost.Texture = playerSprite.Texture;
        newGhost.Vframes = playerSprite.Vframes;
        newGhost.Hframes = playerSprite.Hframes;
        newGhost.Frame = playerSprite.Frame;
        newGhost.FlipH = playerSprite.FlipH;
        //make ghost semi-transparent:
        // newGhost.ZIndex = playerSprite.ZIndex - 1; // Ensure the ghost is rendered behind the player?


        GetTree().Root.AddChild(newGhost);
    }


    public Vector2 StartDash(Vector2 direction)
    {
        if (!_canDash)
        {
            GD.Print("Cannot dash yet, still on cooldown.");
            return Vector2.Zero;
        }


        InstantiateDashGhost();
        _ghostTimer.Start();
        _durationTimer.Start(defaultDashDuration);
        _canDash = false;
        _dashDirection = direction.Normalized();

        Sprite2D playerSprite = Player.Instance.GetNode<Sprite2D>("Sprite2D");
        Shader whiteShader = ResourceLoader.Load<Shader>("res://autoload/player/animations/DashGhost.gdshader");
        ShaderMaterial shader = new ShaderMaterial();
        shader.Shader = whiteShader;
        playerSprite.Material = shader; // Apply the shader to the player's sprite


        return _dashDirection;
    }


    public void StopDash()
    {
        _ghostTimer.Stop();
        GetTree().CreateTimer(_dashCooldown).Timeout += () =>
        {

            GD.Print("Dash cooldown completed"); _canDash = true;
            _durationTimer.Stop();
        };
        Player.Instance.GetNode<Sprite2D>("Sprite2D").Material = null; // Reset the player's material to remove the dash effect
    }

    public bool IsDashing()
    {
        return !_durationTimer.IsStopped();
    }
}
