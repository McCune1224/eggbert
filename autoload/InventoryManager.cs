using Godot;
using System;

public partial class InventoryManager : Node
{
    private static InventoryManager _instance;
    public static InventoryManager Instance => _instance;
    private Inventory _inventory;
}
