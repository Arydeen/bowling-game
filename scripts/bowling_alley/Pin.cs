using Godot;
using Godot.Collections;
using System;
using System.ComponentModel.Design;

public partial class Pin : Area2D
{

	public enum PinType { R1, R2, R3, R4 }

	[Export] public PinType Type;
	[Export] public Array<SpriteFrames> AnimationLibrary;
	[Export] public Array<AudioStream> DeathSounds;
	[Export] public CollisionShape2D Hitbox;

	[Export] public double MaxHealth = 100;
	[Export] public bool Alive = true;

	public bool _hitThisShot {get; set;} = false;

	private double _currentHealth;
	
	private ProgressBar _healthBar;
	private AudioStreamPlayer2D _audio;
	private AudioStreamPlayer2D _shakeAudio;
	private AnimatedSprite2D _sprite;
	private Node2D _spritePivot;
	private GameManager _gameManager;

	private Tween _kineticTween;
	private GpuParticles2D _kineticExplosion;

	public bool _kineticFlag { get; set; } = false; 


	// Initialization Methods --------------------------------------------------------------------------------------------------- //
	public override void _Ready()
	{
		// Fetch Hitbox
		Hitbox = GetNode<CollisionShape2D>("PinHitbox");

		// Fetch Game Manager
		_gameManager = GetNode<GameManager>("../../GameManager");

		// Texture Handling
		_sprite = GetNode<AnimatedSprite2D>("SpritePivot/PinSprite");
		_spritePivot = GetNode<Node2D>("SpritePivot");

		// Audio Handling
		_audio = GetNode<AudioStreamPlayer2D>("DeathSound");
		_shakeAudio = GetNode<AudioStreamPlayer2D>("ShakeSound");

		int index = (int)Type;

		if (AnimationLibrary != null && index < AnimationLibrary.Count)
		{
			_sprite.SpriteFrames = AnimationLibrary[index];
		}
		
		// Health Bar Handling
		_currentHealth = MaxHealth;
		_healthBar = GetNode<ProgressBar>("ProgressBar");
		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = _currentHealth;

		// Kinetic Ball Handling
		_kineticTween = CreateTween();
		_kineticExplosion = GetNode<GpuParticles2D>("KineticExplosionParticles");

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

		GetTree().CreateTimer(0.15f).Timeout += () => CalculateShake(amount, sweet);
	}

	private void ShakeDamage(Pin pin, int amount, bool sweet) 
	{

		if (!pin.Alive) return;

		int shakeDamage = amount / 2;

		pin.SetHealth(pin.GetHealth() - shakeDamage);
		pin.SetHealthBar(pin.GetHealth());

		if (pin.GetHealth() <= 0)
		{
			pin.Die();
		} else
		{
			pin.DamageAnimation(sweet, true);
			HandleKinetic(pin);
		}
	}

	private void CalculateShake(int damage, bool sweet)
	{
		Area2D shakebox = GetNode<Area2D>("Shakebox");
		var areas = shakebox.GetOverlappingAreas();

		foreach(Area2D area in areas)
		{
			if (area is Pin pin && pin != this)
			{
				ShakeDamage(pin, damage, sweet);
			}
		}
	}

	public void Die()
	{
		if (Alive)
		{
			Alive = false;

			_gameManager.AddScore(1, shot:true);

			if (DeathSounds != null && DeathSounds.Count > 0)
			{
				int randomIndex = (int)(GD.Randi() % DeathSounds.Count);
				_audio.Stream = DeathSounds[randomIndex];
			}

			if (_kineticFlag) {PlayKineticParticles();}
			_audio.Play();
			_sprite.Play();
			FadeOut();
		}
	}

	public void DieNoScore()
	{
		if (Alive)
		{
			Alive = false;

			if (DeathSounds != null && DeathSounds.Count > 0)
			{
				int randomIndex = (int)(GD.Randi() % DeathSounds.Count);
				_audio.Stream = DeathSounds[randomIndex];
			}

			_audio.Play();
			_sprite.Play();
			FadeOut();
		}
	}

	// End Damage Methods --------------------------------------------------------------------------------------------------- //

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

	public void FadeIn()
	{
		Tween tween = CreateTween().SetParallel(false);

		_sprite.PlayBackwards();
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.7f);
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
