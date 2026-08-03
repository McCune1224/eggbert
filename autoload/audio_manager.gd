extends Node

var _music_player: AudioStreamPlayer
var _sfx_players: Array[AudioStreamPlayer] = []

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_music_player = AudioStreamPlayer.new()
	_music_player.name = "MusicPlayer"
	add_child(_music_player)
	for index in 4:
		var player := AudioStreamPlayer.new()
		player.name = "SfxPlayer%d" % index
		add_child(player)
		_sfx_players.append(player)

func play_music(stream: AudioStream, fade_seconds: float = 0.0) -> void:
	if _music_player == null:
		return
	if fade_seconds <= 0.0:
		_music_player.stream = stream
		_music_player.play()
		return
	var tween := create_tween()
	tween.tween_property(_music_player, "volume_db", -40.0, fade_seconds)
	tween.tween_callback(func() -> void:
		_music_player.stream = stream
		_music_player.volume_db = 0.0
		_music_player.play()
	)

func stop_music(fade_seconds: float = 0.0) -> void:
	if _music_player == null:
		return
	if fade_seconds <= 0.0:
		_music_player.stop()
		return
	var tween := create_tween()
	tween.tween_property(_music_player, "volume_db", -40.0, fade_seconds)
	tween.tween_callback(_music_player.stop)

func play_sfx(stream: AudioStream, volume_db: float = 0.0) -> void:
	if stream == null:
		return
	for player in _sfx_players:
		if not player.playing:
			player.stream = stream
			player.volume_db = volume_db
			player.play()
			return
	var player := _sfx_players[0] if not _sfx_players.is_empty() else null
	if player != null:
		player.stream = stream
		player.volume_db = volume_db
		player.play()
