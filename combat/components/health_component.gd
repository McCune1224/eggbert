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
	GameLogger.debug("Health", "%s took %d damage (hp %d/%d, source %s)" % [name, amount, current_hp, max_hp, source.name if source != null else "null"])
	damaged.emit(amount, source)
	if current_hp == 0:
		GameLogger.info("Health", "%s died" % name)
		died.emit()

func heal(amount: int) -> void:
	if is_dead:
		return
	var before: int = current_hp
	current_hp = mini(max_hp, current_hp + maxi(0, amount))
	GameLogger.debug("Health", "%s healed %d (hp %d/%d)" % [name, current_hp - before, current_hp, max_hp])
	healed.emit(current_hp - before)

func set_max_hp(new_max: int, refill: bool = false) -> void:
	max_hp = maxi(1, new_max)
	current_hp = max_hp if refill else mini(current_hp, max_hp)

func revive(hp_percent: int = 50) -> void:
	current_hp = maxi(1, maxi(0, max_hp * hp_percent / 100))
	revived.emit()
