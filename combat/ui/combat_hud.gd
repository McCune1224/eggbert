class_name CombatHUD
extends CanvasLayer

const BAR_WIDTH: float = 140.0
const BAR_HEIGHT: float = 12.0
const ENEMY_BAR_WIDTH: float = 110.0
const ENEMY_BAR_HEIGHT: float = 8.0
const BAR_BACKGROUND := Color(0.1, 0.1, 0.15, 0.9)
const PLAYER_BAR := Color(0.91, 0.72, 0.38)
const ENEMY_BAR := Color(0.88, 0.41, 0.41)
const LOW_HP := Color(0.91, 0.45, 0.1)
const CRITICAL_HP := Color(0.88, 0.41, 0.41)

class EnemyEntry:
	var health: HealthComponent
	var label: Label
	var fill: ColorRect

var _player_health: HealthComponent
var _player_fill: ColorRect
var _enemy_list: VBoxContainer
var _enemy_entries: Array[EnemyEntry] = []

func _ready() -> void:
	layer = 128
	_build_player_panel()
	_build_enemy_panel()

func set_player_health_component(health: HealthComponent) -> void:
	if _player_health != null:
		if _player_health.damaged.is_connected(_on_player_damaged):
			_player_health.damaged.disconnect(_on_player_damaged)
		if _player_health.healed.is_connected(_on_player_healed):
			_player_health.healed.disconnect(_on_player_healed)
	_player_health = health
	if _player_health != null:
		_player_health.damaged.connect(_on_player_damaged)
		_player_health.healed.connect(_on_player_healed)
	_update_player_bar()

func add_enemy(display_name: String, health: HealthComponent) -> void:
	if _enemy_list == null or health == null:
		return
	var row := VBoxContainer.new()
	row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var label := Label.new()
	label.text = display_name
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.add_theme_font_size_override("font_size", 9)
	row.add_child(label)
	var bar_container := Control.new()
	bar_container.custom_minimum_size = Vector2(ENEMY_BAR_WIDTH, ENEMY_BAR_HEIGHT)
	bar_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var background := ColorRect.new()
	background.size = Vector2(ENEMY_BAR_WIDTH, ENEMY_BAR_HEIGHT)
	background.color = BAR_BACKGROUND
	background.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var fill := ColorRect.new()
	fill.size = Vector2(ENEMY_BAR_WIDTH, ENEMY_BAR_HEIGHT)
	fill.color = ENEMY_BAR
	fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar_container.add_child(background)
	bar_container.add_child(fill)
	row.add_child(bar_container)
	_enemy_list.add_child(row)
	var entry := EnemyEntry.new()
	entry.health = health
	entry.label = label
	entry.fill = fill
	_enemy_entries.append(entry)
	health.damaged.connect(_on_enemy_changed.bind(entry))
	health.healed.connect(_on_enemy_changed.bind(entry))
	_update_enemy_bar(entry)

func _build_player_panel() -> void:
	var panel := PanelContainer.new()
	panel.position = Vector2(8, 8)
	panel.custom_minimum_size = Vector2(BAR_WIDTH + 16, 0)
	var box := VBoxContainer.new()
	var label := Label.new()
	label.text = "HP"
	label.add_theme_font_size_override("font_size", 11)
	box.add_child(label)
	var container := Control.new()
	container.custom_minimum_size = Vector2(BAR_WIDTH, BAR_HEIGHT)
	var background := ColorRect.new()
	background.size = Vector2(BAR_WIDTH, BAR_HEIGHT)
	background.color = BAR_BACKGROUND
	_player_fill = ColorRect.new()
	_player_fill.size = Vector2(BAR_WIDTH, BAR_HEIGHT)
	_player_fill.color = PLAYER_BAR
	container.add_child(background)
	container.add_child(_player_fill)
	box.add_child(container)
	panel.add_child(box)
	add_child(panel)

func _build_enemy_panel() -> void:
	var panel := PanelContainer.new()
	panel.position = Vector2(514, 8)
	panel.custom_minimum_size = Vector2(118, 0)
	_enemy_list = VBoxContainer.new()
	_enemy_list.add_theme_constant_override("separation", 2)
	_enemy_list.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.add_child(_enemy_list)
	add_child(panel)

func _update_player_bar() -> void:
	if _player_fill == null or _player_health == null:
		return
	var ratio := clampf(float(_player_health.current_hp) / float(maxi(1, _player_health.max_hp)), 0.0, 1.0)
	_player_fill.size.x = BAR_WIDTH * ratio
	_player_fill.color = CRITICAL_HP if ratio <= 0.25 else (LOW_HP if ratio <= 0.5 else PLAYER_BAR)

func _update_enemy_bar(entry: EnemyEntry) -> void:
	if entry == null or entry.health == null or not is_instance_valid(entry.fill):
		return
	var ratio := clampf(float(entry.health.current_hp) / float(maxi(1, entry.health.max_hp)), 0.0, 1.0)
	entry.fill.size.x = ENEMY_BAR_WIDTH * ratio
	entry.fill.color = CRITICAL_HP if ratio <= 0.25 else (LOW_HP if ratio <= 0.5 else ENEMY_BAR)
	entry.label.modulate = Color(0.53, 0.53, 0.53) if entry.health.is_dead else Color.WHITE

func _on_player_damaged(_amount: int, _source: Node) -> void:
	_update_player_bar()

func _on_player_healed(_amount: int) -> void:
	_update_player_bar()

func _on_enemy_changed(_amount: int, entry: EnemyEntry) -> void:
	_update_enemy_bar(entry)

func _exit_tree() -> void:
	if _player_health != null:
		if _player_health.damaged.is_connected(_on_player_damaged):
			_player_health.damaged.disconnect(_on_player_damaged)
		if _player_health.healed.is_connected(_on_player_healed):
			_player_health.healed.disconnect(_on_player_healed)
