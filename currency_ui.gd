extends CanvasLayer

@onready var pins_label: Label = $PinsLabel
@onready var coins_label: Label = $CoinsLabel

func _ready() -> void:
	# connect to global manager
	if not CurrencyManager.currencies_changed.is_connected(_on_currencies_changed):
		CurrencyManager.currencies_changed.connect(_on_currencies_changed)

	# show current values right away
	_on_currencies_changed(CurrencyManager.pins, CurrencyManager.coins)

func _on_currencies_changed(new_pins: int, new_coins: int) -> void:
	pins_label.text = "%d" % new_pins
	coins_label.text = "%d" % new_coins
