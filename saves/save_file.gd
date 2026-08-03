class_name SaveFile
extends Resource

@export var save_point_scene_path: String = ""
@export var save_point_position: Vector2 = Vector2.ZERO
@export var location_name: String = ""
@export var save_timestamp: float = 0.0
@export var play_time_seconds: float = 0.0
@export var component_data: Dictionary[String, Variant] = {}
