extends CanvasLayer
class_name PrizeDescription

@export var show_time := 1.6
@export var fade_time := 0.18

@export var prize_messages := {
	"CreamShammy": "Shammy: Polish for a Speed boost.",
	"OneBumper": "Bumper: +1 Bumper bounce per bowl.",
	"OneNail": "Nail: Boosts Impact damage.",
	"WeightedBall": "Weighted Ball: Strength up.",
	"AfterImage": "After Image: Bowl a second ball.",
	"PinSlayer": "Pin Slayer: First pin hit loses 50% Armor.",
	"Split": "Split: Press Space while rolling to split.",
	"GoldenRotation": "Golden Rotation: Max Speed, converts extra to Shake Damage.",
	"SouvenirCup": "Souvenir Cup: Drinks costing 2+ are 1 Token cheaper.",
	"LeadBall": "Lead Ball: Strength increase.",
	"RedBall": "Red Ball: Speed increase.",
	"RubberBall": "Rubber Ball: Better Bumper bounces and +2 Bumpers.",
	"ThreeBumper": "Stack of Bumpers: +3 Bumpers."
}

var label: Label
var _tween: Tween

func _ready() -> void:
	label = get_node_or_null("label") as Label
	if label == null:
		label = get_node_or_null("Label") as Label

	if label == null:
		push_error("PrizeDescription: Couldn't find child Label named 'label' or 'Label'.")
		return

	label.visible = false
	label.modulate = Color(1, 1, 1, 0)

func show_prize(item_id: String, rarity: int) -> void:
	if label == null:
		return

	var msg := _message_for(item_id, rarity)
	_show_toast(msg)

func _message_for(item_id: String, rarity: int) -> String:
	if prize_messages.has(item_id):
		var custom := str(prize_messages[item_id]).strip_edges()
		if custom != "":
			return custom

	return "%s: %s" % [_rarity_name(rarity), _pretty_item_name(item_id)]

func _rarity_name(rarity: int) -> String:
	match rarity:
		0: return "Common"
		1: return "Rare"
		2: return "Epic"
		3: return "Legendary"
		_: return "Prize"

func _pretty_item_name(item_id: String) -> String:
	if item_id == "":
		return "Unknown"
	return item_id.replace("_", " ").capitalize()

func _show_toast(text: String) -> void:
	if label == null:
		return

	label.text = text
	label.visible = true

	# stop any current tween
	if _tween != null and _tween.is_valid():
		_tween.kill()

	# fade in (optional)
	label.modulate = Color(1, 1, 1, 0)
	_tween = create_tween()
	_tween.tween_property(label, "modulate", Color(1, 1, 1, 1), fade_time)


func hide_toast() -> void:
	if label == null:
		return
	if not label.visible:
		return

	if _tween != null and _tween.is_valid():
		_tween.kill()

	_tween = create_tween()
	_tween.tween_property(label, "modulate", Color(1, 1, 1, 0), fade_time)
	_tween.finished.connect(func():
		label.visible = false
	)
	
