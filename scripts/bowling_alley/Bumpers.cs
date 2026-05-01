using Godot;
using Godot.Collections;
using System;

public partial class Bumpers : Node2D
{
	[Export] public Array<AudioStream> Sounds;
	[Export] public int hitCount = 0;
	[Export] public int hitsAllowed = 0;

	[Export] public float RubberBouncePerStack = 0.2f; // 20% per RubberBall
	[Export] public float RubberBounceMaxBonus = 3.0f; // max +50%

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
		GD.Print($"[Bumpers] Hit fired by area={area.Name}, path={area.GetPath()}");

		if (hitsAllowed <= 0 || hitCount >= hitsAllowed)
			return;

		ApplyRubberBallBounce(area);

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

	// Rubber Ball Methods //
	private int GetRubberBallCount()
	{
		Node stats = GetNodeOrNull<Node>("/root/Player");

		if (stats == null)
			return 0;

		if (stats.HasMethod("get_rubber_ball_count"))
			return (int)stats.Call("get_rubber_ball_count");

		return 0;
	}

	private Ball FindBall(Node node)
	{
		Node current = node;

		while (current != null)
		{
			if (current is Ball ball)
				return ball;

			current = current.GetParent();
		}

		return null;
	}

	private void ApplyRubberBallBounce(Area2D area)
	{
		int rubberCount = GetRubberBallCount();

		if (rubberCount <= 0)
		{
			GD.Print("[RubberBall] no rubber balls");
			return;
		}

		Ball ball = FindBall(area);

		if (ball == null)
		{
			GD.Print("[RubberBall] could not find Ball parent");
			return;
		}

		float bonusPercent = Mathf.Min(
			rubberCount * RubberBouncePerStack,
			RubberBounceMaxBonus
		);

		float bounceMult = 1f + bonusPercent;

		GD.Print($"[RubberBall] count={rubberCount}, bounceMult={bounceMult}");

		ball.ApplyRubberBounce(bounceMult);
	}
}
