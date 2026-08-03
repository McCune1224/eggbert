class_name GameLogger
extends RefCounted

const LOG_LEVELS: Dictionary[String, int] = {"DEBUG": 0, "INFO": 1, "WARN": 2, "ERROR": 3, "OFF": 4}
const LOG_DIRECTORY := "user://logs"

static var level: int = LOG_LEVELS.INFO
static var echo_to_stdout: bool = true
static var _initialized := false

static func initialize_from_env() -> void:
	var configured := OS.get_environment("EGGBERT_LOG_LEVEL").to_upper()
	level = LOG_LEVELS.get(configured, LOG_LEVELS.INFO)
	echo_to_stdout = OS.get_environment("EGGBERT_LOG_ECHO") != "0"
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(LOG_DIRECTORY))
	_prune_logs()
	_initialized = true

static func debug(tag: String, message: String) -> void:
	_write("DEBUG", tag, message)

static func info(tag: String, message: String) -> void:
	_write("INFO", tag, message)

static func warn(tag: String, message: String) -> void:
	_write("WARN", tag, message)

static func error(tag: String, message: String) -> void:
	_write("ERROR", tag, message)

static func _write(kind: String, tag: String, message: String) -> void:
	if not _initialized:
		initialize_from_env()
	if LOG_LEVELS[kind] < level:
		return
	var stamp := Time.get_datetime_string_from_system()
	var line := "%s [%s] [%s] %s" % [stamp, kind, tag, message]
	if echo_to_stdout:
		print(line)
	var date := Time.get_datetime_dict_from_system()
	var filename := "%s/eggbert_%04d-%02d-%02d.log" % [LOG_DIRECTORY, date.year, date.month, date.day]
	var file := FileAccess.open(filename, FileAccess.READ_WRITE)
	if file == null:
		file = FileAccess.open(filename, FileAccess.WRITE)
	if file != null:
		file.seek_end()
		file.store_line(line)

static func _prune_logs() -> void:
	var directory := DirAccess.open(LOG_DIRECTORY)
	if directory == null:
		return
	var files: Array[String] = []
	for filename in directory.get_files():
		if filename.begins_with("eggbert_") and filename.ends_with(".log"):
			files.append(filename)
	files.sort()
	while files.size() > 5:
		directory.remove(files.pop_front())
