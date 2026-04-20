using Godot;
using System;
using System.Threading;

public partial class GameManager : Node2D
{

	[Export] public PackedScene BallScene;
	[Export] public Vector2 BallSpawnPos = new Vector2(160, 175);
	[Export] public PowerMeter Meter;
	[Export] public int PinHealth = 100;

	private Monitor _monitor;
	private PointLight2D _spotlight;
	private AudioStreamPlayer2D _switchNoise;

	// Start Game Tracking Variables ------------------------------------------- //
	private Ball _currentBall;
	private int _totalScore = 0; // Score over the whole game
	private int _roundScore = 0; // Score over is round (5 Frames)
	private int _frameScore = 0; // Score this frame
	private int _shotScore = 0; // Score this shot

	private int _roundNum = 0; // Current round
	private int _frameNum = 0; // Current frame in round
	private int _shotNum = 0; // Current shot in frame
	private bool _firstFrame = true; // Is this the first Frame

	// End Game Tracking Variables --------------------------------------------- //

	public override void _Ready()
	{
		_monitor = GetNode<Monitor>("../Monitor");

		_spotlight = GetNode<PointLight2D>("../Spotlight");
		_spotlight.Enabled = false;
		_switchNoise = GetNode<AudioStreamPlayer2D>("../Spotlight/SpotlightNoise");

		StartRound();
		SpawnNewBall();
	}


	// Reset Methods //
	private void ResetPins()
	{
		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin)
			{
				if (!pin.Alive) { pin.FadeIn(); }
				pin.Alive = true;
				pin.SetHitThisRound(false);
				pin.SetHealth(PinHealth);
				pin.SetHealthBar(PinHealth);

				pin._kineticFlag = false;
			}
		}
		ResetPinsForRound();
	}

	public void ResetScoreboard()
	{
		for (int frame = 1; frame < 5; frame++)
		{
			for (int shot = 1; shot < 4; shot++)
			{
				_monitor.GetNode<Label>($"ScoreboardControl/ScoreboardHBox/Frame{frame}/Shots/Shot{shot}").Text = "";
			}
			_monitor.GetNode<Label>($"ScoreboardControl/ScoreboardHBox/Frame{frame}/FrameTotal").Text = "";
		}
	}

	private void StartRound()
	{
		_roundNum += 1;

		_roundScore = 0;
		_frameScore = 0;
		_shotScore = 0;

		_frameNum = 0;
		_shotNum = 0;
		ResetScoreboard();
		StartFrame();
	}

	private void StartFrame()
	{
		if (_frameNum + 1 > 4) {StartRound(); return;}

		_frameScore = 0;
		_frameNum += 1;

		_shotNum = 0;
		_shotScore = 0;
		
		if (!_firstFrame) { GetTree().CreateTimer(1.25f).Timeout += () => ResetPins(); }
		if (_frameNum == 4) {FadeToNight();}
		else if (_frameNum == 1 && !_firstFrame) {FadeToDay();}
		_firstFrame = false;

		StartShot();
	}

	private void StartShot()
	{
		_shotScore = 0;
		_shotNum += 1;
	}

	// This method resets the "Hit this round" status for all pins
	private void ResetPinsForRound()
	{
		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin && pin.Alive)
			{
				pin.SetHitThisRound(false);
			}
		}
	}
	// End Reset Methods //

	// Animation / Effect Methods //
	public void ActivateSpotlight()
	{
		_spotlight.Enabled = true;
		_switchNoise.Play();
		Tween tween = CreateTween();
		tween.TweenProperty(_spotlight, "energy", 0.8f, 0.2f).From(0f);
	}

	public void DeactivateSpotlight()
	{
		_switchNoise.Play();
		Tween tween = CreateTween();
		tween.TweenProperty(_spotlight, "energy", 0f, 0.2f).From(0.8f);
		tween.Finished += () => _spotlight.Enabled = false;
	}


	public void FadeToNight(float duration = 2.0f)
	{
		CanvasModulate lights = GetNode<CanvasModulate>("../Lights");

		Color nightColor = new Color(0.3f, 0.3f, 0.6f);

		Tween tween = CreateTween();

		tween.TweenProperty(lights, "color", nightColor, duration)
		 .SetTrans(Tween.TransitionType.Sine)
		 .SetEase(Tween.EaseType.Out);

		 tween.Finished += () => ActivateSpotlight();
	}

	public void FadeToDay(float duration = 2.0f)
	{
		CanvasModulate lights = GetNode<CanvasModulate>("../Lights");

		Color DayColor = new Color(1f, 1f, 1f);

		Tween tween = CreateTween();

		DeactivateSpotlight();

		tween.TweenProperty(lights, "color", DayColor, duration)
		 .SetTrans(Tween.TransitionType.Sine)
		 .SetEase(Tween.EaseType.Out);
	}
	// End Animation / Effect Methods //

	// Scoreboard Methods //
	private void UpdateFrameText()
	{
		string newText = _roundScore.ToString();
		switch(_frameNum)
		{
			case (1):
				_monitor.f1t.Text = newText;
				break;
			case (2):
				_monitor.f2t.Text = newText;
				break;
			case (3):
				_monitor.f3t.Text = newText;
				break;
			case (4):
				_monitor.f4t.Text = newText;
				break;
		}
	}

	private void UpdateShotText()
	{
		string path = $"ScoreboardControl/ScoreboardHBox/Frame{_frameNum}/Shots/Shot{_shotNum}";
		Label shotLabel = _monitor.GetNode<Label>(path);
		
		if (shotLabel != null)
		{
			shotLabel.Text = _shotScore.ToString();
		}
	}
	// End Scoreboard Methods //

	// Ball Methods //
	public void SpawnNewBall()
	{
		ResetPinsForRound();

		_currentBall = BallScene.Instantiate<Ball>();
		_currentBall.Initialize(BallSpawnPos);
		AddChild(_currentBall);

		// Ball - Meter linking
		_currentBall.Position = BallSpawnPos;
		_currentBall.Meter = Meter;

		Meter.Ball = _currentBall;

		_currentBall.TreeExited += OnBallRemoved;
	}

	private void OnBallRemoved() 
	{
		UpdateShotText();
		AddScore(_shotScore, frame:true, total:true);

		if (_shotNum == 3)
		{
			GD.Print(_frameNum);
			GD.Print(_shotNum);
			AddScore(_frameScore, round:true);
			UpdateFrameText();
			StartFrame();
		} 
		else
		{
			GD.Print(_frameNum);
			GD.Print(_shotNum);
			StartShot();
		}
			
		GetTree().CreateTimer(1.25f).Timeout += () => SpawnNewBall();
	}
	// End Ball Methods //

	// Score Methods //
	public int GetScore()
	{
		return _totalScore;
	}

	public void AddScore(int amount, bool total = false, bool round = false, bool frame = false, bool shot = false)
	{
		if (total) {_totalScore += amount; GlobalData.Instance.TotalPins += amount;}
		if (round) {_roundScore += amount;}
		if (frame) {_frameScore += amount;}
		if (shot) {_shotScore += amount;}
	}
	// End Score Methods

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("GiveKineticBall"))
		{
			GlobalData.Instance.KineticBall = true;
		}
	}
}
