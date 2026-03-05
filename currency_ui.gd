extends CanvasLayer

@onready var pins_label: Label = $PinsLabel
@onready var tokens_label: Label = $TokensLabel
@onready var toast_label: Label = $Toast1

var _toast_tween: Tween = null
var _toast_version: int = 0

func _ready() -> void:
	# connect to global manager
	if not CurrencyManager.currencies_changed.is_connected(_on_currencies_changed):
		CurrencyManager.currencies_changed.connect(_on_currencies_changed)

	_on_currencies_changed(CurrencyManager.pins, CurrencyManager.tokens)

	# start hidden
	toast_label.visible = false

func _on_currencies_changed(new_pins: int, new_tokens: int) -> void:
	pins_label.text = "%d" % new_pins
	tokens_label.text = "%d" % new_tokens


func _show_typed_toast(full_text: String, seconds_after_done: float = 1.0, char_delay: float = 0.03) -> void:
	_toast_version += 1
	var my_version: int = _toast_version

	if _toast_tween != null and _toast_tween.is_valid():
		_toast_tween.kill()
	_toast_tween = null

	toast_label.visible = true
	toast_label.modulate.a = 1.0
	toast_label.text = ""

	for i in range(full_text.length()):
		if my_version != _toast_version:
			return
		toast_label.text = full_text.substr(0, i + 1)
		await get_tree().create_timer(char_delay).timeout

	if my_version != _toast_version:
		return

	_toast_tween = create_tween()
	_toast_tween.tween_interval(seconds_after_done)
	_toast_tween.tween_property(toast_label, "modulate:a", 0.0, 0.25)
	_toast_tween.tween_callback(func():
		if my_version != _toast_version:
			return
		toast_label.visible = false
		toast_label.modulate.a = 1.0
	)


func show_random_exchange_toast(seconds_after_done: float = 1.0, char_delay: float = 0.03) -> void:
	var lines: PackedStringArray = [
		"Pleasure doing business 😎",
		"Cha-ching!",
		"One token comin' up!",
		"Deal sealed.",
		"mmm.... Pins.",
		"Tokens for pins. No questions.",
		"A shadow always pays its debts.",
		"Good. More pins.",
		"These pins have stories.",
		"Heavy. Like secrets.",
		"Another offering to the dark.",
		"Fine pins… fine payment.",
		"Fair trade… for now.",
		"The shadows approve.",
		"This exchange is… inevitable.",
		"More pins, more fortune.",
		"What I do is just. Every horrid piece of it.",
	]
	var msg: String = lines[randi() % lines.size()]
	_show_typed_toast(msg, seconds_after_done, char_delay)


func show_random_not_enough_pins_toast(seconds_after_done: float = 1.2, char_delay: float = 0.03) -> void:
	var lines: PackedStringArray = [
		"Come back with more pins.",
		"Not enough pins.",
		"Five pins. Then we talk.",
		"No pins, no token.",
		"I don’t work for free.",
		"The shadows demand 5 pins.",
		"Short on pins… tragic.",
		"Your pockets are light.",
		"Bring pins. Take tokens. Simple.",
		"Pins first. Always.",
		"I wait. The dark is patient.",
		"Still short.",
		"Bring me five and we forget this happened.",
		"The token remains in the void.",
		"....."
	]
	var msg: String = lines[randi() % lines.size()]
	_show_typed_toast(msg, seconds_after_done, char_delay)
