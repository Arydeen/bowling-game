using Godot;
using Godot.Collections;
using System;

public partial class Bumpers : Node2D
{
	[Export] public Array<AudioStream> Sounds;
	[Export] public int hitCount = 0;
	[Export] public int hitsAllowed = 0;

	private AudioStreamPlayer2D _audio;
	private AnimatedSprite2D _sprite;
	private CollisionShape2D _bumpLeft;
	private CollisionShape2D _bumpRight;

	public override void _Ready()
	{
		_audio = GetNode<AudioStreamPlayer2D>("Sounds");
		_sprite = GetNode<AnimatedSprite2D>("BumperSprite");

		_bumpLeft = GetNode<CollisionShape2D>("BumperLeft/BumperLeftColShape");
		_bumpRight = GetNode<CollisionShape2D>("BumperRight/BumperRightColShape");

		_bumpLeft.Disabled = true;
		_bumpRight.Disabled = true;

		GetNode<Area2D>("BumperRight").AreaEntered += Hit;
		GetNode<Area2D>("BumperLeft").AreaEntered += Hit;
	}

	public void UpdateHitCount()
	{
		hitCount += 1;
		if (hitCount >= hitsAllowed)
		{
			HideBumpers();
		}
	}

	public void ShowBumpers()
	{
		Visible = true;
		_sprite.Play();
		hitCount = 0;
		_bumpLeft.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_bumpRight.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
	}

	public void HideBumpers()
	{
		_bumpLeft.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_bumpRight.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_sprite.PlayBackwards();
		if (!_sprite.IsPlaying()) {Visible = false;}
	}

	public void Hit(Area2D area)
	{
		UpdateHitCount();
		_audio.Stream = Sounds[0];
		_audio.Play();
	}

	public override void _Process(double delta)
	{
	}
}
