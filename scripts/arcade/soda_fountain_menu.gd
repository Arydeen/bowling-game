extends PopupPanel
class_name SodaFountainMenu

signal drink_chosen(drink: DrinkData)

@onready var list: ItemList = $Root/List

func _ready() -> void:
	list.item_clicked.connect(_on_item_clicked)

func open_menu(drinks: Array[DrinkData], rect: Rect2i) -> void:
	list.clear()

	for d in drinks:
		var cost := Player.get_discounted_drink_cost(d.cost_tokens)
		var idx := list.add_item("%s - %d" % [d.display_name, cost])

		list.set_item_metadata(idx, d)

		d.ensure_default_hover_toasts()
		if d.hover_toasts.size() > 0:
			list.set_item_tooltip(idx, d.hover_toasts[0])
		else:
			list.set_item_tooltip(idx, d.display_name)

	popup(rect)

func _on_item_clicked(index: int, _pos: Vector2, button: int) -> void:
	if button != MOUSE_BUTTON_LEFT:
		return

	var d: DrinkData = list.get_item_metadata(index)
	hide()
	drink_chosen.emit(d)


func _on_close_button_pressed() -> void:
	hide()
