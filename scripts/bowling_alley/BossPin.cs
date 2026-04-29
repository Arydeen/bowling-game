using Godot;
using System;
using Godot.Collections;

public partial class BossPin : Area2D
{


	public enum BossState {shield, open, cracked, hidden}

	[Export] public BossState state;
	[Export] public Array<SpriteFrames> AnimationLibrary;
	[Export] public double MaxHealth = 500;

	public bool _hitThisShot {get; set;} = false;
	public bool Alive = false;
	public bool _kineticFlag { get; set; } = false; 

	private double _currentHealth;
	private int _activePinions = 0;
	private ProgressBar _healthBar;
	private AnimatedSprite2D _sprite;
	private Sprite2D _shield;
	private Tween _shieldTween;
	private Node2D _spritePivot;
	private GameManager _gameManager;
	private AudioStreamPlayer2D _audio;
	private AudioStreamPlayer2D _shakeAudio;
	private AnimatedSprite2D _lanesSprite;

	// Pinions
	private Pinion _pinion1;
	private Pinion _pinion2;

	// Kinetic ball vars
	private Tween _kineticTween;
	private GpuParticles2D _kineticExplosion;
	
	// Initialization Methods --------------------------------------------------------------------------------------------------- //
	public override void _Ready()
	{
		// Fetch Game Manager
		_gameManager = GetNode<GameManager>("../../GameManager");

		// Texture Handling
		_sprite = GetNode<AnimatedSprite2D>("SpritePivot/BossSprite");
		_spritePivot = GetNode<Node2D>("SpritePivot");
		_lanesSprite = GetNode<AnimatedSprite2D>("../../Lanes");

		_shield = GetNode<Sprite2D>("SpritePivot/Shield");
		_shield.Modulate = new Color(1, 1, 1, 0);

		// Audio Handling
		_audio = GetNode<AudioStreamPlayer2D>("SoundEffects");
		_shakeAudio = GetNode<AudioStreamPlayer2D>("ShakeSound");
		
		// Health Bar Handling
		_currentHealth = MaxHealth;
		_healthBar = GetNode<ProgressBar>("ProgressBar");
		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = _currentHealth;

		// Kinetic Ball Handling
		_kineticTween = CreateTween();
		_kineticExplosion = GetNode<GpuParticles2D>("KineticExplosionParticles");

		// Pinion Handling
		_pinion1 = GetNode<Pinion>("Pinion1");
		_pinion2 = GetNode<Pinion>("Pinion2");
		_pinion1.PinionDied += OnPinionDied;
		_pinion2.PinionDied += OnPinionDied;
		_pinion2.ShieldsUp += BossShieldUp;

		// Start Boss Hidden
		state = BossState.hidden;
		Modulate = new Color(1, 1, 1, 0);
	}

	
	public override void _Process(double delta)
	{
		if (_kineticFlag && !_kineticTween.IsRunning()) 
		{
			StartKineticEffect();
		} else if (!_kineticFlag && _kineticTween.IsRunning())
		{
			StopKineticEffect();
		}
	}
	// End Initializaion Methods --------------------------------------------------------------------------------------------------- //

	// Damage Methods --------------------------------------------------------------------------------------------------- //
	public void TakeDamage(int amount, bool sweet, int type)
	{

		if (type == 1 && _kineticFlag) { amount *= 2;}

		_currentHealth -= amount;
		_healthBar.Value = _currentHealth;

		if (_currentHealth <= 0)
		{
			Die();
		} else
		{
			DamageAnimation(sweet, false);
		}
	}

	public void Die()
	{
		if (Alive)
		{
			Alive = false;

			_gameManager.AddScore(1, shot:true);

			if (_kineticFlag) {PlayKineticParticles();}
			_audio.Play();
			_sprite.Play();
			FadeOut();
		}
	}

	// End Damage Methods --------------------------------------------------------------------------------------------------- //

	// Pinion Handling Methods --------------------------------------------------------------------------------------------------- //
	private void OnPinionDied()
	{
		_activePinions -= 1;
		if (_activePinions <= 0)
		{
			// remove Shield
		}
	}

	// End Pinion Handling Methods --------------------------------------------------------------------------------------------------- //

	// Boss Animation Methods --------------------------------------------------------------------------------------------------- //
	private void BossLaugh()
	{
		_sprite.Play("laugh");
	}

	private void BossEnterLaugh()
	{
		_sprite.Play("enter_laugh");
	}

	private void BossScared()
	{
		_sprite.Play("scared");
	}

	public void BossShieldUp()
	{
		state = BossState.shield;

		_shieldTween?.Kill(); 
	
		_shieldTween = CreateTween().SetLoops();
		_shieldTween.TweenProperty(_shield, "modulate:a", 1.0f, 0.8f);
		_shieldTween.TweenProperty(_shield, "modulate:a", 0.5f, 0.8f);

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

			tween.Finished += () => {
				Alive = true; 
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
		
		// Play a sound effect if you have one!
		GD.Print("Boss Pin Landed!");
	}

	
	// End Boss Animation Methods --------------------------------------------------------------------------------------------------- //
	
	// Base Animation Methods ------------------------------------------------------------------------------------------------------- //
	public void DamageAnimation(bool sweet, bool shake)
	{
		if (sweet) {PlayHeavyWobble();} else {PlayWobble();}
		if (_kineticFlag && !shake) {PlayKineticParticles();}
	}

	public void FadeOut()
	{
		Tween tween = CreateTween();

		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.7f);
	}

	public void MakeVisible()
	{
		Tween tween = CreateTween().SetParallel(false);
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0f);
	}

		// Heavy Wobble for sweet spot shake
	public void PlayHeavyWobble()
	{
		Tween tween = CreateTween().SetParallel(false);
		float intensity = 0.4f; // Starting tilt
		float duration = 0.1f;

		tween.TweenProperty(_spritePivot, "rotation", 0f, 0.15f);
		_shakeAudio.PitchScale = 1.25f;
		_shakeAudio.Play();

		for (int i = 0; i < 6; i++)
		{
			// Alternate directions: positive, negative, positive, negative
			float direction = (i % 2 == 0) ? 1 : -1;
			
			tween.TweenProperty(_spritePivot, "rotation", intensity * direction, duration)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			
			// Reduce intensity each time for a "settling" effect
			if (i % 2 == 0) { intensity *= 0.6f; }
		}

		// Final snap back to zero
		tween.TweenProperty(_spritePivot, "rotation", 0f, duration);
	}

	// Small wobble for normal
	private void PlayWobble()
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

	public void SetHealthBar(double amount)
	{
		_healthBar.Value = amount;
	}

	public double GetHealthBar()
	{
		return _healthBar.Value;
	}


}
