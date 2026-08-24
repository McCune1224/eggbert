@tool
extends RefCounted

## Shared level-scaffolding helper used by the Level Wizard plugin and the
## Level Assembly dock's "New Level" button. Builds a fresh BaseLevel scene
## from res://levels/BaseLevel.tscn, adds two TileMapLayers, sets the
## music/ambience exports, saves it under res://levels/<slug>/maps/, then
## opens it in the editor.

const BASE_LEVEL_TEMPLATE := "res://levels/BaseLevel.tscn"


## Creates a new level scene. Returns the saved .tscn path, or "" on failure.
static func create_level(level_name: String, tileset_path: String, music_path: String, ambience_path: String) -> String:
	if level_name.strip_edges() == "":
		printerr("[LevelFactory] empty level name")
		return ""

	var slug := _slugify(level_name)
	var dir := "res://levels/%s/maps" % slug
	var abs_dir := ProjectSettings.globalize_path(dir)
	DirAccess.make_dir_recursive_absolute(abs_dir)

	var template := load(BASE_LEVEL_TEMPLATE) as PackedScene
	if template == null:
		printerr("[LevelFactory] could not load template %s" % BASE_LEVEL_TEMPLATE)
		return ""
	var root := template.instantiate()
	if root == null:
		return ""

	root.name = level_name
	root.set("LevelName", level_name)
	if music_path != "":
		root.set("LevelMusic", load(music_path))
	if ambience_path != "":
		root.set("LevelAmbience", load(ambience_path))

	var ts: Resource = load(tileset_path) if tileset_path != "" else null
	_add_tilemap_layer(root, "ArchitectureTilemap", ts)
	_add_tilemap_layer(root, "ForegroundTilemap", ts)

	var path := "%s/%s.tscn" % [dir, level_name]
	var packed := PackedScene.new()
	var err := packed.pack(root)
	if err != OK:
		printerr("[LevelFactory] pack failed: %d" % err)
		root.queue_free()
		return ""
	err = ResourceSaver.save(packed, path)
	if err != OK:
		printerr("[LevelFactory] save failed: %d" % err)
		root.queue_free()
		return ""

	root.queue_free()
	var ei = Engine.get_singleton("Editor Interface")
	ei.get_resource_filesystem().scan()
	ei.open_scene_from_disk(path)
	return path


static func _add_tilemap_layer(parent: Node, layer_name: String, tileset: Resource) -> void:
	var layer := TileMapLayer.new()
	layer.name = layer_name
	if tileset != null:
		layer.tile_set = tileset
	parent.add_child(layer)


static func _slugify(s: String) -> String:
	var out := s.strip_edges().replace(" ", "_").replace("-", "_")
	var cleaned := ""
	for c in out:
		if c == "_" or (c >= "a" and c <= "z") or (c >= "A" and c <= "Z") or (c >= "0" and c <= "9"):
			cleaned += c
	return cleaned.to_lower()
