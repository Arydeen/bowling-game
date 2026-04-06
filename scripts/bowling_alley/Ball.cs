using Godot;
using System;

public partial class Ball : CharacterBody2D
{

	public enum BallState {	Aiming, // Aiming side to side, first state of the ball
							Powering, // State ball is in when the power meter is up
							Rolling, // Ball state for rolling, most physics and collisions handled here
							Gutter, // Ball state for when ball is in the gutter
							Bouncing, // Bouncing is when ball hits a pin, but does not kill, and rolls backward 
							Finished // Finished is for when ball has exited in some manner, and reset logic is being applied
							}
	private BallState _currState = BallState.Aiming;

	private AudioStreamPlayer2D _rollAudio;
	private AudioStreamPlayer2D _thudAudio;

	[Export] public float AimSpeed = 150f;
	[Export] public float RollSpeed = 60.0f;
	[Export] public int BallDamage = 0;
	[Export] public PowerMeter Meter;

	private bool _aimingLeft = false;
	private float _laneWidthLimit = 90.0f;
	private float _startX; // The center of the lane
	private float _currentOffset = 0f; // How far moved from center
	private float _powerVal = 0;
	private float _gutterDirection = 0f;

	public override void _Ready()
	{
		_startX = Position.X;
		_currState = BallState.Aiming;
		GetNode<Area2D>("Hitbox").AreaEntered += OnGutterEntered;
		_rollAudio = GetNode<AudioStreamPlayer2D>("RollSound");
		_thudAudio = GetNode<AudioStreamPlayer2D>("ThudSound");
	}

	public void Initialize(Vector2 startPos)
	{
		GlobalPosition = startPos;
		_startX = startPos.X;
		_currentOffset = 0f;
		_currState = BallState.Aiming;
	}

	private void OnGutterEntered(Area2D area)
	{
		if (area.IsInGroup("Gutters"))
		{
			_thudAudio.Play();
			_currState = BallState.Gutter;
			string gutterName = area.Name.ToString();

			if (gutterName.Contains("Left"))
			{
				_gutterDirection = 1f;
			}
			else if (gutterName.Contains("Right"))
			{
				_gutterDirection = -1f;
			}
		}
	}

	private void HandleAiming(double delta)
	{
		if (_currentOffset >= _laneWidthLimit) _aimingLeft = true;
		else if (_currentOffset <= -_laneWidthLimit) _aimingLeft = false;

		float dir = _aimingLeft ? -1 : 1;
		_currentOffset += dir * AimSpeed * (float)delta;

		Position = new Vector2(_startX + _currentOffset, Position.Y);

		if (Input.IsActionJustPressed("ball_aim_stop"))
		{
			_currState = BallState.Powering;
			Meter.ShowMeter(); 

			GetViewport().SetInputAsHandled();
		}
	}
	public void FinalizePower(float speed, float rawX)
	{
		GD.Print($"Zone Speed: {speed}, Raw X: {rawX}");
		RollSpeed = speed;
		BallDamage = ((int) speed) + 20;
		_powerVal = rawX;
		_thudAudio.Play();
		_currState = BallState.Rolling;
	}

	private void UpdateScale()
	{
		float startY = 169; // Bottom of lane
		float endY = 81;   // Top of lane (the pins)
		float minScale = 0.5f;
		float maxScale = 1.0f;

		// Remap the current Y position to a scale value
		float t = Mathf.Remap(GlobalPosition.Y, endY, startY, minScale, maxScale);
		Scale = new Vector2(t, t);
	}

	public void CheckForHits()
	{
		Area2D hitbox = GetNode<Area2D>("Hitbox");
		var areas = hitbox.GetOverlappingAreas();

		foreach (Area2D area in areas)
		{
			if (area is Pin pin)
			{
				if (!pin.GetHitThisRound())
				{
					pin.TakeDamage(BallDamage);
					pin.SetHitThisRound(true);
					BallDamage = BallDamage / 2;

					if (pin.Visible)
					{
						StartBounce();
						break;
					}
				} 
				
			}
		}
	}

	private void StartBounce()
	{
		_currState = BallState.Bouncing;
		_thudAudio.Play();

		float bounceBackStrength = 0.3f;
		float bounceNoise = (float)GD.RandRange(-20, 20);

		Velocity = new Vector2(Velocity.X + bounceNoise, -Velocity.Y) * bounceBackStrength;
		FadeOutAndRemove();
	}

	public void FadeOutAndRemove()
	{
		Tween tween = CreateTween();

		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.25f);

		tween.Finished += () =>
		{
			QueueFree();
			_currState = BallState.Aiming;
		};
	}
	public override void _PhysicsProcess(double delta)
	{
		if (_currState == BallState.Rolling) 
		{

			if (!_rollAudio.Playing)
			{
				_rollAudio.Play();
			}
			CheckForHits();

			// IMPORTANT: Checks if ball bounced, not pointless I promise
			if (_currState != BallState.Rolling) return;

			// Hook Strength: Adjusting val, but 0.8 feels good for now.
			float hookStrength = 0.8f;
			float horizontalDrift = _powerVal / 2 * hookStrength;

			// Apply drift to X, and speed to Y
			Velocity = new Vector2(horizontalDrift, -RollSpeed);
		} 
		else if (_currState == BallState.Gutter)
		{
			// If ball in gutter, follow track of the gutter
			float x_val = 20 + (GlobalPosition.Y < 120 ? 5 : 0);
			Velocity = new Vector2(x_val * _gutterDirection, -30);
		} 
		else if (_currState == BallState.Bouncing)
		{
			// Allow the ball to move based on the bounce velocity set in StartBounce()
			// We might want to add some friction/deceleration here
			Velocity *= 0.95f; 
		}
		else
		{
			_rollAudio.Stop();
			return;
		}
		MoveAndSlide();

		// Check if we reached the end of the lane
		if (GlobalPosition.Y <= 81.0f && _currState != BallState.Bouncing)
		{
			Velocity = Vector2.Zero;
			SetPhysicsProcess(false); 
			FadeOutAndRemove();
		}
	}
	public override void _Process(double delta)
	{

		switch (_currState)
		{
			case BallState.Aiming:
				HandleAiming(delta);
				break;
			case BallState.Powering:
				// Do nothing here we are waiting for the Meter to finish
				break;
			case BallState.Rolling:
				UpdateScale(); // Only scale while rolling or in Gutter
				break;
			case BallState.Gutter:
				UpdateScale();
				break;
		}
	}

}
