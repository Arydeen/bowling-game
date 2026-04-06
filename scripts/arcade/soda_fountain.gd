extends Area2D
signal clicked

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	input_pickable = true


func _input_event(viewport: Viewport, event: InputEvent, shape_idx: int) -> void:
	if event is InputEventMouseButton \
	and event.button_index == MOUSE_BUTTON_LEFT \
	and event.pressed:
		clicked.emit()
		viewport.set_input_as_handled()
