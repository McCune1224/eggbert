class_name HealthComponent
extends Node

signal damaged(amount: int, source: Node)
signal healed(amount: int)
signal died
signal revived

@export var max_hp: int = 100
@export var current_hp: int = 0
@export var defense: int = 0

var is_dead: bool:
	get:
		return current_hp <= 0

func _ready() -> void:
	if current_hp <= 0:
		current_hp = max_hp

func take_damage(raw_damage: int, source: Node = null) -> void:
	if is_dead:
		return
	var amount: int = maxi(1, raw_damage - defense)
	current_hp = maxi(0, current_hp - amount)
	damaged.emit(amount, source)
	if current_hp == 0:
		died.emit()

func heal(amount: int) -> void:
	if is_dead:
		return
	var before: int = current_hp
	current_hp = mini(max_hp, current_hp + maxi(0, amount))
	healed.emit(current_hp - before)

func set_max_hp(new_max: int, refill: bool = false) -> void:
	max_hp = maxi(1, new_max)
	current_hp = max_hp if refill else mini(current_hp, max_hp)

func revive(hp_percent: int = 50) -> void:
	current_hp = maxi(1, maxi(0, max_hp * hp_percent / 100))
	revived.emit()
