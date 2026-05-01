extends Node

const PrizeEffect := preload("res://scripts/arcade/prize_effects.gd")

signal drink_count_changed(drink_id: StringName, new_count: int)
signal prize_count_changed(prize_id: StringName, new_count: int)
signal stats_changed()

# -------------------------
# Manual starting inventory (Debugging)
# -------------------------
@export var clear_prizes_on_ready: bool = true

var starting_prizes: Dictionary = {
	&"CreamShammy": 2,
	&"OneNail": 1,
	&"AfterImage": 2,
	&"KineticImpact": 1,
	&"SouvenirCup": 3,
	&"OneBumper": 10,
}

# StringName -> int
var drinks: Dictionary = {}
var prizes: Dictionary = {}

var pins: int = 0
var tokens: int = 0

# -------------------------
# Player stats
# -------------------------
const MAX_SPEED: float = 120.0

var strength: float = 0.0
var _speed: float = 0.0
var impact: float = 0.0
var crit_chance: float = 0.01 # 1%
var bumpers: float = 0.0

var speed: float:
	get:
		return _speed
	set(value):
		_speed = min(value, MAX_SPEED)

# Coconut ball: temporary +30% strength for 1 frame
var coconut_ball_frames_left: int = 0

var rubber_ball_count: int = 0;

var after_image_count: int = 0
var split_count: int = 0
var pin_slayer_count: int = 0

var souvenir_cup_count: int = 0
var kinetic_impact_count: int = 0
var golden_rotation_count: int = 0

# -------------------------
# Getters
# -------------------------

func get_discounted_drink_cost(base_cost: int) -> int:
	if base_cost <= 1:
		return 1
	return max(1, base_cost - souvenir_cup_count)

func get_kinetic_impact_mult() -> int:
	return 1 + kinetic_impact_count

func get_after_image_count() -> int:
	return after_image_count

func get_strength_value() -> float:
	var temp_mult := 1.3 if coconut_ball_frames_left > 0 else 1.0
	return strength * temp_mult

func get_speed_value() -> float:
	return speed

func get_impact_value() -> float:
	return impact

func get_crit_chance() -> float:
	return crit_chance

func get_bumpers() -> float:
	return bumpers

func get_stats_snapshot() -> Dictionary:
	return {
		"strength": strength,
		"speed": speed,
		"impact": impact,
		"crit_chance": crit_chance,
		"bumpers": bumpers,
		"coconut_ball_frames_left": coconut_ball_frames_left,
		"after_image_count": after_image_count,
	}

# END Getters

func _ready() -> void:
	_hook_currency_manager()
	_apply_starting_prizes()
	speed = speed

func _apply_starting_prizes() -> void:
	if clear_prizes_on_ready:
		prizes.clear()

	for raw_id in starting_prizes.keys():
		add_prize(String(raw_id), -1, max(1, int(starting_prizes[raw_id])))

func _hook_currency_manager() -> void:
	var cm := get_node_or_null("/root/CurrencyManager")
	if cm == null:
		return

	# connect once
	if cm.has_signal("currencies_changed") and not cm.currencies_changed.is_connected(_on_currencies_changed):
		cm.currencies_changed.connect(_on_currencies_changed)

	# pull initial values
	if cm.has_method("get"): # just a safe guard; cm is a Node anyway
		pins = int(cm.pins)
		tokens = int(cm.tokens)

func _on_currencies_changed(new_pins: int, new_tokens: int) -> void:
	pins = new_pins
	tokens = new_tokens

func add_drink(d: Resource, amount: int = 1) -> void:
	if d == null or amount <= 0:
		return

	var id := _drink_id(d)
	if id == &"":
		return

	drinks[id] = int(drinks.get(id, 0)) + amount
	drink_count_changed.emit(id, int(drinks[id]))

	for i in range(amount):
		_apply_drink_effect(id)

	stats_changed.emit()
	_print_inventory()

func add_prize(item_id: String, _rarity: int = -1, amount: int = 1) -> void:
	if item_id == "" or amount <= 0:
		return

	var key := StringName(item_id) # keep EXACT ids

	prizes[key] = int(prizes.get(key, 0)) + amount
	prize_count_changed.emit(key, int(prizes[key]))

	PrizeEffect.apply_delta(self, key, amount)

	stats_changed.emit()
	_print_inventory()

func get_drink_count(drink_id: StringName) -> int:
	return int(drinks.get(drink_id, 0))

func get_prize_count(prize_id: StringName) -> int:
	return int(prizes.get(prize_id, 0))

# -------------------------
# Drinking / applying effects
# -------------------------

func consume_drink(d: Resource, amount: int = 1) -> bool:
	if d == null or amount <= 0:
		return false
	return consume_drink_id(_drink_id(d), amount)

func consume_drink_id(drink_id: StringName, amount: int = 1) -> bool:
	if drink_id == &"" or amount <= 0:
		return false

	var have := get_drink_count(drink_id)
	if have < amount:
		return false

	var new_count := have - amount
	if new_count <= 0:
		drinks.erase(drink_id)
		new_count = 0
	else:
		drinks[drink_id] = new_count

	drink_count_changed.emit(drink_id, new_count)

	# apply effects per drink consumed
	for i in range(amount):
		_apply_drink_effect(drink_id)

	_print_inventory()
	return true

func _apply_drink_effect(drink_id: StringName) -> void:
	var key := String(drink_id).to_lower().strip_edges().replace(" ", "_")

	match key:
		&"milk":
			strength += 5.0
		&"coffee":
			speed += 5.0
		&"rootbeer":
			impact += 5.0
		&"critcola":
			crit_chance += 0.05

		&"martini":
			speed *= 1.5
		&"xxxbrew":
			impact *= 2.0
		&"coconut":
			strength *= 2.0
			coconut_ball_frames_left = max(coconut_ball_frames_left, 1)

		_:
			pass

func on_frame_end() -> void:
	if coconut_ball_frames_left > 0:
		coconut_ball_frames_left -= 1
		if coconut_ball_frames_left == 0:
			stats_changed.emit()

# -------------------------
# Helpers
# -------------------------
func _drink_id(d: Resource) -> StringName:
	var id_val = _try_get_prop(d, &"id")
	if typeof(id_val) == TYPE_STRING and String(id_val) != "":
		return StringName(id_val)

	if d.resource_path != "":
		return StringName(d.resource_path)

	var name_val = _try_get_prop(d, &"display_name")
	if typeof(name_val) == TYPE_STRING and String(name_val) != "":
		return StringName(String(name_val).to_lower().replace(" ", "_"))

	return &""

func _try_get_prop(obj: Object, prop: StringName) -> Variant:
	for p in obj.get_property_list():
		if p.name == prop:
			return obj.get(prop)
	return null

func _print_inventory() -> void:
	print("=== INVENTORY ===")
	print("Pins: ", pins, " | Tokens: ", tokens)
	print("Drinks: ", drinks)
	print("Prizes: ", prizes)
	print("Stats: ", get_stats_snapshot())
	print("=================")
