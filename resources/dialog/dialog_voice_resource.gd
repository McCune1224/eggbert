class_name DialogVoiceResource
extends Resource

@export var voice_stream: AudioStream
@export var speaker_name: String = ""
@export var portrait: Texture2D
@export_range(0.01, 2.0) var base_pitch := 1.0
@export_range(0.0, 1.0) var consonant_pitch_variance := 0.1
@export var volume_db := 0.0
