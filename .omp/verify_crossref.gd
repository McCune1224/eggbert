extends SceneTree

# Verifies CutsceneCards.build_cross_refs and resolve_scene_node:
# TargetDoorPath is a scene-relative NodePath and must resolve to the
# actual scene node, not be treated as a file path.

func _initialize() -> void:
	var Cards: GDScript = load("res://addons/cutscene_inspector/cards.gd")
	if Cards == null:
		push_error("Failed to load cards.gd")
		quit(1)
		return

	# Build a small scene: root -> [FloorSwitch, Door] as siblings.
	var root := Node2D.new()
	root.name = "Level"
	get_root().add_child(root)

	var door := Node2D.new()
	door.name = "Door"
	root.add_child(door)

	# A bare Node2D with a TargetDoorPath export — we just need an object
	# whose `get("TargetDoorPath")` returns a NodePath. The helper reads
	# the property dynamically, so no script binding is required.
	var switch_node := Node2D.new()
	switch_node.name = "FloorSwitch"
	var switch_script := GDScript.new()
	switch_script.source_code = "extends Node2D\n@export var TargetDoorPath: NodePath = NodePath(\"\")\n"
	switch_script.reload()
	switch_node.set_script(switch_script)
	switch_node.set("TargetDoorPath", NodePath("../Door"))
	root.add_child(switch_node)
	await process_frame

	# Sanity: the helper resolves relative NodePaths from the source.
	var resolved = Cards.call("resolve_scene_node", switch_node, NodePath("../Door"))
	if resolved != door:
		push_error("resolve_scene_node should find the Door, got %s" % str(resolved))
		quit(1)
		return
	print("[xref] + resolve_scene_node('../Door') returns the Door")

	# A missing NodePath returns null.
	var missing = Cards.call("resolve_scene_node", switch_node, NodePath("../Missing"))
	if missing != null:
		push_error("resolve_scene_node should return null for missing, got %s" % str(missing))
		quit(1)
		return
	var links: Array = Cards.call("build_cross_refs", switch_node)
	if links.size() != 1:
		for l in links:
			print("DEBUG link kind=%s display=%s target=%s" % [l.get("kind"), l.get("display"), str(l.get("target"))])
		push_error("Expected 1 link, got %d" % links.size())
		quit(1)
		return
	var entry: Dictionary = links[0]
	if entry.get("kind") != "Target Door":
		push_error("Link kind should be 'Target Door', got %s" % str(entry.get("kind")))
		quit(1)
		return
	if not (entry.get("target") is Node):
		push_error("Target should be a Node, got %s" % str(entry.get("target")))
		quit(1)
		return
	if entry.get("target") != door:
		push_error("Target should be the Door, got %s" % str(entry.get("target")))
		quit(1)
		return
	if not str(entry.get("display")).contains("Door"):
		push_error("Display should mention Door, got %s" % str(entry.get("display")))
		quit(1)
		return
	print("[xref] + build_cross_refs target is a Node (not a string path)")
	print("[xref] + Display text: %s" % entry.get("display"))

	# Broken path -> target=null (disabled label in UI).
	switch_node.set("TargetDoorPath", NodePath("../Missing"))
	var broken: Array = Cards.call("build_cross_refs", switch_node)
	if broken.size() != 1 or broken[0].get("target") != null:
		push_error("Unresolved path should produce target=null, got %s" % str(broken))
		quit(1)
		return
	print("[xref] + Unresolved NodePath produces target=null (shown as disabled label)")

	# Empty path -> no link. set_indexed bypasses the typed-export null check.
	switch_node.set_indexed("TargetDoorPath", NodePath(""))
	var empty: Array = Cards.call("build_cross_refs", switch_node)
	if not empty.is_empty():
		for l in empty:
			print("DEBUG empty link kind=%s display=%s target=%s" % [l.get("kind"), l.get("display"), str(l.get("target"))])
		push_error("Empty TargetDoorPath should produce no link, got %d" % empty.size())
		quit(1)
		return
	print("[xref] + Empty TargetDoorPath produces no link entry")

	# Cutscene resource link: target is a string path, not a Node.
	var cutscene_node := Node2D.new()
	cutscene_node.name = "CutsceneTrigger"
	var cs_script := GDScript.new()
	cs_script.source_code = """
extends Node2D
@export var Cutscene: Resource = null
"""
	cs_script.reload()
	cutscene_node.set_script(cs_script)
	root.add_child(cutscene_node)
	await process_frame

	var fake_cutscene: Resource = load("res://resources/cutscene/cutscene_resource.gd").new()
	fake_cutscene.take_over_path("res://test/fake_cutscene.tres")
	# Assign via set_indexed so the typed @export accepts a Resource.
	cutscene_node.set_indexed("Cutscene", fake_cutscene)
	var cs_links: Array = Cards.call("build_cross_refs", cutscene_node)
	if cs_links.size() != 1:
		push_error("Cutscene link expected 1 entry, got %d" % cs_links.size())
		quit(1)
		return
	var cs_entry: Dictionary = cs_links[0]
	if cs_entry.get("kind") != "Cutscene":
		push_error("Cutscene link kind wrong: %s" % str(cs_entry.get("kind")))
		quit(1)
		return
	if cs_entry.get("target") != fake_cutscene.resource_path:
		push_error("Cutscene link target should be the resource path string, got %s" % str(cs_entry.get("target")))
		quit(1)
		return
	print("[xref] + Cutscene resource link target is a string path (not a Node)")

	print("[xref] ALL OK")
	quit(0)
