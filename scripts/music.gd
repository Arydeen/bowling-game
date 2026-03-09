extends Node

@onready var player: AudioStreamPlayer = AudioStreamPlayer.new()

var bowling_stream: AudioStream = preload("res://music/bowling track.mp3")

func _ready() -> void:
	add_child(player)
	player.bus = "Music" # optional
	player.stream = bowling_stream
	player.autoplay = false
	player.process_mode = Node.PROCESS_MODE_ALWAYS

func play_bowling() -> void:
	if player.stream != bowling_stream:
		player.stream = bowling_stream
	if not player.playing:
		player.play()

func stop_music() -> void:
	if player.playing:
		player.stop()
