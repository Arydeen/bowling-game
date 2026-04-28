using Godot;
using System;
using System.Formats.Tar;

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

	private AnimatedSprite2D _ballSprite;
	private AudioStreamPlayer2D _rollAudio;
	private AudioStreamPlayer2D _thudAudio;
	private GameManager _gameManager;

	[Export] public float AimSpeed = 150f; // Speed of ball when aiming
	[Export] public float RollSpeed = 60.0f; // Speed of ball rolling
	[Export] public int BallDamage = 0; // Impact damage of ball
	[Export] public PowerMeter Meter;

	private bool _aimingLeft = false;
	private float _laneWidthLimit = 90.0f;
	private float _startX; // The center of the lane
	private float _currentOffset = 0f; // How far moved from center
	private float _powerVal = 0; // Should be called angle
	private float _gutterDirection = 0f;
	private bool _isSweet = false; // If landed in sweet spot
	// private SpriteFrames _ballAnimation;


	// Startup Methods //
	public override void _Ready()
	{
		_startX = Position.X;
		_currState = BallState.Aiming;

		// Texture Handling
		// _ballAnimation = GetNode<SpriteFrames>("");
		_ballSprite = GetNode<AnimatedSprite2D>("BallSprite");

		GetNode<Area2D>("Hitbox").AreaEntered += OnGutterEntered;
		GetNode<Area2D>("Hitbox").AreaEntered += HitBumper;
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
	// End Startup Methods //

	// State Handling //
	public override void _Process(double delta)
	{

		switch (_currState)
		{
			case BallState.Aiming:
				HandleAiming(delta);
				if (_ballSprite.IsPlaying()) {_ballSprite.Stop();}
				break;
			case BallState.Powering:
				if (_ballSprite.IsPlaying()) {_ballSprite.Stop();}
				break;
			case BallState.Rolling:
				UpdateScale(); // Only scale while rolling or in Gutter
				if (!_ballSprite.IsPlaying()) {_ballSprite.Play();}
				break;
			case BallState.Gutter:
				UpdateScale();
				if (!_ballSprite.IsPlaying()) {_ballSprite.Play();}
				break;
		}
	}
	// End State Handling //

	// Gutter Methods //
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
	// End Gutter Methods

	// Aiming and Power Methods
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

	public void FinalizePower(float speed, float rawX, bool sweet)
	{
		GD.Print($"Zone Speed: {speed}, Raw X: {rawX}");
		RollSpeed = speed;
		BallDamage = ((int) speed) + 20;
		_powerVal = rawX;
		_thudAudio.Play();
		_currState = BallState.Rolling;
		_isSweet = sweet;
	}
	// End Aiming and Power Methods //
	
	// Movement Methods //
	private void UpdateScale()
	{
		float startY = 175; // Bottom of lane
		float endY = 81;   // Top of lane (the pins)
		float minScale = 0.5f;
		float maxScale = 1.0f;

		// Remap the current Y position to a scale value
		float t = Mathf.Remap(GlobalPosition.Y, endY, startY, minScale, maxScale);
		Scale = new Vector2(t, t);
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
		Tween tween = CreateTween().SetParallel(true);;

		tween.TweenProperty(this, "scale", new Vector2(0.5f, 0.5f), 0.05);
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.25f);

		tween.Finished += () =>
		{
			QueueFree();
			_currState = BallState.Aiming;
		};
	}
	// End Movement Methods //

	// Physics Methods //
	public void HitBumper(Area2D area)
	{
		if (area.IsInGroup("Bumpers"))
		{
			_thudAudio.Play();
			string bumperName = area.Name.ToString();

			if (bumperName.Contains("Left"))
			{
				_powerVal = _isSweet ? 300 : 200f;
				BallDamage = (int)Math.Ceiling(BallDamage * 1.25);
			}
			else if (bumperName.Contains("Right"))
			{
				_powerVal = _isSweet ? -300 : -200f;
				BallDamage = (int)Math.Ceiling(BallDamage * 1.25);
			}
		}
	}

	public void CheckForHits()
	{
		Area2D hitbox = GetNode<Area2D>("Hitbox");
		var areas = hitbox.GetOverlappingAreas();

		foreach (Area2D area in areas)
		{
			if (area is Pin pin)
			{
				if (!pin._hitThisShot)
				{
					pin.TakeDamage(BallDamage, _isSweet, 1);
					pin._hitThisShot = true;
					BallDamage = BallDamage / 3;

					if (pin.Alive)
					{
						StartBounce();
						break;
					}
				} 
				
			} else if (area is BossPin boss)
			{
				if (boss.Alive)
				{
					GD.Print("Boss Hit");
					if (!boss._hitThisShot)
					{
						boss.TakeDamage(BallDamage, _isSweet, 1);
						boss._hitThisShot = true;

						if (boss.Alive)
						{
							StartBounce();
							break;
						}
					}
				}
			}
		}
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
			_powerVal = Mathf.MoveToward(_powerVal, Mathf.Clamp(_powerVal, -20, 20), (float)delta * 10f);
			float horizontalDrift = _powerVal * hookStrength;

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
	// End Physics Methods
}
