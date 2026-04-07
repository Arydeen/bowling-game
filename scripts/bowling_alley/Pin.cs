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

	[Export] public int MaxHealth = 100;

	[Export] public bool Alive = true;

	private int _currentHealth;
	private bool _hitThisRound = false;
	private ProgressBar _healthBar;
	private AudioStreamPlayer2D _audio;
	private AudioStreamPlayer2D _shakeAudio;
	private AnimatedSprite2D _sprite;
	private Node2D _spritePivot;

	public void SetHitThisRound(bool val) { _hitThisRound = val;}

	public bool GetHitThisRound() {return _hitThisRound;}
	


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

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

	}
 
	public void DamageAnimation()
	{
		Tween tween = CreateTween();
		// Flash to red quickly
		tween.TweenProperty(_sprite, "modulate", new Color(1, 0.2f, 0.2f, 1), 0.1f);
		// Return to normal after 0.4s (for a total of 0.5s)
		tween.TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 1), 0.4f).SetDelay(0.1f);
	}

	public void FadeOut()
	{
		Tween tween = CreateTween();

		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.7f);
	}

	public void TakeDamage(int amount, bool sweet)
	{
		CalculateShake(amount, sweet);
		DamageAnimation();
		_currentHealth -= amount;
		_healthBar.Value = _currentHealth;

		if (_currentHealth <= 0)
		{
			Die();
		}

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

	public void SetHealth(int amount)
	{
		_currentHealth = amount;
	}

	public int GetHealth()
	{
		return _currentHealth;
	}

	public void SetHealthBar(int amount)
	{
		_healthBar.Value = amount;
	}

	public double GetHealthBar()
	{
		return _healthBar.Value;
	}

	private void ShakeDamage(Pin pin, int amount, bool sweet) 
	{
		if (sweet) {pin.PlayHeavyWobble();} else {pin.PlayWobble();}

		int shakeDamage = amount / 2;

		pin.SetHealth(pin.GetHealth() - shakeDamage);
		pin.SetHealthBar(pin.GetHealth());

		if (pin.GetHealth() <= 0)
		{
			pin.Die();
		}
	}

	public void Die()
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
		_audio.Finished += () => QueueFree();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
