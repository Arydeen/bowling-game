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
			player.add_speed_amount(4.0 * c)

		&"OneNail":
			player.impact += 4.0 * c

		&"WeightedBall":
			player.strength += 4.0 * c

		&"OneBumper":
			player.bumpers += 1.0 * c

		#RARE

		&"LeadBall":
			player.strength += 8.0 * c

		&"RedBall": 
			player.add_speed_amount(8.0 * c)

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
			if player.golden_rotation_count <= 0:
				player.golden_rotation_count += 1
				player.speed = player.MAX_SPEED
				player.bonus_speed = 0.0

				# If somehow more than 1 got added at once, extras become bonus speed
				if c > 1:
					player.bonus_speed += 16.0 * float(c - 1)
			else:
				player.golden_rotation_count += c
				player.bonus_speed += 16.0 * float(c)

		&"KineticImpact":
			player.kinetic_impact_count += c

		&"SouvenirCup":
			player.souvenir_cup_count += c

		_:
			pass
