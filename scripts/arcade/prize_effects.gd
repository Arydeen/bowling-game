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
			player.bumpers += 1.0 * c

		#RARE

		&"LeadBall":
			player.strength += 8.0 * c

		&"RedBall": 
			player.speed += 8.0 * c

		&"ThreeBumper":
			player.bumpers += 3.0 * c

		&"RubberBall":
			player.bumpers += 2.0 * c
			player.rubber_ball_count += c

		#EPIC
		
		&"AfterImage":
			player.after_image_count += c

		&"PinSlayer":
			player.pin_slayer_count += c

		&"Split":
			player.split_count += c

		#LEGENDARY

		&"GoldenRotation":
			player.golden_rotation_count += c

		&"KineticImpact":
			player.kinetic_impact_count += c

		&"SouvenirCup":
			player.souvenir_cup_count += c

		_:
			pass
