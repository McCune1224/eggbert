class_name DialogNode
extends Resource

@export var id: String = ""
@export var speaker_name: String = ""
@export var voice: Resource
@export_multiline var lines: Array[String] = []
@export var responses: Array[DialogResponse] = []
@export var condition: Resource
@export var set_flags_on_enter: Array[String] = []
