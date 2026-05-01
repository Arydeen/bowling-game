extends CanvasLayer

const ARCADE_TEX: Texture2D = preload("res://textures/backgrounds/ArcadeClosedOffBackground.png")

@export var stone_push_sfx: AudioStream
@export var stone_push_volume_db: float = -12.0

@onready var black_rect: ColorRect = $BlackRect
@onready var arcade_face: TextureRect = $ArcadeFace
@onready var old_screen: TextureRect = $OldScreen

var _busy := false
var _stone_push_player: AudioStreamPlayer


func _ready() -> void:
	layer = 999

	_stone_push_player = AudioStreamPlayer.new()
	add_child(_stone_push_player)
	_stone_push_player.stream = stone_push_sfx
	_stone_push_player.volume_db = stone_push_volume_db

	_setup_control(black_rect)
	_setup_control(arcade_face)
	_setup_control(old_screen)

	black_rect.visible = false
	black_rect.color = Color.BLACK
	black_rect.z_index = 0

	arcade_face.visible = false
	arcade_face.texture = ARCADE_TEX
	arcade_face.z_index = 1
	arcade_face.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	arcade_face.stretch_mode = TextureRect.STRETCH_SCALE

	old_screen.visible = false
	old_screen.z_index = 2
	old_screen.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	old_screen.stretch_mode = TextureRect.STRETCH_SCALE


func _setup_control(control: Control) -> void:
	control.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Prevent Full Rect anchors from fighting position/size tweens.
	control.anchor_left = 0.0
	control.anchor_top = 0.0
	control.anchor_right = 0.0
	control.anchor_bottom = 0.0

	control.offset_left = 0.0
	control.offset_top = 0.0
	control.offset_right = 0.0
	control.offset_bottom = 0.0


func _play_stone_push() -> void:
	if _stone_push_player == null:
		return

	if stone_push_sfx != null:
		_stone_push_player.stream = stone_push_sfx

	if _stone_push_player.stream == null:
		return

	_stone_push_player.volume_db = stone_push_volume_db
	_stone_push_player.stop()
	_stone_push_player.play()


func _stop_stone_push() -> void:
	if _stone_push_player != null and _stone_push_player.playing:
		_stone_push_player.stop()


func change_scene_cube(scene_path: String, duration: float = 3.0, hold_time: float = 0.35) -> void:
	if _busy:
		return

	_busy = true

	var viewport := get_viewport()
	var screen_size := viewport.get_visible_rect().size

	# Screenshot current scene.
	var img := viewport.get_texture().get_image()
	var tex := ImageTexture.create_from_image(img)

	# Background.
	black_rect.visible = true
	black_rect.position = Vector2.ZERO
	black_rect.size = screen_size
	black_rect.modulate.a = 1.0

	# OLD LANES FACE:
	old_screen.texture = tex
	old_screen.visible = true
	old_screen.position = Vector2.ZERO
	old_screen.size = screen_size
	old_screen.scale = Vector2.ONE
	old_screen.rotation = 0.0
	old_screen.modulate.a = 1.0

	# ARCADE FACE:
	arcade_face.texture = ARCADE_TEX
	arcade_face.visible = true
	arcade_face.position = Vector2(screen_size.x, 0)
	arcade_face.size = Vector2(0, screen_size.y)
	arcade_face.scale = Vector2.ONE
	arcade_face.rotation = 0.0
	arcade_face.modulate.a = 1.0

	await get_tree().create_timer(hold_time).timeout

	_play_stone_push()

	var tween := create_tween()
	tween.set_parallel(true)

	# Old screen moves left and collapses.
	tween.tween_property(old_screen, "position:x", -screen_size.x * 0.18, duration)\
		.set_trans(Tween.TRANS_SINE)\
		.set_ease(Tween.EASE_IN_OUT)

	tween.tween_property(old_screen, "size:x", screen_size.x * 0.18, duration)\
		.set_trans(Tween.TRANS_SINE)\
		.set_ease(Tween.EASE_IN_OUT)

	# Arcade face moves in from the right and grows.
	tween.tween_property(arcade_face, "position:x", 0.0, duration)\
		.set_trans(Tween.TRANS_SINE)\
		.set_ease(Tween.EASE_IN_OUT)

	tween.tween_property(arcade_face, "size:x", screen_size.x, duration)\
		.set_trans(Tween.TRANS_SINE)\
		.set_ease(Tween.EASE_IN_OUT)

	await tween.finished

	# Arcade face is now covering the screen.
	old_screen.visible = false
	old_screen.texture = null

	get_tree().change_scene_to_file(scene_path)

	await get_tree().process_frame
	await get_tree().process_frame

	# Shop/arcade scene is now loaded, so stop the push sound.
	_stop_stone_push()

	# No fade. Remove transition overlay.
	arcade_face.visible = false
	arcade_face.position = Vector2.ZERO
	arcade_face.size = screen_size
	arcade_face.modulate.a = 1.0

	black_rect.visible = false

	_busy = false
