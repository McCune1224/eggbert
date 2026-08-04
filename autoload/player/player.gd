extends CharacterBody2D

const PLAYER_SPEED: float = 150.0
const SPRINT_SCALE: float = 1.7
const MIN_EQUIPMENT_SPEED_SCALE: float = 0.5

var in_interaction: bool = false
var facing_direction: Vector2 = Vector2.DOWN
var animation_player: AnimationPlayer
var dash: Node
var camera: Camera2D
var interaction_prompt: Node
var health_component: Node
var parry_component: Node
var _death_in_progress: bool = false

func _ready() -> void:
	add_to_group("player")
	add_to_group("persist")
	collision_mask = 18
	animation_player = get_node_or_null("AnimationPlayer") as AnimationPlayer
	dash = get_node_or_null("Dash")
	camera = get_node_or_null("PlayerCamera") as Camera2D
	interaction_prompt = get_node_or_null("InteractionPrompt")
	health_component = get_node_or_null("HealthComponent")
	parry_component = get_node_or_null("ParryComponent")
	if health_component != null and health_component.has_signal("died"):
		health_component.died.connect(_on_health_died)
	if health_component != null and health_component.has_signal("damaged") and camera != null and camera.has_method("shake"):
		health_component.damaged.connect(_on_health_damaged)
	if parry_component != null and parry_component.has_signal("parried") and camera != null and camera.has_method("shake"):
		parry_component.parried.connect(_on_parried)
	if animation_player != null and animation_player.has_animation("idle forward"):
		animation_player.play("idle forward")

func _physics_process(_delta: float) -> void:
	if in_interaction:
		velocity = Vector2.ZERO
		_update_animation(Vector2.ZERO)
		return
	var direction := Input.get_vector("player_left", "player_right", "player_up", "player_down")
	if direction.length_squared() > 1.0:
		direction = direction.normalized()
	if direction != Vector2.ZERO:
		facing_direction = direction

	var speed := PLAYER_SPEED * _equipment_speed_scale()
	if Input.is_action_just_pressed("dash") and dash != null and dash.has_method("start_dash"):
		dash.call("start_dash", direction if direction != Vector2.ZERO else facing_direction)
	if dash != null and dash.has_method("is_dashing") and bool(dash.call("is_dashing")):
		var dash_direction: Vector2 = dash.get("dash_direction")
		if dash_direction == Vector2.ZERO:
			dash_direction = facing_direction
		velocity = dash_direction * speed * float(dash.get("dash_scale"))
	elif Input.is_action_pressed("player_sprint"):
		velocity = direction * speed * SPRINT_SCALE
	else:
		velocity = direction * speed
	move_and_slide()
	_push_colliders(direction)
	_update_animation(direction)

func _equipment_speed_scale() -> float:
	var equipment := get_tree().root.get_node_or_null("Equipment")
	if equipment == null:
		return 1.0
	var boost := float(equipment.get("total_speed_boost"))
	return maxf(MIN_EQUIPMENT_SPEED_SCALE, 1.0 + boost / 100.0)

func _push_colliders(direction: Vector2) -> void:
	var push_direction := direction.normalized()
	for index in range(get_slide_collision_count()):
		var collision := get_slide_collision(index)
		var collider := collision.get_collider()
		if collider != null and collider.has_method("try_push"):
			if push_direction == Vector2.ZERO:
				push_direction = -collision.get_normal()
			collider.call("try_push", push_direction)

func _update_animation(direction: Vector2) -> void:
	if animation_player == null:
		return
	var animation := ""
	if direction == Vector2.ZERO:
		var current := animation_player.current_animation
		if current.begins_with("walk "):
			animation = "idle " + current.trim_prefix("walk ")
		elif current.begins_with("idle "):
			return
		else:
			animation = _facing_animation("idle")
	else:
		animation = _facing_animation("walk", direction)
	if animation_player.has_animation(animation) and animation_player.current_animation != animation:
		animation_player.play(animation)

func _facing_animation(prefix: String, direction: Vector2 = facing_direction) -> String:
	if absf(direction.x) > absf(direction.y):
		return "%s %s" % [prefix, "left" if direction.x < 0.0 else "right"]
	return "%s %s" % [prefix, "back" if direction.y < 0.0 else "forward"]

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("check"):
		_perform_check()
	if event.is_action_pressed("debug_start_combat"):
		var combat := get_tree().root.get_node_or_null("CombatController")
		if combat != null and combat.has_method("enter_combat"):
			combat.call("enter_combat", "res://combat/arena/OatmealArena.tscn", Vector2.ZERO)

func _perform_check() -> void:
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	var cutscene := get_tree().root.get_node_or_null("CutsceneController")
	if (dialog != null and bool(dialog.get("is_dialog_active"))) or (cutscene != null and bool(cutscene.get("is_playing"))):
		return
	var query: PhysicsShapeQueryParameters2D = PhysicsShapeQueryParameters2D.new()
	var rectangle: RectangleShape2D = RectangleShape2D.new()
	rectangle.size = Vector2(48.0, 48.0)
	query.shape = rectangle
	query.collision_mask = 16
	query.transform = Transform2D(0.0, global_position + facing_direction * 40.0)
	query.collide_with_areas = true
	query.collide_with_bodies = false
	var results := get_world_2d().direct_space_state.intersect_shape(query)
	var nearest: Node = null
	var nearest_distance := INF
	for result in results:
		var candidate := result.get("collider") as Node
		if candidate == null or not candidate.has_method("get_check_line"):
			continue
		var line := str(candidate.call("get_check_line"))
		if line.is_empty():
			continue
		var distance := global_position.distance_squared_to(candidate.global_position)
		if distance < nearest_distance:
			nearest_distance = distance
			nearest = candidate
	if nearest != null and dialog != null and dialog.has_method("start_dialog"):
		GameLogger.info("Player", "Check interaction with %s (%s)" % [nearest.name, nearest.get_class()])
		var lines: Array[String] = [str(nearest.call("get_check_line"))]
		dialog.call("start_dialog", lines)

func start_interaction() -> void:
	in_interaction = true

func end_interaction() -> void:
	in_interaction = false

func set_initial_position(new_position: Vector2) -> void:
	global_position = new_position

func get_colliding_bodies() -> Array[Node2D]:
	var colliders: Array[Node2D] = []
	for index in range(get_slide_collision_count()):
		var collider := get_slide_collision(index).get_collider()
		if collider is Node2D:
			colliders.append(collider as Node2D)
	return colliders

func get_save_key() -> String:
	return "player"

func serialize() -> Dictionary[String, Variant]:
	return {
		"position": global_position,
		"facing_direction": facing_direction,
	}

func deserialize(data: Dictionary[String, Variant]) -> void:
	global_position = data.get("position", Vector2.ZERO) as Vector2
	facing_direction = data.get("facing_direction", Vector2.DOWN) as Vector2

func get_load_priority() -> int:
	return 10

func _on_health_damaged(_amount: Variant, _source: Variant = null) -> void:
	if camera != null and camera.has_method("shake"):
		camera.call("shake", 6.0, 0.3)

func _on_parried() -> void:
	if camera != null and camera.has_method("shake"):
		camera.call("shake", 4.0, 0.15)

func _on_health_died() -> void:
	if _death_in_progress:
		return
	_death_in_progress = true
	GameLogger.info("Player", "Player died; reloading last save")
	var save_manager := get_tree().root.get_node_or_null("SaveManager")
	var dialog := get_tree().root.get_node_or_null("DialogManager")
	if dialog != null and dialog.has_method("start_dialog"):
		var lines: Array[String] = ["You collapsed..."]
		dialog.call("start_dialog", lines)
	if save_manager != null and save_manager.has_method("load_game"):
		await get_tree().process_frame
		save_manager.call("load_game")
	_death_in_progress = false
