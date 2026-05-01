extends Node

@export var total_pins_sync_interval_sec: float = 0.25

signal currencies_changed(new_pins: int, new_tokens: int)

var pins: int = 0
var tokens: int = 0

var _global_data: Node = null
var _last_total_pins: int = 0
var _sync_timer: Timer

func _ready() -> void:
	# grab GlobalData Current Total
	_global_data = get_node_or_null("/root/GlobalData")

	if _global_data != null:
		_last_total_pins = int(_global_data.get("TotalPins"))

	# Poll for increase
	_sync_timer = Timer.new()
	_sync_timer.wait_time = total_pins_sync_interval_sec
	_sync_timer.one_shot = false
	_sync_timer.autostart = true
	add_child(_sync_timer)
	_sync_timer.timeout.connect(_sync_from_global_total)

	_emit_changed()

func _sync_from_global_total() -> void:
	if _global_data == null:
		_global_data = get_node_or_null("/root/GlobalData")
		if _global_data == null:
			return
		
		_last_total_pins = int(_global_data.get("TotalPins"))
		return

	var total_now := int(_global_data.get("TotalPins"))

	# Only look for increase
	if total_now > _last_total_pins:
		var delta := total_now - _last_total_pins
		pins += delta
		_emit_changed()

	_last_total_pins = total_now

func add_pins(amount: int) -> void:
	if amount <= 0:
		return
	pins += amount
	_emit_changed()

func add_tokens(amount: int) -> void:
	if amount <= 0:
		return
	tokens += amount
	_emit_changed()

func spend_pins(amount: int) -> bool:
	if amount <= 0:
		return true
	if pins < amount:
		return false
	pins -= amount
	_emit_changed()
	return true

func spend_tokens(amount: int) -> bool:
	if amount <= 0:
		return true
	if tokens < amount:
		return false
	tokens -= amount
	_emit_changed()
	return true

func _emit_changed() -> void:
	currencies_changed.emit(pins, tokens) 

func convert_pins_to_coin(pin_cost: int = 5, coin_amount: int = 1) -> bool:
	if pin_cost <= 0 or coin_amount <= 0:
		return false

	if pins < pin_cost:
		return false

	pins -= pin_cost
	tokens += coin_amount
	_emit_changed()
	return true
