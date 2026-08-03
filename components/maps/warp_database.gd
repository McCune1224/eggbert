class_name WarpDatabase
extends RefCounted

static var warps: Dictionary[String, Dictionary] = {}

static func register(warp_id: String, scene_path: String, transition_name: String = "") -> void:
	warps[warp_id] = {"scene_path": scene_path, "transition_name": transition_name}

static func get_warp(warp_id: String) -> Dictionary:
	return warps.get(warp_id, {})
