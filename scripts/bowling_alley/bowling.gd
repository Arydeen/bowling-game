extends Node2D

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	# Music.play_bowling()
	# $AmbiencePlayer.play()
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass


func _unhandled_input(event: InputEvent) -> void:
	if Input.is_action_just_pressed("go_shop"):
		get_tree().change_scene_to_file("res://scenes/shop.tscn")
