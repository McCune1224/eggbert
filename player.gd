extends CharacterBody2D


var speed = 150
enum Direction { UP, DOWN, LEFT, RIGHT }

# Default direction for the enum
var player_direction = Direction.DOWN



@onready var ap = $PlayerSprite/AnimationPlayer

func get_player_input() -> Vector2:
	var direction = Vector2.ZERO
	if Input.is_action_pressed("player_right"):
		direction.x += 1
	if Input.is_action_pressed("player_left"):
		direction.x -= 1
	if Input.is_action_pressed("player_down"):
		direction.y += 1
	if Input.is_action_pressed("player_up"):
		direction.y -= 1
		
	# Normalize to prevent faster diagonal movement
	direction = direction.normalized()
	return direction

func _physics_process(delta):
	var direction = get_player_input()
	velocity = direction * speed
	move_and_slide()
	pass

func _process(delta: float) -> void:
	var direction = get_player_input()
	_set_player_animation("")
	pass

func _set_player_animation(previous_direction: String):
	if Input.is_action_pressed("player_right"):
		ap.play("walking_right")
	if Input.is_action_pressed("player_left"):
		ap.play("walking_left")
	if Input.is_action_pressed("player_up"):
		ap.play("walking_up")
	if Input.is_action_pressed("player_down"):
		ap.play("walking_down")
	pass
