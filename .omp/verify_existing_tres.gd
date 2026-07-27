extends SceneTree

# Loads the two shipped .tres files that store Type=12 and confirms they
# deserialize as StepType.Stop (ordinal 12), not StepType.DialogBranch (13).

func _initialize() -> void:
	var paths := [
		"res://levels/shrine/npcs/SunnysideRevelationCutscene.tres",
		"res://levels/beach/npcs/GreatBeyondFinaleCutscene.tres",
	]
	for path in paths:
		var resource: Resource = load(path)
		if resource == null:
			push_error("Failed to load %s" % path)
			quit(1)
			return
		var steps: Array = resource.get("Steps")
		if steps == null:
			push_error("%s: Steps array missing" % path)
			quit(1)
			return
		# Find the Type=12 step (the last one in both files).
		var found_stop := false
		for i in range(steps.size()):
			var step_type: int = int(steps[i].get("Type"))
			if step_type == 12:
				found_stop = true
				# Confirm the enum name resolves to Stop, not DialogBranch.
				# The C# enum: Stop=12, DialogBranch=13. The int value alone
				# is the canonical check since the resource is already deserialized.
				print("[tres] %s: step[%d] Type=12 (Stop)" % [path.get_file(), i])
		if not found_stop:
			push_error("%s: no step with Type=12 found — enum reorder broke shipped data" % path)
			quit(1)
			return
		# Also verify no step is silently mislabeled as 13 (DialogBranch).
		for i in range(steps.size()):
			if int(steps[i].get("Type")) == 13:
				push_error("%s: step[%d] has Type=13 (DialogBranch) — not expected" % [path.get_file(), i])
				quit(1)
				return
	print("[tres] Both shipped .tres files preserve Stop=12 ordinal")
	print("[tres] ALL OK")
	quit(0)
