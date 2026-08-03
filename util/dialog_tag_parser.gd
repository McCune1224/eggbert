class_name DialogTagParser
extends RefCounted

static func parse(line: String) -> Dictionary[String, Variant]:
	var result: Dictionary[String, Variant] = {"text": line, "speaker": "", "tags": {}}
	var regex := RegEx.new()
	regex.compile("^\\[([^\\]]+)\\]\\s*(.*)$")
	var match := regex.search(line)
	if match == null:
		return result
	result.text = match.get_string(2)
	for tag in match.get_string(1).split(" "):
		var parts := tag.split("=", true, 1)
		if parts.size() == 2:
			result.tags[parts[0]] = parts[1]
		else:
			result.tags[tag] = true
	result.speaker = str(result.tags.get("speaker", ""))
	return result
