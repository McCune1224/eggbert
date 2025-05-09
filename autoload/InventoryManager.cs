using Godot;
using System;

public partial class InventoryManager : Node
{
    private static InventoryManager _instance;
    public static InventoryManager Instance => _instance;
    private Inventory _inventory;

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }
}
