extends Node

@onready var player: AudioStreamPlayer = AudioStreamPlayer.new()

var bowling_stream: AudioStream = preload("res://music/bowling track.mp3")
var arcade_stream: AudioStream = preload("res://music/ArcadeMusic.mp3")

@export var fade_time: float = 0.8

const SILENT_DB := -80.0
const FULL_DB := 0.0

var _fade_tween: Tween
var _target_stream: AudioStream = null

func _ready() -> void:
	add_child(player)
	player.bus = "Music" # optional
	player.autoplay = false
	player.process_mode = Node.PROCESS_MODE_ALWAYS
	player.volume_db = FULL_DB

func play_bowling() -> void:
	_fade_to(bowling_stream)

func play_arcade() -> void:
	_fade_to(arcade_stream, true)

func stop_music() -> void:
	_kill_fade()
	if player.playing:
		player.stop()

func _fade_to(stream: AudioStream, disable_loop: bool = false) -> void:
	if stream == null:
		return
		
	if player.playing and player.stream == stream:
		return

	if _target_stream == stream:
		return

	_target_stream = stream

	_kill_fade()

	# If nothing is playing yet, just start immediately
	if not player.playing or player.stream == null:
		player.stream = stream
		_apply_loop_override(stream, disable_loop)
		player.volume_db = FULL_DB
		player.play()
		_target_stream = null
		return

	# Fade out -> swap -> fade in
	_fade_tween = create_tween()
	_fade_tween.tween_property(player, "volume_db", SILENT_DB, fade_time)
	_fade_tween.tween_callback(func():
		player.stop()
		player.stream = stream
		_apply_loop_override(stream, disable_loop)
		player.play()
	)
	_fade_tween.tween_property(player, "volume_db", FULL_DB, fade_time)
	_fade_tween.tween_callback(func():
		_target_stream = null
	)

func _kill_fade() -> void:
	if _fade_tween != null and _fade_tween.is_valid():
		_fade_tween.kill()
	_fade_tween = null
	_target_stream = null

func _apply_loop_override(stream: AudioStream, disable_loop: bool) -> void:
	if not disable_loop:
		return
	if stream != null and stream.has_method("set_loop"):
		stream.call("set_loop", false)
	elif stream != null and "loop" in stream:
		stream.set("loop", false)
