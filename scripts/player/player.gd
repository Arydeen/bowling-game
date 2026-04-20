extends Node

signal drink_count_changed(drink_id: StringName, new_count: int)
signal prize_count_changed(prize_id: StringName, new_count: int)

# StringName -> int
var drinks: Dictionary = {}
var prizes: Dictionary = {}

var pins: int = 0
var tokens: int = 0

func _ready() -> void:
	_hook_currency_manager()

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
	
	_print_inventory()

func add_prize(item_id: String, _rarity: int = -1, amount: int = 1) -> void:
	if item_id == "" or amount <= 0:
		return

	var key := StringName(item_id)
	prizes[key] = int(prizes.get(key, 0)) + amount
	prize_count_changed.emit(key, int(prizes[key]))

	_print_inventory()

func get_drink_count(drink_id: StringName) -> int:
	return int(drinks.get(drink_id, 0))

func get_prize_count(prize_id: StringName) -> int:
	return int(prizes.get(prize_id, 0))

func _drink_id(d: Resource) -> StringName:
	var id_val = _try_get_prop(d, &"id")
	if typeof(id_val) == TYPE_STRING and String(id_val) != "":
		return StringName(id_val)

	# fallback:
	if d.resource_path != "":
		return StringName(d.resource_path)

	# last fallback: display_name
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
	print("=================")
