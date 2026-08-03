class_name DialogBranch
extends Resource

@export var nodes: Array[DialogNode] = []

func get_node_by_id(node_id: String) -> DialogNode:
	for node in nodes:
		if node != null and node.id == node_id:
			return node
	return null
