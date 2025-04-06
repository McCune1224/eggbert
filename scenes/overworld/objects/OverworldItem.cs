using Godot;
using System;


public partial class OverworldItem : Area2D
{
    CollisionShape2D _collision;

    public override void _Ready()
    {
        CollisionLayer = CollisionConfig.ItemLayer;
        CollisionMask = CollisionConfig.ItemMask;
        // Get the collision shape
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
    }
}
