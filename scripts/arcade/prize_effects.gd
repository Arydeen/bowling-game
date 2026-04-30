extends RefCounted
class_name PrizeEffects

static func apply_delta(player: Object, prize_id: StringName, delta_count: int) -> void:
	if player == null:
		return

	var c = max(0, int(delta_count))
	if c == 0:
		return

	match prize_id:
		# COMMON

		&"CreamShammy":
			player.speed += 6.0 * c

		&"OneNail":
			player.impact += 6.0 * c

		&"WeightedBall":
			player.strength += 6.0 * c

		&"OneBumper":
			player.bumpers += 1.0

		#RARE

		&"LeadBall":
			player.strength += 8.0

		&"RedBall": 
			player.speed += 8.0 

		&"ThreeBumper":
			player.bumpers += 3.0

		#EPIC

		#LEGENDARY

		_:
			pass
