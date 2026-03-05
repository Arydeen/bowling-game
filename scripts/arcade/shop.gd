extends Node2D

@onready var capsule_machine: Area2D = $CapsuleMachine
@onready var shop_center: Marker2D = $ShopCenter
@onready var coin_exchange_machine: Area2D = $PinExchange

const CAPSULE_COMMON_TEX: Texture2D = preload("res://textures/capsules/CapsuleCommon.png")
const CAPSULE_RARE_TEX: Texture2D = preload("res://textures/capsules/CapsuleRare.png")
const CAPSULE_EPIC_TEX: Texture2D = preload("res://textures/capsules/CapsuleEpic.png")
const CAPSULE_LEGENDARY_TEX: Texture2D = preload("res://textures/capsules/CapsuleLegendary.png")

@export var spawn_offset: Vector2 = Vector2(0, -120)
@export var drop_time: float = 0.6

@export var rock_angle: float = 0.25
@export var rock_time: float = 0.12

#FLICKERING SHOP 
@onready var background: Sprite2D = $Arcade

const BG_ON: Texture2D = preload("res://textures/backgrounds/ArcadeClosedBackground.png")
const BG_OFF: Texture2D = preload("res://textures/backgrounds/ArcadeClosedOffBackground.png")

@export var background_swap_time: float = .75

var _background_timer: Timer
var _background_is_on: bool = true

# spam tuning SATISFYING
@export var rock_speed_boost_per_press: float = 0.35
@export var rock_speed_max: float = 4.0

#Lights
@export var open_flash_time: float = 0.25
@export var open_flash_peak_energy: float = 2 #Intensity (Start)
@export var open_flash_peak_scale: float = 2

@export var open_light_sustain_energy: float = 5   # stays ON after open (End)
@export var open_light_sustain_scale: float = 5

@export var open_light_spike_count: int = 8         # number of rays
@export var open_light_spike_sharpness: float = 4.0  # higher = thinner spikes
@export var open_light_falloff: float = 1.8          # higher = fades faster outward
@export var open_light_texture_size: int = 256       # 128/256/512

var _open_light: PointLight2D = null
var _open_light_tween: Tween = null

#Currency
@export var capsule_spin_cost_coins: int = 1

enum Rarity { COMMON, RARE, EPIC, LEGENDARY }
var _current_rarity: Rarity = Rarity.COMMON

enum Step { NONE, WAIT_LEFT, WAIT_RIGHT, WAIT_OPEN, WAIT_REMOVE }
var _step: Step = Step.NONE

var _busy: bool = false
var _animating: bool = false

var _capsule: Sprite2D = null
var _tex_size: Vector2 = Vector2.ZERO

var _rock_tween: Tween = null
var _rock_mult: float = 1.0
var _rock_target_angle: float = 0.0

func _ready() -> void:
	randomize()
	_setup_open_light()
	capsule_machine.clicked.connect(_on_capsule_machine_clicked)
	coin_exchange_machine.clicked.connect(_on_pin_exchange_clicked)

	_setup_background_blink()

func _unhandled_input(event: InputEvent) -> void:
	if _busy and _capsule != null and Input.is_action_just_pressed("capsule_open"):
		_on_space_pressed()
		get_viewport().set_input_as_handled()

func _on_pin_exchange_clicked() -> void:
	if _busy:
		return

	if not CurrencyManager.convert_pins_to_coin(5, 1):
		print("Not enough pins!")
		return

	print("Exchanged 5 pins for 1 coin")

func _on_capsule_machine_clicked() -> void:
	if _busy:
		return

	# Pay 1 coin per spin BEFORE starting
	if not CurrencyManager.spend_coins(capsule_spin_cost_coins):
		print("Not enough coins!")
		return

	_busy = true
	_step = Step.NONE
	_stop_open_light()
	_spawn_and_drop_capsule()

func _pick_capsule_texture() -> Texture2D:
	# 56% common, 28% rare, 11% epic, 5% legendary
	var roll := randi_range(1, 100)

	if roll <= 56:
		_current_rarity = Rarity.COMMON
		return CAPSULE_COMMON_TEX
	elif roll <= 84: # 57-84 (28%)
		_current_rarity = Rarity.RARE
		return CAPSULE_RARE_TEX
	elif roll <= 95: # 85-95 (11%)
		_current_rarity = Rarity.EPIC
		return CAPSULE_EPIC_TEX
	else:            # 96-100 (5%)
		_current_rarity = Rarity.LEGENDARY
		return CAPSULE_LEGENDARY_TEX

func _spawn_and_drop_capsule() -> void:
	_capsule = Sprite2D.new()
	_capsule.texture = _pick_capsule_texture()
	_capsule.centered = true
	_capsule.z_index = 100
	_capsule.rotation = 0.0

	_tex_size = _capsule.texture.get_size()

	# Start CLOSED CAPSULE``
	_capsule.region_enabled = true
	_capsule.region_rect = Rect2(0.0, 0.0, _tex_size.x, _tex_size.y * 0.5)

	var end_pos: Vector2 = shop_center.global_position
	var start_pos: Vector2 = end_pos + spawn_offset

	_capsule.global_position = start_pos
	add_child(_capsule)

	var tween: Tween = create_tween()
	tween.tween_property(_capsule, "global_position", end_pos, drop_time)

	await tween.finished
	_step = Step.WAIT_LEFT

func _on_space_pressed() -> void:
	if not _busy:
		return
	if _capsule == null:
		return

	# If we're currently rocking, spamming increases speed (DON'T kill/restart tween).
	if _animating:
		_rock_mult = min(_rock_mult + rock_speed_boost_per_press, rock_speed_max)
		if _rock_tween != null and _rock_tween.is_valid():
			_rock_tween.set_speed_scale(_rock_mult)
		return

	match _step:
		Step.WAIT_LEFT:
			_animating = true
			await _rock(-rock_angle)
			_animating = false
			_step = Step.WAIT_RIGHT

		Step.WAIT_RIGHT:
			_animating = true
			await _rock(rock_angle)
			_animating = false
			_step = Step.WAIT_OPEN

		Step.WAIT_OPEN:
			# OPEN CAPSULE
			_capsule.region_rect = Rect2(0.0, _tex_size.y * 0.5, _tex_size.x, _tex_size.y * 0.5)
			if _open_light != null:
				_open_light.color = _get_light_color_for_rarity(_current_rarity)
			_flash_open_light()
			_step = Step.WAIT_REMOVE

		Step.WAIT_REMOVE:
			_stop_open_light()
			
			_capsule.queue_free()
			_capsule = null
			_busy = false
			_step = Step.NONE

		_:
			pass

func _rock(target_angle: float) -> void:
	_capsule.rotation = 0.0
	_rock_mult = 1.0
	_rock_target_angle = target_angle

	_rock_tween = create_tween()
	_rock_tween.set_speed_scale(_rock_mult)
	_rock_tween.tween_property(_capsule, "rotation", _rock_target_angle, rock_time)
	_rock_tween.tween_property(_capsule, "rotation", 0.0, rock_time)

	var tween := _rock_tween
	await tween.finished

	if _rock_tween == tween:
		_rock_tween = null

func _make_spiky_light_texture(size: int = 256) -> Texture2D:
	# Procedural "rays" texture: bright spikes from center, fading outward.
	var img: Image = Image.create(size, size, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))

	var center: Vector2 = Vector2(size * 0.5, size * 0.5)
	var max_r: float = center.x

	for y: int in range(size):
		for x: int in range(size):
			var p: Vector2 = Vector2(x + 0.5, y + 0.5) - center
			var r: float = p.length() / max_r
			if r >= 1.0:
				continue

			var ang: float = atan2(p.y, p.x) # -PI..PI
			var s: float = abs(sin(ang * float(open_light_spike_count) * 0.5))
			s = pow(s, open_light_spike_sharpness)

			var radial: float = pow(1.0 - r, open_light_falloff)
			var a: float = clamp(radial * (0.35 + 0.65 * s), 0.0, 1.0)

			img.set_pixel(x, y, Color(1, 1, 1, a))

	var tex: ImageTexture = ImageTexture.create_from_image(img)
	return tex


func _setup_open_light() -> void:
	_open_light = PointLight2D.new()
	_open_light.texture = _make_spiky_light_texture(open_light_texture_size)
	_open_light.energy = 0.0
	_open_light.scale = Vector2.ONE
	_open_light.color = Color(1, 1, 1, 1)
	_open_light.shadow_enabled = true
	_open_light.z_index = 200

	# Origin at shop_center (center)
	shop_center.add_child(_open_light)
	_open_light.position = Vector2.ZERO


func _flash_open_light() -> void:
	if _open_light == null:
		return

	if _open_light_tween != null and _open_light_tween.is_valid():
		_open_light_tween.kill()

	_open_light.energy = 0.0
	_open_light.scale = Vector2.ONE

	_open_light_tween = create_tween()
	_open_light_tween.tween_property(_open_light, "energy", open_flash_peak_energy, open_flash_time * 0.35)
	_open_light_tween.parallel().tween_property(_open_light, "scale", Vector2.ONE * open_flash_peak_scale, open_flash_time * 0.35)

	# settle and STAY (no fade to 0)
	_open_light_tween.tween_property(_open_light, "energy", open_light_sustain_energy, open_flash_time * 0.65)
	_open_light_tween.parallel().tween_property(_open_light, "scale", Vector2.ONE * open_light_sustain_scale, open_flash_time * 0.65)


func _stop_open_light() -> void:
	if _open_light_tween != null and _open_light_tween.is_valid():
		_open_light_tween.kill()
	_open_light_tween = null

	if _open_light != null:
		_open_light.energy = 0.0
		_open_light.scale = Vector2.ONE


func _get_light_color_for_rarity(r: Rarity) -> Color:
	match r:
		Rarity.COMMON:
			return Color8(120, 255, 80)   # green
		Rarity.RARE:
			return Color8(160, 220, 255)  # blue
		Rarity.EPIC:
			return Color8(155, 90, 255)   # purple
		Rarity.LEGENDARY:
			return Color8(255, 215, 80)   # yellow
		_:
			return Color.WHITE

func _setup_background_blink() -> void:
	if background == null:
		push_error("Background node not found. Check the node path in shop.gd.")
		return

	background.texture = BG_OFF
	_background_is_on = false

	_background_timer = Timer.new()
	_background_timer.wait_time = background_swap_time
	_background_timer.one_shot = false
	_background_timer.timeout.connect(_toggle_background)
	add_child(_background_timer)
	_background_timer.start()


func _toggle_background() -> void:
	_background_is_on = not _background_is_on

	if _background_is_on:
		background.texture = BG_ON
	else:
		background.texture = BG_OFF
