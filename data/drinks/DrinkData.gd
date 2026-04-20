extends Resource
class_name DrinkData

@export var id = ""              
@export var display_name = ""      
@export var cost_tokens = 1

# Sprite sheet animation data
@export var sprite_sheet: Texture2D
@export var sheet_cols = 1
@export var sheet_rows = 1
@export var frame_count = 1
@export var anim_fps = 18.0

@export var use_pour_sfx = false
@export var open_sfx: AudioStream
@export var sip: bool = false

@export var hover_toasts: PackedStringArray = []

func ensure_default_hover_toasts() -> void:
	if hover_toasts.size() > 0:
		return

	match id:
		"Milk":
			hover_toasts = PackedStringArray([
				"Mmm fountain milk: +Strength."
			])

		"Coffee":
			hover_toasts = PackedStringArray([
				"Fountain coffee?: +Speed."
			])

		"Rootbeer":
			hover_toasts = PackedStringArray([
				"Fizzy boost: +Impact."
			])

		"Critcola", "crit_cola", "crit-cola":
			hover_toasts = PackedStringArray([
				"Luck in a cup: +Crit Chance."
			])

		"Honeybeer":
			hover_toasts = PackedStringArray([
				"Sweet slowdown: Slower power meter."
			])

		"Martini":
			hover_toasts = PackedStringArray([
				"Shaken for speed: 1.5× Speed."
			])

		"Xxxbrew":
			hover_toasts = PackedStringArray([
				"Mystery pirate fuel: 2× Impact."
			])

		"Coconut":
			hover_toasts = PackedStringArray([
				"2× Strength + 1-frame Coconut Ball."
			])

		_:
			hover_toasts = PackedStringArray([
				"%s: tasty and mysterious." % display_name
			])
