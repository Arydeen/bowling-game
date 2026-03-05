extends Area2D

signal clicked
signal shutter_opened

@onready var shutter: AnimatedSprite2D = $ArcadeShutterAnimation

var is_open: bool = false
var is_opening: bool = false

var open_anim_name: StringName = &"ArcadeShutterAnimation"

func _ready() -> void:
	input_pickable = true

	# hide it at start so flicker is visible
	shutter.visible = false

	# reset to first frame 
	shutter.stop()
	shutter.frame = 0

func _input_event(_viewport: Viewport, event: InputEvent, _shape_idx: int) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		clicked.emit()

func open_shutter() -> void:
	if is_open or is_opening:
		return

	is_opening = true

	# show only when starting the animation
	shutter.visible = true
	shutter.play(open_anim_name) # make sure this animation is NOT looping
	shutter.animation_finished.connect(_on_shutter_finished, CONNECT_ONE_SHOT)

func _on_shutter_finished() -> void:
	is_opening = false
	is_open = true

	# freeze on last frame and keep visible
	shutter.stop()
	var last := shutter.sprite_frames.get_frame_count(open_anim_name) - 1
	if last >= 0:
		shutter.frame = last

	shutter_opened.emit()
