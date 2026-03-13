extends Node

signal currencies_changed(new_pins: int, new_tokens: int)

var pins: int = 100
var tokens: int = 0

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

func _ready() -> void:
	_emit_changed()
