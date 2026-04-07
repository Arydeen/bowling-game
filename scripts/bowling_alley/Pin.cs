using Godot;
using Godot.Collections;
using System;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;

public partial class Pin : Area2D
{

	public enum PinType { R1, R2, R3, R4 }

	[Export] public PinType Type;
	[Export] public Array<SpriteFrames> AnimationLibrary;
	[Export] public Array<AudioStream> DeathSounds;

	[Export] public int MaxHealth = 100;

	private int _currentHealth;
	private bool _hitThisRound = false;

	private ProgressBar _healthBar;
	private AudioStreamPlayer2D _audio;
	private AnimatedSprite2D _sprite;

	public void SetHitThisRound(bool val) { _hitThisRound = val;}

	public bool GetHitThisRound() {return _hitThisRound;}
	


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		// Texture Handling
		_sprite = GetNode<AnimatedSprite2D>("PinSprite");
		// Audio Handling
		_audio = GetNode<AudioStreamPlayer2D>("DeathSound");

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

	private void DamageAnimation()
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

	public void TakeDamage(int amount)
	{
		DamageAnimation();
		_currentHealth -= amount;
		_healthBar.Value = _currentHealth;

		if (_currentHealth <= 0)
		{
			Die();
		}

	}

	public void Die()
	{
		

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
