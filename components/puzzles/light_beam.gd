class_name LightBeam
extends Node2D

@export var beam_length: float = 400.0
@export var beam_color: Color = Color(1.0, 0.8, 0.2, 0.9)
@export var beam_width: float = 4.0
@export var max_reflections: int = 10

var _line: Line2D
var _direction: Vector2 = Vector2.RIGHT
var _active_sensor: LightSensor

func _ready() -> void:
	_line = Line2D.new()
	_line.width = beam_width
	_line.default_color = beam_color
	_line.antialiased = true
	add_child(_line)
	_cast_beam()

func set_direction(dir: Vector2) -> void:
	_direction = dir.normalized()
	_cast_beam()

func _cast_beam() -> void:
	var points: Array[Vector2] = [Vector2.ZERO]
	var origin := Vector2.ZERO
	var dir := _direction
	var space := get_world_2d().direct_space_state
	var reflections := 0
	var hit_sensor: LightSensor = null

	for _i in max_reflections:
		var query := PhysicsRayQueryParameters2D.create(
			global_position + origin,
			global_position + origin + dir * beam_length,
			CollisionConfig.WALLS_LAYER | CollisionConfig.TRIGGER_AREA_LAYER
		)
		query.collide_with_areas = true
		query.collide_with_bodies = true
		var result := space.intersect_ray(query)
		if result.is_empty():
			points.append(origin + dir * beam_length)
			break
		var hit_point := result["position"].as_vector2() - global_position
		points.append(hit_point)
		var collider := result["collider"] as Object
		if collider is LightMirror:
			var normal := result["normal"].as_vector2()
			dir = dir.bounce(normal).normalized()
			origin = hit_point
			reflections += 1
			continue
		if collider is LightSensor:
			hit_sensor = collider as LightSensor
			hit_sensor.activate()
			break
		break

	if _active_sensor != null and _active_sensor != hit_sensor:
		_active_sensor.deactivate()
	_active_sensor = hit_sensor
	_line.points = points
