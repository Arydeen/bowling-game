using Godot;
using System;
using System.Formats.Tar;
using System.Collections.Generic;

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

	[Export] public NodePath PlayerPath = new NodePath("/root/Player");

	[Export] public float AimSpeed = 150f; // Speed of ball when aiming
	[Export] public float RollSpeed = 60.0f; // Speed of ball rolling
	[Export] public int BallDamage = 0; // Impact damage of ball
	[Export] public  bool IsSweet = false; // If landed in sweet spot
	[Export] public PowerMeter Meter;

	private bool _aimingLeft = false;
	private float _laneWidthLimit = 90.0f;
	private float _startX; // The center of the lane
	private float _currentOffset = 0f; // How far moved from center
	private float _powerVal = 0; // Should be called angle
	private float _gutterDirection = 0f;

	// private SpriteFrames _ballAnimation;

	// After Image
	public bool IsAfterImage = false;
	public int AfterImageIndex = 0;
	private bool _afterImagesSpawned = false;

	// Split
	private readonly HashSet<Area2D> _bumpersHitThisBall = new();
	public bool IsSplitBall = false;
	public bool SplitScheduled = false;
	private bool _splitUsed = false;
	private bool _splitInputArmed = true;
	private bool _ballBounceCooldown = false;
	private float _ballRepelVelocityX = 0f;
	private float _sizeMult = 1f;

	public bool HasSplit => _splitUsed;

	private Node _player;
	private int _impactDamage = 0;

	private readonly HashSet<Pin> _hitPins = new();


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
		GetNode<Area2D>("Hitbox").AreaEntered += OnBumperAreaEntered;
		_rollAudio = GetNode<AudioStreamPlayer2D>("RollSound");
		_thudAudio = GetNode<AudioStreamPlayer2D>("ThudSound");

		_player = GetNodeOrNull<Node>(PlayerPath);
		if (_player == null)
			GD.PushWarning($"Ball: Player not found at {PlayerPath}");

		EnsureGameManager();
		EnsureMeter();
	}

	public void Initialize(Vector2 startPos)
	{
		GlobalPosition = startPos;
		_startX = startPos.X;
		_currentOffset = 0f;
		_currState = BallState.Aiming;

		_hitPins.Clear();
		_afterImagesSpawned = false;

		IsSplitBall = false;
		SplitScheduled = false;
		_splitUsed = false;
		_splitInputArmed = true;
		_sizeMult = 1f;
		_bumpersHitThisBall.Clear();
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
				HandleSplitInput();
				UpdateScale();
				if (!_ballSprite.IsPlaying()) {_ballSprite.Play();}
				break;
			case BallState.Gutter:
				UpdateScale();
				if (!_ballSprite.IsPlaying()) {_ballSprite.Play();}
				break;
		}
	}
	// End State Handling //

	// Player Stat Handling //
	private float GetPlayerSpeed()
	{
		if (_player == null) return 0f;
		return (float)(double)_player.Call("get_speed_value");
	}

	private float GetPlayerImpact()
	{
		if (_player == null) return 0f;
		return (float)(double)_player.Call("get_impact_value");
	}

	private int GetPlayerKineticImpactMult()
	{
		if (_player == null) return 1;
		return 1 + (int)(long)_player.Call("get_kinetic_impact_mult");
	}

	private float GetPlayerCritChance()
	{
		if (_player == null) return 0.01f;
		return (float)(double)_player.Call("get_crit_chance");
	}

	private void EnsureGameManager()
	{
		if (_gameManager != null) return;

		_gameManager = GetTree().Root.FindChild("GameManager", recursive: true, owned: false) as GameManager
			?? GetTree().Root.FindChild("GameManager", recursive: true, owned: true) as GameManager;

		if (_gameManager == null)
			GD.PushError("Ball: GameManager not found. Make sure the node is named exactly 'GameManager' in the running scene.");
	}
	// End Player Stat Handling //

	// Bumper Methods //

	private void OnBumperAreaEntered(Area2D area)
	{
		if (!area.IsInGroup("Bumpers"))
			return;

		if (!_bumpersHitThisBall.Add(area))
			return;

		HitBumper(area);
	}
	private void CheckForBumpers()
	{
		if (_currState != BallState.Rolling)
			return;

		Area2D hitbox = GetNode<Area2D>("Hitbox");
		var areas = hitbox.GetOverlappingAreas();

		foreach (Area2D area in areas)
		{
			if (!area.IsInGroup("Bumpers"))
				continue;

			if (!_bumpersHitThisBall.Add(area))
				continue;

			HitBumper(area);
		}
	}
	// End Bumper Methods //

	// Ball PowerUps //

	private int GetPlayerSplitCount()
	{
		if (_player == null)
			return 0;

		if (!_player.HasMethod("get_split_count"))
			return 0;

		return Math.Max(0, (int)(long)_player.Call("get_split_count"));
	}

	private bool SplitActionJustPressed()
	{
		return Input.IsActionJustPressed("ball_aim_stop") || Input.IsActionJustPressed("power_meter_stop");
	}

	private bool SplitActionHeld()
	{
		return Input.IsActionPressed("ball_aim_stop") || Input.IsActionPressed("power_meter_stop");
	}

	private void HandleSplitInput()
	{
		if (_currState != BallState.Rolling)
			return;

		if (IsAfterImage)
			return;

		if (IsSplitBall)
			return;

		if (_splitUsed)
			return;

		if (!_splitInputArmed)
		{
			if (!SplitActionHeld())
				_splitInputArmed = true;

			return;
		}

		if (!SplitActionJustPressed())
			return;

		int splitCount = GetPlayerSplitCount();

		if (splitCount <= 0)
			return;

		EnsureGameManager();

		if (_gameManager == null)
			return;

		_gameManager.TriggerSplitChain(this);
		GetViewport().SetInputAsHandled();
	}

	public void PerformSplit()
	{
		if (_splitUsed)
			return;

		if (_currState != BallState.Rolling)
			return;

		if (IsSplitBall)
			return;

		int splitCount = GetPlayerSplitCount();

		if (splitCount <= 0)
			return;

		_splitUsed = true;
		SplitScheduled = true;

		EnsureGameManager();

		if (_gameManager == null)
			return;

		GD.Print($"[Split] {Name} splitting with count={splitCount}");

		_gameManager.SpawnSplitBallsFrom(this, splitCount);

		CallDeferred(Node.MethodName.QueueFree);
	}

	public void InitializeSplitCloneFrom(Ball source, Vector2 startPos, float splitScaleMult, float addedPowerVal)
	{
		Initialize(startPos);

		IsSplitBall = true;
		IsAfterImage = source.IsAfterImage;
		AfterImageIndex = source.AfterImageIndex;

		_splitUsed = true;
		SplitScheduled = true;
		_afterImagesSpawned = true;

		_sizeMult = splitScaleMult;

		RollSpeed = source.RollSpeed;
		BallDamage = source.BallDamage;
		IsSweet = source.IsSweet;
		Meter = source.Meter;

		_impactDamage = source._impactDamage;
		_powerVal = Mathf.Clamp(source._powerVal + addedPowerVal, -450f, 450f);

		_currState = BallState.Rolling;

		GD.Print($"[Split] clone made. scale={splitScaleMult}, addedPowerVal={addedPowerVal}, finalPowerVal={_powerVal}");
	}

	private int GetPlayerAfterImages()
	{
		if (_player == null) return 0;
		return (int)(long)_player.Call("get_after_image_count"); // GDScript int comes through as long
	}

	public void ApplyRubberBounce(float multiplier)
	{
		if (_currState != BallState.Rolling)
			return;

		CallDeferred(nameof(ApplyRubberBounceDeferred), multiplier);
	}

	public void ApplyRubberBounceDeferred(float multiplier)
	{
		if (_currState != BallState.Rolling)
			return;

		float oldPowerVal = _powerVal;

		_powerVal *= multiplier;

		// Safety cap so sweet bumper 300 * 1.5 = 450 max.
		_powerVal = Mathf.Clamp(_powerVal, -450f, 450f);

		GD.Print($"[Ball] Rubber bounce applied. oldPowerVal={oldPowerVal}, newPowerVal={_powerVal}, mult={multiplier}");
	}

	private void HitOtherBall(Area2D area)
	{
		if (_currState != BallState.Rolling)
			return;

		if (_ballBounceCooldown)
			return;

		Node parent = area.GetParent();

		if (parent is not Ball otherBall)
			return;

		if (otherBall == this)
			return;

		if (otherBall._currState != BallState.Rolling)
			return;

		_ballBounceCooldown = true;
		otherBall._ballBounceCooldown = true;

		float diffX = GlobalPosition.X - otherBall.GlobalPosition.X;

		if (Mathf.Abs(diffX) < 0.1f)
			diffX = GD.Randf() < 0.5f ? -1f : 1f;

		float dir = Mathf.Sign(diffX);

		//Immediately separate them so they do not stay overlapped.
		float separation = 8f;
		GlobalPosition += new Vector2(dir * separation, 0);
		otherBall.GlobalPosition -= new Vector2(dir * separation, 0);

		//Give both balls a short-lived sideways push.
		float repelStrength = 120f;
		_ballRepelVelocityX += dir * repelStrength;
		otherBall._ballRepelVelocityX -= dir * repelStrength;

		_powerVal += dir * 180f;
		otherBall._powerVal -= dir * 180f;

		_powerVal = Mathf.Clamp(_powerVal, -450f, 450f);
		otherBall._powerVal = Mathf.Clamp(otherBall._powerVal, -450f, 450f);

		GD.Print($"[BallBounce] {Name} separated from {otherBall.Name}");

		GetTree().CreateTimer(0.12f).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this))
				_ballBounceCooldown = false;

			if (GodotObject.IsInstanceValid(otherBall))
				otherBall._ballBounceCooldown = false;
		};
	}
	// End Ball PowerUps //

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
		if (IsAfterImage) return;
		EnsureMeter();
		EnsureGameManager();
		if (_currentOffset >= _laneWidthLimit) _aimingLeft = true;
		else if (_currentOffset <= -_laneWidthLimit) _aimingLeft = false;

		float dir = _aimingLeft ? -1 : 1;
		_currentOffset += dir * AimSpeed * (float)delta;

		Position = new Vector2(_startX + _currentOffset, Position.Y);

		if (Input.IsActionJustPressed("ball_aim_stop") && !_gameManager.InputLock)
		{
			_currState = BallState.Powering;
			Meter.ShowMeter(); 

			GetViewport().SetInputAsHandled();
		}
	}

	public void FinalizePower(float speed, float rawX, bool sweet)
	{
		GD.Print($"Zone Speed: {speed}, Raw X: {rawX}");
		RollSpeed = speed + GetPlayerSpeed();
		BallDamage = ((int) speed) + 20;

		_impactDamage = Mathf.RoundToInt(GetPlayerImpact());
		_powerVal = rawX;
		_thudAudio.Play();
		_currState = BallState.Rolling;
		IsSweet = sweet;

		_splitInputArmed = false;

		GD.Print(
			$"ROLL -> Speed: {RollSpeed:0.00} | BaseDmg: {BallDamage} | Impact: {_impactDamage} | TotalPerHit(no crit): {BallDamage + _impactDamage}"
		);

		_currState = BallState.Rolling;
		IsSweet = sweet;

		EnsureGameManager();
		if (!IsAfterImage && !_afterImagesSpawned && _gameManager != null)
		{
			_afterImagesSpawned = true;

			int count = GetPlayerAfterImages();
			if (count > 0)
				_gameManager.SpawnAfterImages(GlobalPosition, speed, rawX, sweet, count);
		}
	}

	private void EnsureMeter()
	{
		if (Meter != null) return;

		Meter = GetTree().Root.FindChild("PowerMeter", recursive: true, owned: false) as PowerMeter
			?? GetTree().Root.FindChild("PowerMeter", recursive: true, owned: true) as PowerMeter;

		if (Meter == null)
				GD.PushError("Ball: PowerMeter not found. Make sure the node is named exactly 'PowerMeter' in the running scene.");
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
		Scale = new Vector2(t * _sizeMult, t * _sizeMult);
	}

	public void StartBounce()
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
				_powerVal = IsSweet ? 300 : 200f;
				BallDamage = (int)Math.Ceiling(BallDamage * 1.25);
			}
			else if (bumperName.Contains("Right"))
			{
				_powerVal = IsSweet ? -300 : -200f;
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
			if (area is not Pin pin)
				continue;

			// per-ball hit gate (fixes afterimage balls skipping pins)
			if (!_hitPins.Add(pin))
				continue;

			// roll crit every time we hit a pin
			float critChance = Mathf.Clamp(GetPlayerCritChance(), 0f, 1f);
			bool isCrit = GD.Randf() < critChance;

			int baseDamage = BallDamage;
			if (isCrit)
				baseDamage *= 2;

			// impact is added after and is never doubled
			int damageToDeal = baseDamage + _impactDamage;

			int kCount = GetPlayerKineticImpactMult();
			int detonateMult = 1 + kCount;

			if (!IsAfterImage && kCount > 0)
			{
				// arm (mark) the pin
				pin._kineticFlag = true;
			}

			int finalDamage = damageToDeal;

			if (IsAfterImage && kCount > 0 && pin._kineticFlag)
			{
				// detonate (big hit)
				finalDamage *= detonateMult;
			}

			// pass 0 so Pin.cs doesn't apply its own kinetic multiplier on top
			pin.TakeDamage(finalDamage, IsSweet, 0);

			BallDamage = Math.Max(10, BallDamage / 3);

			if (pin.Alive)
			{
				StartBounce();
				break;
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
			CheckForBumpers();

			if (_currState != BallState.Rolling) return;

			float hookStrength = 0.8f;
			_powerVal = Mathf.MoveToward(_powerVal, Mathf.Clamp(_powerVal, -20, 20), (float)delta * 10f);
			float horizontalDrift = _powerVal * hookStrength;

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
