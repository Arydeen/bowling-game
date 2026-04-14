extends CanvasLayer

@onready var prize_flow: HFlowContainer = $Root/Menu/PrizeScrollContainer/PrizeFlow
@onready var drink_flow: HFlowContainer = $Root/Menu/DrinkScrollContainer/DrinkFlow

@export var prize_icon_dir: String = "res://textures/prizes/"          # recursive (common/rare/epic/legendary)
@export var drink_icon_dir: String = "res://textures/drink_icons/"     # flat folder: rootbeer.png, honeybeer.png, etc.

# icon sizes
@export var icon_size_max: int = 35
@export var icon_size_min: int = 15
@export var shrink_start: int = 6
@export var shrink_full: int = 20

# no gaps between icons
@export var icon_spacing: int = 0

# font for x2/x3...
@export var count_font: FontFile

# ------- PRIZES -------
var _prize_slots: Dictionary[StringName, Control] = {}
var _prize_tex_cache: Dictionary[StringName, Texture2D] = {}
var _prize_icon_path_by_id: Dictionary[StringName, String] = {}

# ------- DRINKS -------
var _drink_slots: Dictionary[StringName, Control] = {}
var _drink_tex_cache: Dictionary[StringName, Texture2D] = {}
var _drink_counts: Dictionary[StringName, int] = {}


func _ready() -> void:
	_setup_flow(prize_flow)
	_setup_flow(drink_flow)

	# prizes: index recursively so common/rare/etc works
	_build_prize_icon_index(prize_icon_dir, _prize_icon_path_by_id, _prize_tex_cache)

	if not Player.prize_count_changed.is_connected(_on_prize_changed):
		Player.prize_count_changed.connect(_on_prize_changed)
	if not Player.drink_count_changed.is_connected(_on_drink_changed):
		Player.drink_count_changed.connect(_on_drink_changed)

	call_deferred("_rebuild_all")


func _setup_flow(flow: HFlowContainer) -> void:
	flow.add_theme_constant_override("h_separation", icon_spacing)
	flow.add_theme_constant_override("v_separation", icon_spacing)
	flow.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	flow.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	flow.size_flags_vertical = Control.SIZE_EXPAND_FILL


func _rebuild_all() -> void:
	_rebuild_from_dict(prize_flow, Player.prizes, _prize_slots, true)
	_rebuild_from_dict(drink_flow, Player.drinks, _drink_slots, false)
	_update_all_sizes()


func _rebuild_from_dict(flow: HFlowContainer, dict: Dictionary, slots: Dictionary[StringName, Control], is_prize: bool) -> void:
	for child in flow.get_children():
		child.queue_free()
	slots.clear()

	for k in dict.keys():
		var id: StringName = k
		var count: int = int(dict[k])

		if not is_prize:
			id = StringName(String(id).to_lower())
			_drink_counts[id] = count
		_create_or_update_slot(flow, slots, id, count, is_prize)


# --- Signals ---
func _on_prize_changed(prize_id: StringName, new_count: int) -> void:
	_create_or_update_slot(prize_flow, _prize_slots, prize_id, new_count, true)
	_update_all_sizes()

func _on_drink_changed(drink_id: StringName, new_count: int) -> void:
	var id := StringName(String(drink_id).to_lower())
	_drink_counts[id] = new_count
	_create_or_update_slot(drink_flow, _drink_slots, id, new_count, false)
	_update_all_sizes()


# --- Slot create/update ---
func _create_or_update_slot(flow: HFlowContainer, slots: Dictionary[StringName, Control], id: StringName, count: int, is_prize: bool) -> void:
	if count <= 0:
		if slots.has(id):
			slots[id].queue_free()
			slots.erase(id)
		return

	var slot: Control = slots.get(id, null)
	if slot == null:
		slot = _make_slot(id, is_prize)
		slots[id] = slot
		flow.add_child(slot)

	_update_slot_count(slot, count)


func _make_slot(id: StringName, is_prize: bool) -> Control:
	var size: int = _compute_icon_size(_prize_slots.size() + _drink_slots.size())

	var slot := Control.new()
	slot.name = String(id)
	slot.mouse_filter = Control.MOUSE_FILTER_PASS
	slot.custom_minimum_size = Vector2(size, size)

	var icon := TextureRect.new()
	icon.name = "Icon"
	icon.mouse_filter = Control.MOUSE_FILTER_PASS
	icon.anchor_left = 0
	icon.anchor_top = 0
	icon.anchor_right = 1
	icon.anchor_bottom = 1
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.texture = _get_texture(id, is_prize)
	slot.add_child(icon)

	var lbl := Label.new()
	lbl.name = "Count"
	lbl.mouse_filter = Control.MOUSE_FILTER_PASS
	lbl.z_index = 10
	lbl.anchor_left = 0
	lbl.anchor_top = 0
	lbl.anchor_right = 1
	lbl.anchor_bottom = 0
	lbl.offset_right = -2
	lbl.offset_top = 2
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	lbl.add_theme_color_override("font_outline_color", Color(0, 0, 0))
	lbl.add_theme_constant_override("outline_size", 3)

	if count_font != null:
		lbl.add_theme_font_override("font", count_font)

	slot.add_child(lbl)
	return slot


func _update_slot_count(slot: Control, count: int) -> void:
	var lbl := slot.get_node("Count") as Label
	lbl.text = "" if count <= 1 else "x%d" % count

	var s: int = int(slot.custom_minimum_size.x)
	lbl.add_theme_font_size_override("font_size", clamp(int(s * 0.14), 7, 12))


# --- Textures ---
func _get_texture(id: StringName, is_prize: bool) -> Texture2D:
	if is_prize:
		if _prize_tex_cache.has(id):
			return _prize_tex_cache[id]

		var tex: Texture2D = null
		if _prize_icon_path_by_id.has(id):
			tex = load(_prize_icon_path_by_id[id]) as Texture2D
		else:
			push_warning("Menu: Missing prize icon for '%s'" % String(id))

		_prize_tex_cache[id] = tex
		return tex

	# DRINKS: direct path load, always lowercase id -> file
	if _drink_tex_cache.has(id):
		return _drink_tex_cache[id]

	var base := drink_icon_dir
	if not base.ends_with("/"):
		base += "/"

	var key := StringName(String(id).to_lower())
	var path := "%s%s.png" % [base, String(key)]

	var tex2: Texture2D = null
	if ResourceLoader.exists(path):
		tex2 = load(path) as Texture2D
	else:
		push_warning("Menu: Missing drink icon file: %s" % path)

	_drink_tex_cache[id] = tex2
	return tex2


# --- Sizes ---
func _compute_icon_size(unique_total: int) -> int:
	var n: int = max(unique_total, 1)
	if n <= shrink_start:
		return icon_size_max

	var denom: int = max(shrink_full - shrink_start, 1)
	var t: float = clamp((n - shrink_start) / float(denom), 0.0, 1.0)
	return int(lerp(float(icon_size_max), float(icon_size_min), t))


func _update_all_sizes() -> void:
	var size: int = _compute_icon_size(_prize_slots.size() + _drink_slots.size())

	for id in _prize_slots.keys():
		var slot := _prize_slots[id]
		slot.custom_minimum_size = Vector2(size, size)
		_update_slot_count(slot, int(Player.prizes.get(id, 1)))

	for id2 in _drink_slots.keys():
		var slot2 := _drink_slots[id2]
		slot2.custom_minimum_size = Vector2(size, size)
		var c := int(_drink_counts.get(id2, 1))
		_update_slot_count(slot2, c)


# -----------------------
# Prize icon indexing (recursive)
# -----------------------
func _build_prize_icon_index(root_dir: String, out_map: Dictionary[StringName, String], tex_cache: Dictionary[StringName, Texture2D]) -> void:
	out_map.clear()
	tex_cache.clear()

	var root := root_dir
	if not root.ends_with("/"):
		root += "/"

	_index_dir_recursive(root, out_map)


func _index_dir_recursive(dir_path: String, out_map: Dictionary[StringName, String]) -> void:
	var dir := DirAccess.open(dir_path)
	if dir == null:
		push_warning("Menu: Can't open dir: %s" % dir_path)
		return

	dir.list_dir_begin()
	var f := dir.get_next()
	while f != "":
		if f == "." or f == "..":
			f = dir.get_next()
			continue

		var full := dir_path.path_join(f)

		if dir.current_is_dir():
			_index_dir_recursive(full, out_map)
		else:
			var ext := f.get_extension().to_lower()
			if ext in ["png", "webp", "jpg", "jpeg"]:
				var id := StringName(f.get_basename())
				if not out_map.has(id):
					out_map[id] = full

		f = dir.get_next()
	dir.list_dir_end()
