using Godot;
using System;
using System.Collections.Generic;

public class Inventory
{
    public List<Item> Items { get; private set; }


    public Inventory()
    {
        Items = new List<Item>();
    }

    public void AddItem(Item item)
    {
        Items.Add(item);
    }

    // This method will be used to get the items that are usable in combat
    // For now, it just returns all items
    public List<Item> GetItemsByType(ItemType type)
    {
        List<Item> combatItems = new List<Item>();
        foreach (var item in Items)
        {
            if (item.Type == type)
            {
                combatItems.Add(item);
            }
        }
        return combatItems;
    }
}
