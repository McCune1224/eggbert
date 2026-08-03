class_name ItemDatabase
extends RefCounted

static var all: Dictionary[String, Item] = {
	"rusty_key": _item("rusty_key", "Rusty Key", Item.ItemCategory.KEY, "An old rusted key. Probably opens something nearby."),
	"cell_key": _item("cell_key", "Cell Key", Item.ItemCategory.KEY, "A heavy iron key marked with a 'C'."),
	"hardboiled_egg": _consumable("hardboiled_egg", "Hardboiled Egg", 30),
	"scrambled_egg": _consumable("scrambled_egg", "Scrambled Egg", 60),
	"butter_knife": _equipment("butter_knife", "Butter Knife", Item.EquipSlot.WEAPON, 3, 0, 0),
	"egg_shell": _equipment("egg_shell", "Egg Shell", Item.EquipSlot.ARMOR, 0, 5, 0),
	"lucky_yolk": _equipment("lucky_yolk", "Lucky Yolk", Item.EquipSlot.ACCESSORY, 0, 0, 2),
	"baseball_bat": _equipment("baseball_bat", "Baseball Bat", Item.EquipSlot.WEAPON, 5, 0, 0),
	"soda_can_armor": _equipment("soda_can_armor", "Soda Can Armor", Item.EquipSlot.ARMOR, 0, 8, 0),
	"dice": _equipment("dice", "Dice", Item.EquipSlot.ACCESSORY, 3, 3, 0),
	"eggshell_helm": _equipment("eggshell_helm", "Eggshell Helm", Item.EquipSlot.ARMOR, 0, 4, 0, 10),
	"eggdrop_soup": _consumable("eggdrop_soup", "Eggdrop Soup", 25),
	"deviled_egg": _consumable("deviled_egg", "Deviled Egg", 20),
	"egg_salad_sandwich": _consumable("egg_salad_sandwich", "Egg Salad Sandwich", 45),
	"golden_yolk": _item("golden_yolk", "Golden Yolk", Item.ItemCategory.KEY, "A radiant yolk that pulses with warmth."),
	"warden_key": _item("warden_key", "Warden's Key", Item.ItemCategory.KEY, "A heavy brass key stamped with the warden's seal."),
}

static func get_item(item_id: String) -> Item:
	return all.get(item_id)

static func _item(item_id: String, title: String, category: Item.ItemCategory, details: String) -> Item:
	var item := Item.new()
	item.id = item_id
	item.display_name = title
	item.category = category
	item.description = details
	return item

static func _consumable(item_id: String, title: String, heal: int) -> Item:
	var item := _item(item_id, title, Item.ItemCategory.CONSUMABLE, "Restores %d HP." % heal)
	item.heal_amount = heal
	return item

static func _equipment(item_id: String, title: String, slot: Item.EquipSlot, attack: int, defense: int, speed: int, max_hp: int = 0) -> Item:
	var item := _item(item_id, title, Item.ItemCategory.EQUIPMENT, "")
	item.slot = slot
	item.attack_boost = attack
	item.defense_boost = defense
	item.speed_boost = speed
	item.max_hp_boost = max_hp
	return item
