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
	private bool _pendingHide = false;

	public override void _Ready()
	{
		_audio = GetNode<AudioStreamPlayer2D>("Sounds");
		_sprite = GetNode<AnimatedSprite2D>("BumperSprite");

		_bumpLeft = GetNode<CollisionShape2D>("BumperLeft/BumperLeftColShape");
		_bumpRight = GetNode<CollisionShape2D>("BumperRight/BumperRightColShape");

		_bumpLeft.Disabled = true;
		_bumpRight.Disabled = true;
		Visible = false;

		_sprite.AnimationFinished += OnSpriteAnimationFinished;
		GetNode<Area2D>("BumperRight").AreaEntered += Hit;
		GetNode<Area2D>("BumperLeft").AreaEntered += Hit;
		GD.Print($"[Bumpers] Ready at path: {GetPath()}  instance_id={GetInstanceId()}");
	}

	public void ApplyForNewFrame(int bumperCapacity)
	{
		GD.Print($"[Bumpers] NewFrame cap={bumperCapacity}");
		hitsAllowed = Math.Max(0, bumperCapacity);
		hitCount = 0;

		if (hitsAllowed > 0)
			ShowBumpers();
		else
			HideBumpers();
	}

	public void ApplyCapacityMidFrame(int bumperCapacity)
	{
		hitsAllowed = Math.Max(0, bumperCapacity);

		if (hitsAllowed > 0 && hitCount < hitsAllowed)
			ShowBumpers(resetHitCount: false);
		else
			HideBumpers();
	}

	public void UpdateHitCount()
	{
		hitCount += 1;

		if (hitsAllowed <= 0 || hitCount >= hitsAllowed)
			HideBumpers();
	}

	public void ShowBumpers(bool resetHitCount = true)
	{
		_pendingHide = false;
		Visible = true;

		if (resetHitCount)
			hitCount = 0;

		_bumpLeft.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_bumpRight.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);

		_sprite.Play(); // play forward
	}

	public void HideBumpers()
	{
		_bumpLeft.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_bumpRight.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		_pendingHide = true;
		if (Visible)
			_sprite.PlayBackwards();
		else
			_pendingHide = false;
	}

	private void OnSpriteAnimationFinished()
	{
		if (_pendingHide)
		{
			_pendingHide = false;
			Visible = false;
		}
	}

	public void Hit(Area2D area)
	{
		if (hitsAllowed <= 0 || hitCount >= hitsAllowed)
			return;

		UpdateHitCount();

		if (Sounds.Count > 0)
		{
			_audio.Stream = Sounds[0];
			_audio.Play();
		}
	}

	public override void _Process(double delta)
	{
	}
}
