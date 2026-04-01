using Godot;
using System;

public partial class Ball : CharacterBody2D
{

	public enum BallState {Aiming, Powering, Rolling, Finished}
	private BallState _currState = BallState.Aiming;

	[Export] public float AimSpeed = 150f;
	[Export] public float RollSpeed = 60.0f;
	[Export] public PowerMeter Meter;

	private bool _aimingLeft = false;
	private float _laneWidthLimit = 90.0f;
	private float _startX; // The center of the lane
	private float _currentOffset = 0f; // How far moved from center
	private float _powerVal = 0;

	public override void _Ready()
	{
		_startX = Position.X;
		_currState = BallState.Aiming;
	}

	public void Initialize(Vector2 startPos)
	{
		GlobalPosition = startPos;
		_startX = startPos.X;
		_currentOffset = 0f;
		_currState = BallState.Aiming;
	}

	private void HandleAiming(double delta)
{
	if (_currentOffset >= _laneWidthLimit) _aimingLeft = true;
	else if (_currentOffset <= -_laneWidthLimit) _aimingLeft = false;

	float dir = _aimingLeft ? -1 : 1;
	_currentOffset += dir * AimSpeed * (float)delta;

	Position = new Vector2(_startX + _currentOffset, Position.Y);

	if (Input.IsActionJustPressed("ball_aim_stop"))
	{
		_currState = BallState.Powering;
		Meter.ShowMeter(); 

		GetViewport().SetInputAsHandled();
	}
}
	public void FinalizePower(float speed, float rawX)
	{
		GD.Print($"Zone Speed: {speed}, Raw X: {rawX}");
		RollSpeed = speed;
		_powerVal = rawX;
		_currState = BallState.Rolling;
	}

	private void UpdateScale()
	{
		float startY = 169; // Bottom of lane
		float endY = 81;   // Top of lane (the pins)
		float minScale = 0.5f;
		float maxScale = 1.0f;

		// Remap the current Y position to a scale value
		float t = Mathf.Remap(GlobalPosition.Y, endY, startY, minScale, maxScale);
		Scale = new Vector2(t, t);
	}

	public void CheckForHits()
	{
		Area2D hitbox = GetNode<Area2D>("Hitbox");

		var bodies = hitbox.GetOverlappingAreas();

		foreach (Area2D area in bodies)
		{
			if (area is Pin pin)
			{
				if (!pin.GetHitThisRound())
				{
					pin.TakeDamage(10000);
					pin.SetHitThisRound(true);
				} 
				
			}
		}
	}

	public void FadeOutAndRemove()
	{
		Tween tween = CreateTween();

		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.25f);

		tween.Finished += () =>
		{
			QueueFree();
			_currState = BallState.Aiming;
		};
	}
	public override void _PhysicsProcess(double delta)
	{
		if (_currState != BallState.Rolling) return;

		CheckForHits();

		// Hook Strength: Adjusting val, but 0.8 feels good for now.
		float hookStrength = 0.8f;
		float horizontalDrift = _powerVal / 2 * hookStrength;

		// Apply drift to X, and speed to Y
		Velocity = new Vector2(horizontalDrift, -RollSpeed);

		MoveAndSlide();

		// Check if we reached the end of the lane
		if (GlobalPosition.Y <= 81.0f)
		{
			Velocity = Vector2.Zero;
			SetPhysicsProcess(false); 
			FadeOutAndRemove();
		}
	}
	public override void _Process(double delta)
	{

		switch (_currState)
		{
			case BallState.Aiming:
				HandleAiming(delta);
				break;
			case BallState.Powering:
				// Do nothing here we are waiting for the Meter to finish
				break;
			case BallState.Rolling:
				UpdateScale(); // Only scale while rolling
				break;
		}
	}

}
