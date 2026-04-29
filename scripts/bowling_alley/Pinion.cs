using Godot;
using System;
using Godot.Collections;
using System.Runtime.Serialization;

public partial class Pinion : Area2D
{
	[Signal] public delegate void PinionDiedEventHandler();
	[Signal] public delegate void ShieldsUpEventHandler();

	[Export] public Array<SpriteFrames> AnimationLibrary;

	public bool _hitThisShot {get; set;} = false;
	public bool Alive = false;

	private AnimatedSprite2D _sprite;
	private Sprite2D _shield;
	private Tween _shieldTween;
	private Node2D _spritePivot;
	private AudioStreamPlayer2D _audio;

	// Initialization Methods --------------------------------------------------------------------------------------------------- //
	public override void _Ready()
	{

		// Texture Handling
		_sprite = GetNode<AnimatedSprite2D>("SpritePivot/PinSprite");
		_spritePivot = GetNode<Node2D>("SpritePivot");

		_shield = GetNode<Sprite2D>("SpritePivot/Shield");
		_shield.Modulate = new Color(1, 1, 1, 0);

		// Audio Handling
		_audio = GetNode<AudioStreamPlayer2D>("SoundEffects");

		AreaEntered += Die;
	}

	
	public override void _Process(double delta)
	{
		
	}
	// End Initializaion Methods --------------------------------------------------------------------------------------------------- //

	// Damage Methods --------------------------------------------------------------------------------------------------- //
	public void Die(Area2D area)
	{
		// Check if overlapping area is the ball
		if (!area.Owner.IsInGroup("Ball")) return;

		if (Alive && !_hitThisShot)
		{
			_hitThisShot = true;
			Alive = false;
			_shieldTween?.Kill();
			_shield.Modulate = new Color(1, 1, 1, 0);

			EmitSignal(SignalName.PinionDied);

			_audio.Play();
			// _sprite.Play();
			FadeOut();
		}
	}

	// End Damage Methods --------------------------------------------------------------------------------------------------- //

	// Pinion Animation Methods --------------------------------------------------------------------------------------------------- //
	private void PinionShieldUp()
	{
		if (!Alive) return;

		EmitSignal(SignalName.ShieldsUp);
		_shieldTween?.Kill(); 
	
		_shieldTween = CreateTween().SetLoops();
		_shieldTween.TweenProperty(_shield, "modulate:a", 1.0f, 0.4f);
		_shieldTween.TweenProperty(_shield, "modulate:a", 0.5f, 0.4f);
	}
	public void PinionEnter()
	{
		Alive = true;
		_hitThisShot = false;
		MakeVisible();
		_sprite.Play("pinion");
		GetTree().CreateTimer(0.6f).Timeout += PlayWobble;
	}
	
	// End Pinion Animation Methods --------------------------------------------------------------------------------------------------- //
	
	// Base Animation Methods ------------------------------------------------------------------------------------------------------- //
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

	// Small wobble for normal
	private void PlayWobble()
	{
		Tween tween = CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		float wobbleAngle = 0.2f; 
		float duration = 0.15f;

		tween.TweenProperty(_spritePivot, "rotation", 0f, duration);
		tween.TweenProperty(_spritePivot, "rotation", wobbleAngle, duration);
		tween.TweenProperty(_spritePivot, "rotation", -wobbleAngle, duration);
		tween.TweenProperty(_spritePivot, "rotation", 0f, duration);
		tween.Finished += PinionShieldUp;
	}
	// End Base Animation Methods --------------------------------------------------------------------------------------------------- //
}
