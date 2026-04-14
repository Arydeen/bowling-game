extends Node

@export var player_menu_scene: PackedScene = preload("res://scenes/player/player_menu.tscn") 

var _menu: CanvasLayer = null
var _is_open := false
var _toggling := false

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS  # still receives input if you ever pause later

func _unhandled_input(event: InputEvent) -> void:
	if not event.is_action_pressed("toggle_player_menu"):
		return

	# prevent double toggles in the same frame/event chain
	if _toggling:
		return

	_toggling = true
	get_viewport().set_input_as_handled()
	call_deferred("_toggle_menu")

func _toggle_menu() -> void:
	_toggling = false

	if _menu == null or not is_instance_valid(_menu):
		_menu = player_menu_scene.instantiate() as CanvasLayer
		get_tree().root.add_child(_menu)
		_menu.visible = false

	_is_open = not _is_open
	_menu.visible = _is_open

	# get_tree().paused = _is_open
