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
