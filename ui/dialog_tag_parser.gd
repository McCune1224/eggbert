class_name DialogTagParser
extends RefCounted

static func parse(input: String) -> Array[Dictionary]:
	var segments: Array[Dictionary] = []
	var text := ""
	var pending_pause: float = 0.0
	var pending_cps: float = 0.0
	var index := 0
	while index < input.length():
		if input[index] != "[":
			text += input[index]
			index += 1
			continue
		var close := input.find("]", index)
		if close < 0:
			text += input[index]
			index += 1
			continue
		var tag := input.substr(index + 1, close - index - 1)
		var equals := tag.find("=")
		if equals < 0:
			text += input.substr(index, close - index + 1)
			index = close + 1
			continue
		if not text.is_empty():
			segments.append({"text": text, "pause_before": pending_pause, "cps": pending_cps})
			text = ""
			pending_pause = 0.0
			pending_cps = 0.0
		var tag_name := tag.substr(0, equals).strip_edges()
		var tag_value := tag.substr(equals + 1).strip_edges().to_lower()
		if tag_name == "pause":
			pending_pause = tag_value.to_float()
		elif tag_name == "speed":
			match tag_value:
				"slow": pending_cps = 20.0
				"fast": pending_cps = 80.0
				"instant": pending_cps = -1.0
				_: pending_cps = 0.0
		else:
			text += input.substr(index, close - index + 1)
		index = close + 1
	if not text.is_empty():
		segments.append({"text": text, "pause_before": pending_pause, "cps": pending_cps})
	return segments
