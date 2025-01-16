extends CharacterBody2D

@onready var up_sprite = $SpriteUp
@onready var down_sprite = $SpriteDown
@onready var left_sprite = $SpriteLeft
@onready var right_sprite = $SpriteRight

var speed = 100
enum FacedDirection { UP, DOWN, LEFT, RIGHT }

# Default direction for the enum
var player_direction = FacedDirection.DOWN




func get_player_input() -> Vector2:
	var direction = Vector2.ZERO
	if Input.is_action_pressed("ui_right"):
		direction.x += 1
	if Input.is_action_pressed("ui_left"):
		direction.x -= 1
	if Input.is_action_pressed("ui_down"):
		direction.y += 1
	if Input.is_action_pressed("ui_up"):
		direction.y -= 1
		
	# Normalize to prevent faster diagonal movement
	direction = direction.normalized()
	return direction

func _physics_process(delta):
	var direction = get_player_input()
	velocity = direction * speed
	move_and_slide()
	_set_player_animation()
	pass

func _set_player_animation():
	down_sprite.hide()
	up_sprite.hide()
	left_sprite.hide()
	right_sprite.hide()
	match get_player_input():
		Vector2(1,0):
			right_sprite.show()
		Vector2(-1,0):
			left_sprite.show()
		Vector2(0,1):
			down_sprite.show()
		Vector2(0,-1):
			up_sprite.show()
		_:
			down_sprite.show()
	pass
