class_name Item
extends Resource

enum ItemCategory {
	KEY,
	CONSUMABLE,
	EQUIPMENT,
}

enum EquipSlot {
	NONE,
	WEAPON,
	ARMOR,
	ACCESSORY,
}

@export var id: String = ""
@export var display_name: String = ""
@export_multiline var description: String = ""
@export var icon: Texture2D
@export_multiline var description_used: String = ""
@export var category: ItemCategory = ItemCategory.KEY
@export var heal_amount: int = 0
@export var slot: EquipSlot = EquipSlot.NONE
@export var attack_boost: int = 0
@export var defense_boost: int = 0
@export var speed_boost: int = 0
@export var max_hp_boost: int = 0
@export var parry_radius_boost: float = 0.0
@export var parry_damage_boost: int = 0
