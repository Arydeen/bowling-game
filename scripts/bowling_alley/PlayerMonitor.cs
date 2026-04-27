using Godot;
using System;

public partial class PlayerMonitor : Node2D
{
	
	// Private Sprite Vars
	private AnimatedSprite2D _playerSprite;
	private AnimatedSprite2D _backgroundSprite;
	private PointLight2D _monitorlight;
	private Vector2 _playerOrigin;

	// Random value for animation timers
	private Random _animRand = new Random();

	public override void _Ready()
	{
		
		_playerSprite = GetNode<AnimatedSprite2D>("Player");
		_backgroundSprite = GetNode<AnimatedSprite2D>("Background");
		_monitorlight = GetNode<PointLight2D>("Spotlight");

		_playerSprite.AnimationFinished += OnPlayerAnimationFinished;
		_backgroundSprite.AnimationFinished += OnBackgroundAnimationFinished;

		_playerOrigin = _playerSprite.Position;

		StartPlayerTimer();
		StartBackgroundTimer();
		
		StartSwaying();

	}

	// --- PLAYER LOGIC ---
	private void StartPlayerTimer()
	{
		float waitTime = _animRand.Next(10, 31); // 10 to 30 seconds
		GetTree().CreateTimer(waitTime).Timeout += () => _playerSprite.Play();
	}

	private void OnPlayerAnimationFinished()
	{
		_playerSprite.Stop(); 
		StartPlayerTimer(); 
	}

	private void StartSwaying()
	{
		Tween swayTween = CreateTween();

		// Calculate the target position (2 pixels to the right and left)
		Vector2 targetPosR = _playerSprite.Position + new Vector2(_animRand.Next(0, 3), 0);
		Vector2 targetPosL = _playerSprite.Position + new Vector2(-_animRand.Next(0, 3), 0);

		swayTween.TweenProperty(_playerSprite, "position", targetPosR, 1.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		swayTween.TweenProperty(_playerSprite, "position", _playerSprite.Position, 1.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		swayTween.TweenProperty(_playerSprite, "position", targetPosL, 1.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		swayTween.TweenProperty(_playerSprite, "position", _playerSprite.Position, 1.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		swayTween.Finished += StartSwaying;	
	}

	// --- BACKGROUND LOGIC ---
	private void StartBackgroundTimer()
	{
		float waitTime = _animRand.Next(45, 91); // 45 to 90 seconds
		GetTree().CreateTimer(waitTime).Timeout += () => _backgroundSprite.Play();
	}

	private void OnBackgroundAnimationFinished()
	{
		_backgroundSprite.Stop();
		StartBackgroundTimer();
	}

	// -- Monitor Light Logic --- //
	public void ActivateSpotlight()
	{
		_monitorlight.Enabled = true;
		// _switchNoise.Play();
		Tween tween = CreateTween();
		tween.TweenProperty(_monitorlight, "energy", 0.8f, 0.2f).From(0f);
	}

	public void DeactivateSpotlight()
	{
		// _switchNoise.Play();
		Tween tween = CreateTween();
		tween.TweenProperty(_monitorlight, "energy", 0f, 0.2f).From(0.8f);
		tween.Finished += () => _monitorlight.Enabled = false;
	}
	
	public override void _Process(double delta)
	{
	}
}
