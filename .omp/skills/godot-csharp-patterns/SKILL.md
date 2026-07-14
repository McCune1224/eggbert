---
name: godot-csharp-patterns
description: Common Godot 4.7 C# patterns for this project — singletons, signals, exports, async, scene loading, and input handling.
---

## Autoload singleton pattern
```csharp
public partial class MySingleton : Node
{
    private static MySingleton _instance;
    public static MySingleton Instance => _instance;

    public override void _Ready()
    {
        if (_instance == null) _instance = this;
        else QueueFree();
    }
}
```
Registered in `project.godot` autoload section. Can be Node, Node2D, CanvasLayer, etc.

## Export fields
```csharp
// Godot C# uses [Export] not [ExportVar]
[Export] public float Speed = 200f;
[Export] public NodePath TargetPath { get; set; }
[Export] public PackedScene MyScene { get; set; }
[Export] public Resource MyResource { get; set; }
```

## Signal pattern
```csharp
[Signal] public delegate void MySignalEventHandler();
// Emit:
EmitSignal(SignalName.MySignal);
// Connect:
other.MySignal += Handler;
// Await:
await ToSignal(other, Other.SignalName.SomeSignal);
```

## Input handling
```csharp
// In _Input — fires for every node that has it, propagates up tree
public override void _Input(InputEvent @event)
{
    if (@event.IsActionPressed("interact"))
        GetViewport().SetInputAsHandled(); // prevent propagation
}

// In _Process — polling (only use when _Input isn't feasible)
if (Input.IsActionJustPressed("interact")) { ... }
```

## Scene loading
```csharp
// Instantiate a packed scene
var instance = sceneResource.Instantiate();
AddChild(instance);

// Load a resource by path
var res = ResourceLoader.Load<PackedScene>("res://path/to/scene.tscn");

// Load a level via GameController
GameController.Instance.LoadLevel("res://path/level.tscn", playerPosition);
GameController.Instance.LoadLevel("res://path/level.tscn", transitionName);
```

## Async/await with Godot signals (C#)
```csharp
// Await a signal
await ToSignal(someNode, SomeNode.SignalName.Finished);

// Await a timer
var timer = GetTree().CreateTimer(2.0f);
await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);

// Cancellation pattern
_cts = new CancellationTokenSource();
if (_cts.IsCancellationRequested) break;
```

## Collision layers
Use constants from `CollisionConfig` (not magic numbers):
```csharp
CollisionLayer = CollisionConfig.PlayerLayer;
CollisionMask = CollisionConfig.WallsLayer | CollisionConfig.NPCLayer;
```

## Node path resolution
```csharp
// Within current level (for placed NPCs)
var npc = GameController.Instance.CurrentLevel.GetNode(path);

// Relative to self
var child = GetNode<NodeType>("ChildName");

// Safe lookup
var maybe = GetNodeOrNull<NodeType>("OptionalChild");
```

## Common anti-patterns to avoid
- Don't call `GetTree()` in `_Ready()` on nodes not yet in tree — use `CallDeferred` or check `IsInsideTree()`
- Don't create Tweens in `_Process` — they accumulate and leak
- Don't use `Input.IsActionJustPressed()` from `_Input` — prefer `@event.IsActionPressed()`
- Don't hand-write .tres files with nested Resources — use the editor
