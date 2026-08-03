class_name FontCache
extends RefCounted

static var _yoster: Font

static func get_yoster() -> Font:
	if _yoster == null:
		_yoster = load("res://assets/fonts/yoster.ttf") as Font
	return _yoster
