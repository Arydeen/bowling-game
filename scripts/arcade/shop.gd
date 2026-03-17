extends Node2D

@onready var capsule_machine: Area2D = $CapsuleMachine
@onready var shop_center: Marker2D = $ShopCenter
@onready var coin_exchange_machine := $PinExchange
@onready var currency_ui := $CurrencyUI
@onready var capsule_open_sfx: AudioStreamPlayer2D = $CapsuleOpen
@onready var shutter_open_sfx: AudioStreamPlayer2D = $ShutterOpen
@onready var capsule_shake_sfx: AudioStreamPlayer2D = $CapsuleShake
@onready var drink_one_shot_sfx: AudioStreamPlayer2D = $BeerBottleOpen

@onready var soda_fountain: Area2D = $SodaFountain
@onready var soda_menu: PopupMenu = $SodaFountainMenu
@onready var pour_noise = $PourNoise
@onready var sip_drink: AudioStreamPlayer2D = $DrinkSip

#FLICKERING SHOP 
@onready var background: Sprite2D = $Arcade

const CAPSULE_COMMON_TEX: Texture2D = preload("res://textures/capsules/CapsuleCommon.png")
const CAPSULE_RARE_TEX: Texture2D = preload("res://textures/capsules/CapsuleRare.png")
const CAPSULE_EPIC_TEX: Texture2D = preload("res://textures/capsules/CapsuleEpic.png")
const CAPSULE_LEGENDARY_TEX: Texture2D = preload("res://textures/capsules/CapsuleLegendary.png")
const MILK_SHEET = preload("res://textures/drinks/CupMilk.png")

@export var drinks: Array[DrinkData] = []

var _active_drink: DrinkData = null
var _drink_close_armed = false

@export var milk_sheet_cols = 6
@export var milk_sheet_rows = 7
@export var milk_frame_count = 40
@export var milk_anim_fps = 30

var _milk_waiting_to_close = false
var _milk_sprite = null              
var _milk_waiting_for_space = false 

var _space_was_down = false

@export var spawn_offset: Vector2 = Vector2(0, -120)
@export var drop_time: float = 0.6

@export var rock_angle: float = 0.25
@export var rock_time: float = 0.12

const BG_ON: Texture2D = preload("res://textures/backgrounds/ArcadeClosedBackground.png")
const BG_OFF: Texture2D = preload("res://textures/backgrounds/ArcadeClosedOffBackground.png")

@export var background_swap_time: float = .75

var _pin_shutter_opening: bool = false

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

var _drink_by_id: Dictionary = {}

#Currency
@export var capsule_spin_cost_tokens: int = 1

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
	Music.play_arcade()
	randomize()
	_setup_open_light()
	capsule_machine.clicked.connect(_on_capsule_machine_clicked)

	# pin exchange + shutter
	coin_exchange_machine.clicked.connect(_on_pin_exchange_clicked)
	coin_exchange_machine.shutter_opened.connect(_on_pin_exchange_shutter_opened)

	soda_fountain.clicked.connect(_on_soda_fountain_clicked)
	soda_menu.id_pressed.connect(_on_soda_menu_id_pressed)

	soda_menu.close_requested.connect(soda_menu.hide)

	_setup_background_blink()


func _handle_space_press() -> void:
	# Second press closes
	if _milk_waiting_to_close:
		_close_current_drink()
		return

	# First press starts animation
	if _milk_waiting_for_space:
		_start_milk_open()
		return


func _input(event: InputEvent) -> void:
	if not (event is InputEventKey):
		return

	if (not event.pressed) and event.is_action_released("capsule_open"):
		_drink_close_armed = true
		return

	if not event.pressed:
		return

	if event.echo:
		return

	if not event.is_action_pressed("capsule_open"):
		return

	#print("SPACE press. wait_space=", _milk_waiting_for_space, " wait_close=", _milk_waiting_to_close, " armed=", _drink_close_armed)

	if _milk_waiting_to_close:
		if not _drink_close_armed:
			# Ignore the press until the player releases Space at least once
			return
		_close_current_drink()
		get_viewport().set_input_as_handled()
		return

	if _milk_waiting_for_space:
		_start_milk_open()
		get_viewport().set_input_as_handled()
		return


func _unhandled_input(event: InputEvent) -> void:
	if (_milk_waiting_for_space or _milk_waiting_to_close) and event.is_action_pressed("capsule_open"):
		get_viewport().set_input_as_handled()
		return

	if _busy and _capsule != null and event is InputEventKey and event.pressed and not event.echo and event.is_action_pressed("capsule_open"):
		_on_space_pressed()
		get_viewport().set_input_as_handled()

func _on_pin_exchange_clicked() -> void:
	# First click: open shutter ONLY
	if not coin_exchange_machine.is_open:
		if _pin_shutter_opening:
			return

		_pin_shutter_opening = true

		if shutter_open_sfx != null and not shutter_open_sfx.playing:
			shutter_open_sfx.play()

		coin_exchange_machine.open_shutter()
		return

	# After it's open: trade 5 pins -> 1 coin
	if not CurrencyManager.convert_pins_to_coin(5, 1):
		currency_ui.show_random_not_enough_pins_toast()
		print("Not enough pins!")
		return

	if currency_ui != null and currency_ui.has_method("show_random_exchange_toast"):
		currency_ui.show_random_exchange_toast()	

	print("Exchanged 5 pins -> 1 coin")

func _on_pin_exchange_shutter_opened() -> void:
	_stop_background_blink(true) # true = freeze to BG_ON

func _stop_background_blink(freeze_to_on: bool) -> void:
	if _background_timer != null and _background_timer.is_inside_tree():
		_background_timer.stop()
		_background_timer.queue_free()

	_background_timer = null
	_background_is_on = freeze_to_on
	background.texture = BG_ON if freeze_to_on else BG_OFF

func _on_capsule_machine_clicked() -> void:
	if _busy:
		return

	# Pay 1 coin per spin BEFORE starting
	if not CurrencyManager.spend_tokens(capsule_spin_cost_tokens):
		print("Not enough tokens!")
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

			if capsule_open_sfx != null:
				if capsule_open_sfx.playing:
					capsule_open_sfx.stop()
				capsule_open_sfx.play()

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
	_play_capsule_shake_sfx()

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


func _play_capsule_shake_sfx() -> void:
	if capsule_shake_sfx == null:
		return
	
	capsule_shake_sfx.stop()
	capsule_shake_sfx.play()

func _on_soda_fountain_clicked() -> void:
	soda_menu.clear()
	_drink_by_id.clear()

	var popup_size = Vector2i(200, 100) # width/height of the popup
	soda_menu.max_size = Vector2i(0, popup_size.y) # scrollbar 

	#ITEMS
	for i in range(drinks.size()):
		var d = drinks[i]
		var label = "%s - %d" % [d.display_name, d.cost_tokens]
		soda_menu.add_item(label, i)
		_drink_by_id[i] = d

	#CENTER
	var x = 60
	var vp_rect = get_viewport().get_visible_rect()
	var y = int((vp_rect.size.y - popup_size.y) * 0.5)

	# FORCE SIZE (before + after showing)
	soda_menu.min_size = popup_size
	soda_menu.size = popup_size

	soda_menu.popup(Rect2i(Vector2i(x, y), popup_size))
	soda_menu.call_deferred("set_size", popup_size)


func _on_soda_menu_id_pressed(id: int) -> void:
	var d = _drink_by_id.get(id)
	if d == null:
		return

	if not CurrencyManager.spend_tokens(d.cost_tokens):
		print("Not enough tokens to buy %s!" % d.display_name)
		return

	print("You bought %s for %d token%s!" % [d.display_name, d.cost_tokens, "" if d.cost_tokens == 1 else "s"])

	soda_menu.hide()
	_spawn_and_drop_drink(d)


func _make_atlas_frame(sheet: Texture2D, idx: int, cols: int, cell_w: int, cell_h: int) -> AtlasTexture:
	var col = idx % cols
	var row = int(idx / cols)

	var at = AtlasTexture.new()
	at.atlas = sheet
	at.region = Rect2(col * cell_w, row * cell_h, cell_w, cell_h)
	return at


func _build_drink_frames(drink: DrinkData) -> SpriteFrames:
	var frames = SpriteFrames.new()

	if drink == null or drink.sprite_sheet == null:
		push_error("DrinkData missing sprite_sheet.")
		return frames

	var tex_size = drink.sprite_sheet.get_size()
	var cell_w = int(tex_size.x / drink.sheet_cols)
	var cell_h = int(tex_size.y / drink.sheet_rows)

	if cell_w <= 0 or cell_h <= 0:
		push_error("Invalid cols/rows for drink: %s" % drink.display_name)
		return frames

	var max_frames = drink.sheet_cols * drink.sheet_rows

	var limit = int(drink.frame_count)
	if limit <= 1:
		limit = max_frames
	limit = clamp(limit, 1, max_frames)

	var fps = max(float(drink.anim_fps), 1.0)

	# get image so we can skip empty cells
	var img: Image = drink.sprite_sheet.get_image()
	if img != null and img.is_compressed():
		img.decompress()

	# idle
	frames.add_animation("idle")
	frames.set_animation_loop("idle", false)
	frames.set_animation_speed("idle", 1.0)
	frames.add_frame("idle", _make_atlas_frame(drink.sprite_sheet, 0, drink.sheet_cols, cell_w, cell_h))

	# open
	frames.add_animation("open")
	frames.set_animation_loop("open", false)
	frames.set_animation_speed("open", fps)

	for i in range(1, limit):
		var col = i % drink.sheet_cols
		var row = int(i / drink.sheet_cols)
		var rect = Rect2i(col * cell_w, row * cell_h, cell_w, cell_h)

		if img != null and not _region_has_alpha(img, rect):
			continue

		frames.add_frame("open", _make_atlas_frame(drink.sprite_sheet, i, drink.sheet_cols, cell_w, cell_h))

	return frames


func _spawn_and_drop_drink(drink: DrinkData) -> void:
	if _milk_sprite != null:
		return

	_active_drink = drink
	_milk_waiting_for_space = false
	_milk_waiting_to_close = false

	var spr = AnimatedSprite2D.new()
	spr.process_mode = Node.PROCESS_MODE_ALWAYS  # ensures it animates even if parent is paused/disabled
	spr.sprite_frames = _build_drink_frames(drink)

	spr.animation = "idle"
	spr.frame = 0
	spr.play("idle")
	spr.stop()

	spr.centered = true
	spr.z_index = 120

	var end_pos = shop_center.global_position
	var start_pos = end_pos + spawn_offset
	spr.global_position = start_pos
	add_child(spr)

	_milk_sprite = spr

	var tween = create_tween()
	tween.tween_property(spr, "global_position", end_pos, drop_time)
	await tween.finished

	_milk_waiting_for_space = true


func _start_milk_open() -> void:
	if _milk_sprite == null or _active_drink == null:
		return

	_milk_waiting_for_space = false
	_milk_waiting_to_close = false

	var sf = _milk_sprite.sprite_frames
	if sf == null or not sf.has_animation("open") or sf.get_frame_count("open") <= 0:
		push_error("Open animation has no frames for drink: %s" % _active_drink.display_name)
		return

	if _active_drink.use_pour_sfx:
		_pour_sfx_start()

	_play_drink_open_sfx(_active_drink)

	_milk_sprite.animation_finished.connect(_on_drink_open_finished, Object.CONNECT_ONE_SHOT)

	_drink_close_armed = false
	_milk_sprite.animation = "open"
	_milk_sprite.frame = 0
	_milk_sprite.play()


func _on_drink_open_finished() -> void:
	if _milk_sprite == null or _active_drink == null:
		return

	if _active_drink.use_pour_sfx:
		_pour_sfx_stop()

	var sf = _milk_sprite.sprite_frames
	var last_open = sf.get_frame_count("open") - 1

	_milk_sprite.animation = "open"
	_milk_sprite.stop()
	_milk_sprite.frame = max(last_open, 0)

	_milk_waiting_to_close = true
	#print("Drink finished, waiting_to_close =", _milk_waiting_to_close)


func _pour_sfx_start() -> void:
	if pour_noise == null:
		return

	if pour_noise.playing:
		pour_noise.stop()
	pour_noise.play()


func _pour_sfx_stop() -> void:
	if pour_noise == null:
		return

	if pour_noise.playing:
		pour_noise.stop()


func _close_current_drink() -> void:
	if _active_drink != null and _active_drink.sip and sip_drink != null:
		if sip_drink.playing:
			sip_drink.stop()
		sip_drink.play()

	_milk_waiting_to_close = false
	_milk_waiting_for_space = false

	_pour_sfx_stop()
	_active_drink = null

	if _milk_sprite != null:
		if _milk_sprite.animation_finished.is_connected(_on_drink_open_finished):
			_milk_sprite.animation_finished.disconnect(_on_drink_open_finished)

		_milk_sprite.queue_free()
		_milk_sprite = null
	#print("Closed drink -> sprite null =", _milk_sprite == null)


func _region_has_alpha(img: Image, rect: Rect2i) -> bool:
	var step = 4
	var x0 = clamp(rect.position.x, 0, img.get_width() - 1)
	var y0 = clamp(rect.position.y, 0, img.get_height() - 1)
	var x1 = clamp(rect.position.x + rect.size.x, 0, img.get_width())
	var y1 = clamp(rect.position.y + rect.size.y, 0, img.get_height())

	for y in range(y0, y1, step):
		for x in range(x0, x1, step):
			if img.get_pixel(x, y).a > 0.05:
				return true
	return false


func _play_drink_open_sfx(drink: DrinkData) -> void:
	if drink_one_shot_sfx == null:
		return
	if drink == null or drink.open_sfx == null:
		return

	if drink_one_shot_sfx.playing:
		drink_one_shot_sfx.stop()

	drink_one_shot_sfx.stream = drink.open_sfx
	drink_one_shot_sfx.play()


func _play_sip_drink(stream: AudioStream) -> void:
	if stream == null or sip_drink == null:
		return
	if sip_drink.playing:
		sip_drink.stop()
	sip_drink.stream = stream
	sip_drink.play()
