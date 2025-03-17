using Godot;
using System;
using static IInteractable;

public partial class Overworld : Node2D
{
    // Nodes that will be assigned in the scene
    private CharacterBody2D _player;
    private Camera2D _camera;
    private TileMapLayer _groundTileMap;
    private TileMapLayer _collisionTileMap;
    private CanvasLayer _uiLayer;

    // Constants for movement
    private const float PLAYER_SPEED = 150.0f;

    // Interaction area
    private Area2D _interactionArea;

    // State tracking
    private bool _inInteraction = false;
    private Godot.Collections.Dictionary<string, Vector2> _entrancePoints = new Godot.Collections.Dictionary<string, Vector2>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Connect to OverworldManager if needed
        var overworldManager = GameController.Instance;
        _player = Player.Instance;

        // Initialize entrance points (locations where the player can enter this map)
        _entrancePoints.Add("starting_area", new Vector2(100, 100));
        _entrancePoints.Add("ruins_entrance", new Vector2(300, 50));
        _entrancePoints.Add("snowdin_path", new Vector2(500, 200));

        // Setup player position based on the current area
        SetupPlayerPosition();

        // Initialize interaction area
        SetupInteractionArea();
    }

    private void SetupPlayerPosition()
    {
        // Find the player node
        _player = Player.Instance;
        _camera = _player.GetNode<Camera2D>("Camera2D");

        // Get the current area from OverworldManager
        string currentArea = GameController.Instance.CurrentArea;

        // Position player at the appropriate entrance point
        if (_entrancePoints.ContainsKey(currentArea))
        {
            _player.Position = _entrancePoints[currentArea];
        }
        else
        {
            // Default position if area not found
            _player.Position = _entrancePoints["starting_area"];
        }

        // Update the stored position in the manager
        GameController.Instance.SetPlayerPosition(_player.Position);
    }

    private void SetupInteractionArea()
    {
        // Create an interaction area that follows the player
        _interactionArea = new Area2D();
        _interactionArea.Name = "InteractionArea";

        var collisionShape = new CollisionShape2D();
        var shape = new RectangleShape2D();
        shape.Size = new Vector2(20, 20);
        collisionShape.Shape = shape;

        _interactionArea.AddChild(collisionShape);
        _player.AddChild(_interactionArea);

        // Position it slightly in front of the player
        _interactionArea.Position = new Vector2(0, 20);

        // Connect signals
        _interactionArea.BodyEntered += OnInteractionAreaBodyEntered;
        _interactionArea.BodyExited += OnInteractionAreaBodyExited;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!_inInteraction)
        {
            HandlePlayerMovement(delta);
        }
    }

    // Handle keyboard input for player movement
    private void HandlePlayerMovement(double delta)
    {
        // Get input direction
        Vector2 direction = Input.GetVector("player_left", "player_right", "player_up", "player_down");

        // Normalize the vector to ensure consistent movement speed in all directions
        if (direction.Length() > 1.0f)
        {
            direction = direction.Normalized();
        }

        // Set the velocity and move the player
        _player.Velocity = direction * PLAYER_SPEED;
        _player.MoveAndSlide();

        // Update the player position in the manager
        GameController.Instance.SetPlayerPosition(_player.Position);

        // Handle interaction input (typically Z key in Undertale)
        if (Input.IsActionJustPressed("ui_accept"))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // Get all bodies in interaction area
        var bodies = _interactionArea.GetOverlappingBodies();

        foreach (var body in bodies)
        {
            // Check if it's an interactable object
            if (body is IInteractable interactable)
            {
                interactable.Interact();
                return;
            }

            // Or check for an Interact method
            if (body is Node node && node.HasMethod("Interact"))
            {
                node.Call("Interact");
                return;
            }
        }
    }

    private void OnInteractionAreaBodyEntered(Node2D body)
    {
        // Visual indicator or hint that interaction is possible
        if (body is IInteractable || (body is Node node && node.HasMethod("Interact")))
        {
            GD.Print("Interaction available");
            // Here you could show a visual indicator
        }
    }

    private void OnInteractionAreaBodyExited(Node2D body)
    {
        // Remove visual indicator
        if (body is IInteractable || (body is Node node && node.HasMethod("Interact")))
        {
            GD.Print("Interaction no longer available");
            // Here you could hide the visual indicator
        }
    }

    // Method to start a dialogue or cutscene
    public void StartInteraction()
    {
        _inInteraction = true;
    }

    // Method to end a dialogue or cutscene
    public void EndInteraction()
    {
        _inInteraction = false;
    }
}
