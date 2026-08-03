class_name WarpDatabase
extends RefCounted
## Static registry of warp destinations. A destination is unlocked via the
## WorldFlag "warp_<id>"; the map menu lists unlocked destinations.

static var warps: Dictionary[String, Dictionary] = {
	"overworld_entry": {"name": "Overworld", "level_path": "res://levels/overworld/maps/Overworld.tscn", "target_transition_name": "HubArrival"},
	"the_great_beyond": {"name": "The Great Beyond", "level_path": "res://levels/overworld/maps/TheGreatBeyond.tscn", "target_transition_name": "HubArrival"},
	"courtyard": {"name": "Courtyard", "level_path": "res://levels/courtyard/maps/courtyard.tscn", "target_transition_name": "HubArrival"},
	"eggsile_area1": {"name": "Eggsile — Area 1", "level_path": "res://levels/eggsile/maps/area1.tscn", "target_transition_name": "HubArrival"},
	"prison": {"name": "Prison", "level_path": "res://levels/prison/maps/prison.tscn", "target_transition_name": "HubArrival"},
	"factory_gate": {"name": "Factory Gate", "level_path": "res://levels/factory/maps/OpeningZone.tscn", "target_transition_name": "HubArrival"},
	"courtyard_depths": {"name": "Courtyard Depths", "level_path": "res://levels/courtyard/maps/CourtyardDepths.tscn", "target_transition_name": "HubArrival"},
	"prison_block_c": {"name": "Prison Block C", "level_path": "res://levels/prison/maps/PrisonBlockC.tscn", "target_transition_name": "HubArrival"},
	"kitchen": {"name": "Kitchen", "level_path": "res://levels/kitchen/maps/Kitchen.tscn", "target_transition_name": "HubArrival"},
	"wardens_quarters": {"name": "Warden's Quarters", "level_path": "res://levels/warden/maps/WardensQuarters.tscn", "target_transition_name": "HubArrival"},
	"rec_room": {"name": "Rec Room", "level_path": "res://levels/recroom/maps/RecRoom.tscn", "target_transition_name": "HubArrival"},
	"secret_tunnels": {"name": "Secret Tunnels", "level_path": "res://levels/tunnels/maps/SecretTunnels.tscn", "target_transition_name": "HubArrival"},
	"sunnyside_shrine": {"name": "Sunnyside Shrine", "level_path": "res://levels/shrine/maps/SunnysideShrine.tscn", "target_transition_name": "HubArrival"},
	"solitary": {"name": "Solitary", "level_path": "res://levels/solitary/maps/Solitary.tscn", "target_transition_name": "HubArrival"},
	"prison_tunnels": {"name": "Prison Tunnels", "level_path": "res://levels/prison/maps/prison.tscn", "target_transition_name": "HubArrival"},
	"eggsile_sewers": {"name": "Eggsile Sewers", "level_path": "res://levels/eggsile/maps/EggsileSewers.tscn", "target_transition_name": "HubArrival"},
}

static func register(warp_id: String, scene_path: String, transition_name: String = "") -> void:
	warps[warp_id] = {"name": warp_id, "level_path": scene_path, "target_transition_name": transition_name}

static func get_warp(warp_id: String) -> Dictionary:
	return warps.get(warp_id, {})

static func is_unlocked(warp_id: String) -> bool:
	var flags := _flags_node()
	return flags != null and flags.has_method("has_flag") and bool(flags.call("has_flag", "warp_" + warp_id))

static func unlock(warp_id: String) -> void:
	var flags := _flags_node()
	if flags != null and flags.has_method("set_flag"):
		flags.call("set_flag", "warp_" + warp_id, true)
	GameLogger.info("WarpDatabase", "Unlocked: '%s'" % warp_id)

static func get_unlocked() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for warp_id: String in warps:
		if is_unlocked(warp_id):
			result.append(warps[warp_id])
	GameLogger.debug("WarpDatabase", "GetUnlocked: %d/%d warps available" % [result.size(), warps.size()])
	return result

static func _flags_node() -> Node:
	var tree := Engine.get_main_loop() as SceneTree
	return tree.root.get_node_or_null("WorldFlags") if tree != null else null
