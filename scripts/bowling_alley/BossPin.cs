using Godot;
using System;
using Godot.Collections;
using System.Collections.Generic;

public partial class BossPin : Area2D
{
	[Signal] public delegate void BossHealthChangedEventHandler(double newValue, double maxValue);
	[Signal] public delegate void BossKilledEventHandler();

	public enum BossState {shield, open, cracked, hidden}

	[Export] public BossState state;
	[Export] public Array<SpriteFrames> AnimationLibrary;
	[Export] public Array<AudioStream> Sounds;
	[Export] public double MaxHealth = 300;
	[Export] public int ActivePinions = 0;

	public bool _hitThisShot {get; set;} = false;
	public bool Alive = false;
	public bool _kineticFlag { get; set; } = false; 
	private readonly HashSet<Ball> _ballsHitThisShot = new();

	private double _currentHealth;
	private AnimatedSprite2D _sprite;
	private Sprite2D _shield;
	private Sprite2D _crack;
	private Sprite2D _screenShield;
	private Tween _shieldTween;
	private Tween _screenShieldTween;
	private Node2D _spritePivot;
	private GpuParticles2D _crackParticles;
	private GameManager _gameManager;
	private AudioStreamPlayer2D _audio;
	private AudioStreamPlayer2D _shakeAudio;
	private AnimatedSprite2D _lanesSprite;
	private CollisionShape2D _hitbox;

	// Pinions
	private Pinion _pinion1;
	private Pinion _pinion2;

	// Kinetic ball vars
	private Tween _kineticTween = null;
	private GpuParticles2D _kineticExplosion;
	
	// Initialization Methods --------------------------------------------------------------------------------------------------- //
	public override void _Ready()
	{
		// Fetch Game Manager
		_gameManager = GetNode<GameManager>("../../GameManager");

		// Texture Handling
		_sprite = GetNode<AnimatedSprite2D>("SpritePivot/BossSprite");
		_sprite.AnimationFinished += OnAnimationFinished;
		_spritePivot = GetNode<Node2D>("SpritePivot");
		_lanesSprite = GetNode<AnimatedSprite2D>("../../Lanes");

		_shield = GetNode<Sprite2D>("SpritePivot/Shield");
		_shield.Modulate = new Color(1, 1, 1, 0);
		_screenShield = GetNode<Sprite2D>("ScreenShield");
		_screenShield.Modulate = new Color(1, 1, 1, 0);

		_crack = GetNode<Sprite2D>("SpritePivot/Crack");
		_crack.Modulate = new Color(1, 1, 1, 0);
		_crackParticles = GetNode<GpuParticles2D>("CrackParticles");

		// Audio Handling
		_audio = GetNode<AudioStreamPlayer2D>("SoundEffects");
		_shakeAudio = GetNode<AudioStreamPlayer2D>("ShakeSound");
		
		// Health Bar Handling
		_currentHealth = MaxHealth;

		// Kinetic Ball Handling
		_kineticExplosion = GetNode<GpuParticles2D>("KineticExplosionParticles");

		_hitbox = GetNode<CollisionShape2D>("PinHitbox");
		_hitbox.Disabled = true;

		// Pinion Handling
		_pinion1 = GetNode<Pinion>("Pinion1");
		_pinion2 = GetNode<Pinion>("Pinion2");
		_pinion1.PinionDied += OnPinionDied;
		_pinion2.PinionDied += OnPinionDied;
		_pinion2.ShieldsUp += BossShieldUp;

		// Start Boss Hidden
		state = BossState.hidden;
		Modulate = new Color(1, 1, 1, 0);
		AreaEntered += TakeDamage;
	}

	
	public override void _Process(double delta)
	{
		if (_kineticFlag && (_kineticTween == null || !_kineticTween.IsRunning())) 
		{
			StartKineticEffect();
		} 
		else if (!_kineticFlag && _kineticTween != null && _kineticTween.IsRunning())
		{
			StopKineticEffect();
		}
	}
	// End Initializaion Methods --------------------------------------------------------------------------------------------------- //

	// Damage Methods --------------------------------------------------------------------------------------------------- //
	public void ApplyOnHits()
	{
		if (GlobalData.Instance.KineticBall) {_kineticFlag = true;}
	}

	public void TakeDamage(Area2D area)
	{
		if (area == null)
			return;

		if (area.GetParent() is not Ball ball)
			return;

		if (!_ballsHitThisShot.Add(ball))
			return;

		_hitThisShot = true;

		if (state == BossState.shield)
		{
			ball.StartBounce();
			BossLaugh();
			return;
		}

		int amount = ball.BallDamage;

		if (_kineticFlag)
			amount *= 2;

		_currentHealth -= amount;
		EmitSignal(SignalName.BossHealthChanged, _currentHealth, MaxHealth);

		if (_currentHealth <= 0)
		{
			Die();
		}
		else
		{
			DamageAnimation(ball.IsSweet, false);
			ball.StartBounce();
		}
	}

	public void Die()
	{
		if (Alive)
		{
			Alive = false;
			_hitbox.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);;
			state = BossState.hidden;
			EmitSignal(SignalName.BossKilled);

			if (_kineticFlag) {PlayKineticParticles();}
			_lanesSprite.PlayBackwards();
			_audio.Play();
			_sprite.Play();
			FadeOut();
		}
	}

	public void ResetShotHits()
	{
		_hitThisShot = false;
		_ballsHitThisShot.Clear();
	}

	// End Damage Methods --------------------------------------------------------------------------------------------------- //

	// Pinion Handling Methods --------------------------------------------------------------------------------------------------- //
	private void OnPinionDied()
	{
		ActivePinions -= 1;
		if (ActivePinions <= 0)
		{
			state = BossState.open;
			_shieldTween?.Kill(); 
			_shieldTween = CreateTween();
			_shieldTween.TweenProperty(_shield, "modulate:a", 0.0f, 0.5f);
			_screenShieldTween?.Kill(); 
			_screenShieldTween = CreateTween();
			_screenShieldTween.TweenProperty(_screenShield, "modulate:a", 0.0f, 0.5f);
			ApplyOnHits();
		}
	}

	// End Pinion Handling Methods --------------------------------------------------------------------------------------------------- //

	// Boss Animation Methods --------------------------------------------------------------------------------------------------- //
	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "laugh" || _sprite.Animation == "enter_laugh")
		{
			_sprite.Play("idle");
		}
	}

	private void BossLaugh()
	{
		_sprite.Play("laugh");
		_audio.Stream = Sounds[1];
		_audio.Play();
	}

	private void BossEnterLaugh()
	{
		_sprite.Play("enter_laugh");
		_audio.Stream = Sounds[1];
		_audio.Play();
	}

	private void BossScared()
	{
		_sprite.Play("scared");
	}

	public void BossShieldUp()
	{
		state = BossState.shield;
		ActivePinions = 2;

		_shieldTween?.Kill(); 
	
		_shieldTween = CreateTween().SetLoops();
		_shieldTween.TweenProperty(_shield, "modulate:a", 1.0f, 0.8f);
		_shieldTween.TweenProperty(_shield, "modulate:a", 0.5f, 0.8f);

		_screenShieldTween?.Kill(); 
	
		_screenShieldTween = CreateTween().SetLoops();
		_screenShieldTween.TweenProperty(_screenShield, "modulate:a", 1.0f, 0.8f);
		_screenShieldTween.TweenProperty(_screenShield, "modulate:a", 0.5f, 0.8f);

	}

	public void BossEnter()
	{
		if (state == BossState.hidden)
		{
			Vector2 targetPos = Position;

			Position = new Vector2(targetPos.X, targetPos.Y - 500);

			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);

			tween.TweenProperty(this, "position", targetPos, 0.8f);
				// .SetTrans(Tween.TransitionType.Expo);

			tween.TweenProperty(this, "modulate:a", 1.0f, 0.5f);

			tween.Chain().TweenCallback(Callable.From(TriggerImpact));

			_currentHealth = MaxHealth;

			tween.Finished += () => {
				Alive = true;
				_hitbox.Disabled = false;
				
				// Monitor Change
				GetNode<Monitor>("../../Monitor").TransitionToBoss();
				EmitSignal(SignalName.BossHealthChanged, _currentHealth, MaxHealth); 

				_lanesSprite.Play();
				GetTree().CreateTimer(0.5f).Timeout += () => {
					BossEnterLaugh();
					_pinion1.PinionEnter();
					GetTree().CreateTimer(0.2f).Timeout += _pinion2.PinionEnter;
				};
			};
		}
	}

	private void TriggerImpact()
	{
		// Find the camera in the scene and call Shake
		var camera = GetTree().Root.FindChild("Camera", true, false) as GameCamera;
		camera?.Shake(15.0f); // 15 is a decent 'heavy' shake for 320x180
	}

	
	// End Boss Animation Methods --------------------------------------------------------------------------------------------------- //
	
	// Base Animation Methods ------------------------------------------------------------------------------------------------------- //
	public void DamageAnimation(bool sweet, bool shake)
	{
		if (sweet) {PlayHeavyWobble();} else {PlayWobble();}
		if (_kineticFlag && !shake) {PlayKineticParticles();  _kineticFlag = false; }
		if (_currentHealth <= MaxHealth / 2)
		{
			state = BossState.cracked;
			_crackParticles.Emitting = true;
			_crackParticles.Restart();
			_crack.Modulate = new Color(1, 1, 1, 1);
			BossScared();
		}
	}

	public void FadeOut()
	{
		Tween tween = CreateTween();

		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.7f);
		tween.Finished += () => {
			_crack.Modulate = new Color(1, 1, 1, 0);
			_sprite.PlayBackwards("enter_laugh");
		};
	}

	public void MakeVisible()
	{
		Tween tween = CreateTween().SetParallel(false);
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0f);
	}

	private void PlayHeavyWobble()
	{
		Tween tween = CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		float wobbleAngle = 0.2f; 
		float duration = 0.15f;

		tween.TweenProperty(_spritePivot, "rotation", 0f, duration);
		_shakeAudio.PitchScale = 1f;
		_shakeAudio.Play();
		tween.TweenProperty(_spritePivot, "rotation", wobbleAngle, duration);
		tween.TweenProperty(_spritePivot, "rotation", -wobbleAngle, duration);
		tween.TweenProperty(_spritePivot, "rotation", 0f, duration);
	}

	private void PlayWobble()
	{
		Tween tween = CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		float wobbleAngle = 0.05f; 
		float duration = 0.15f;

		tween.TweenProperty(_spritePivot, "rotation", 0f, duration);
		_shakeAudio.PitchScale = 1f;
		_shakeAudio.Play();
		tween.TweenProperty(_spritePivot, "rotation", wobbleAngle, duration);
		tween.TweenProperty(_spritePivot, "rotation", -wobbleAngle, duration);
		tween.TweenProperty(_spritePivot, "rotation", 0f, duration);
	}

	// End Base Animation Methods --------------------------------------------------------------------------------------------------- //
	
	// Kinetic Power Up Methods --------------------------------------------------------------------------------------------------- //
	private static void HandleKinetic(Pin pin)
	{
		if (GlobalData.Instance.KineticBall)
		{
			pin._kineticFlag = true;
		}
	}

 	private void PlayKineticParticles()
	{
		_kineticExplosion.Emitting = true;
		_kineticExplosion.Restart();
	}

	public void StartKineticEffect()
	{
		Color kineticColor = new Color(0.9f, 0.5f, 0.9f);
		Color baseColor = new Color(1f, 1f, 1f);

		_kineticTween = CreateTween().SetLoops();

		_kineticTween.TweenProperty(_sprite, "modulate", kineticColor, 0.75f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		_kineticTween.TweenProperty(_sprite, "modulate", baseColor, 0.75f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
	}

	public void StopKineticEffect()
	{
		Color baseColor = new Color(1f, 1f, 1f);

		if (_kineticTween != null && _kineticTween.IsRunning())
		{
			_kineticTween.Kill();
		}

		Tween resetTween = CreateTween();
		resetTween.TweenProperty(_sprite, "modulate", baseColor, 0.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}
	
	// End Kinetic Power Up Methods ---------------------------------------------------------------------------------------------------//

	public void SetHealth(double amount)
	{
		_currentHealth = amount;
	}

	public double GetHealth()
	{
		return _currentHealth;
	}
}
