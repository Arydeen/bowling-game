extends Node2D

signal prize_spawned(item_id: String, rarity: int)

@export var common_items: Array[Texture2D] = []
@export var rare_items: Array[Texture2D] = []
@export var epic_items: Array[Texture2D] = []
@export var legendary_items: Array[Texture2D] = []

@export var pop_offset: Vector2 = Vector2(0, -80)
@export var pop_time: float = 0.35
@export var start_scale: float = 0.55
@export var end_scale: float = 1.0

var _prize_sprite: Sprite2D
var _current_item_id: String = ""
var _current_rarity: int = -1

func pop_prize(rarity: int, from_global_pos: Vector2) -> void:
	clear_prize()

	var tex := _pick_texture_for_rarity(rarity)
	if tex == null:
		return

	_current_rarity = rarity
	_current_item_id = _texture_to_id(tex)

	_prize_sprite = Sprite2D.new()
	_prize_sprite.texture = tex
	_prize_sprite.centered = true
	_prize_sprite.z_index = 160

	_prize_sprite.light_mask = 0

	var mat := CanvasItemMaterial.new()
	mat.light_mode = CanvasItemMaterial.LIGHT_MODE_UNSHADED
	_prize_sprite.material = mat

	add_child(_prize_sprite)
	_prize_sprite.global_position = from_global_pos
	_prize_sprite.scale = Vector2.ONE * start_scale

	var tween := create_tween()
	tween.tween_property(
		_prize_sprite,
		"global_position",
		from_global_pos + pop_offset,
		pop_time
	).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	tween.parallel().tween_property(
		_prize_sprite,
		"scale",
		Vector2.ONE * end_scale,
		pop_time
	).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	prize_spawned.emit(_current_item_id, rarity)

func clear_prize() -> void:
	_current_item_id = ""
	_current_rarity = -1

	if _prize_sprite != null and is_instance_valid(_prize_sprite):
		_prize_sprite.queue_free()
	_prize_sprite = null

func get_current_item_id() -> String:
	return _current_item_id

func _pick_texture_for_rarity(rarity: int) -> Texture2D:
	var pool: Array[Texture2D] = []

	match rarity:
		0: pool = common_items      # COMMON
		1: pool = rare_items        # RARE
		2: pool = epic_items        # EPIC
		3: pool = legendary_items   # LEGENDARY
		_: pool = common_items

	if pool.is_empty():
		push_warning("CapsulePrize: No textures set for rarity %s" % str(rarity))
		return null

	return pool[randi() % pool.size()]

func _texture_to_id(tex: Texture2D) -> String:
	if tex == null:
		return ""
	if tex.resource_path == "":
		return "prize"
	return tex.resource_path.get_file().get_basename()
