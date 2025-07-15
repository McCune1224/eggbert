using Godot;

public enum ItemType
{
    Key,
    Weapon,
    Puzzle
}

public class Item
{
    public readonly ItemType Type;
}
