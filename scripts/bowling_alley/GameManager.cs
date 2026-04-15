using Godot;
using System;
using System.Threading;

public partial class GameManager : Node2D
{

	[Export] public PackedScene BallScene;
	[Export] public Vector2 BallSpawnPos = new Vector2(160, 175);
	[Export] public PowerMeter Meter;

	private Monitor _monitor;
	

	// Start Game Tracking Variables ------------------------------------------- //
	private Ball _currentBall;
	private int _totalScore = 0; // Score over the whole game
	private int _roundScore = 0; // Score over is round (5 Frames)
	private int _frameScore = 0; // Score this frame
	private int _shotScore = 0; // Score this shot

	private int _roundNum = 0; // Current round
	private int _frameNum = 0; // Current frame in round
	private int _shotNum = 0; // Current shot in frame

	// End Game Tracking Variables --------------------------------------------- //

	private void StartRound()
	{
		_roundNum += 1;

		_roundScore = 0;
		_frameScore = 0;
		_shotScore = 0;

		_frameNum = 0;
		_shotNum = 0;
		StartFrame();
	}

	private void StartFrame()
	{
		_frameScore = 0;
		_frameNum += 1;

		_shotNum = 0;
		_shotScore = 0;
		StartShot();
	}

	private void StartShot()
	{
		_shotScore = 0;
		_shotNum += 1;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		StartRound();

		SpawnNewBall();
		_monitor = GetNode<Monitor>("../Monitor");
	}

	public int GetScore()
	{
		return _totalScore;
	}

	private void ResetPinsForRound()
	{
		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin)
			{
				pin.SetHitThisRound(false);
			}
		}
	}

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

	private void OnBallRemoved() // Not sure what gonna have this do eventually, but for now just spawns another ball
	{
		GD.Print($"Pins collected this shot: {_roundScore}");
		GD.Print($"All-time Pins: {GlobalData.Instance.TotalPins}");

		_monitor.SetText(GlobalData.Instance.TotalPins);

		UpdateShotText();

		if (_shotNum == 3)
			{
				// if (_frameNum == 4)
				// {
				// 	return;
				// }
				GD.Print(_frameNum);
				GD.Print(_shotNum);
				AddScore(_shotScore, frame:true);
				AddScore(_frameScore, round:true);
				UpdateFrameText();
				StartFrame();
			} 
			else
			{
				GD.Print(_frameNum);
				GD.Print(_shotNum);
				AddScore(_shotScore, frame:true);
				StartShot();
			}
			
		GetTree().CreateTimer(1.0f).Timeout += () => SpawnNewBall();
	}

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
		string newText = _shotScore.ToString();
		switch(_frameNum)
		{
			case (1):
				switch(_shotNum)
				{
					case 1:
						_monitor.f1s1.Text = newText;
						break;
					case 2:
						_monitor.f1s2.Text = newText;
						break;
					case 3:
						_monitor.f1s3.Text = newText;
						break;
				}
				break;
			case (2):
				switch(_shotNum)
				{
					case 1:
						_monitor.f2s1.Text = newText;
						break;
					case 2:
						_monitor.f2s2.Text = newText;
						break;
					case 3:
						_monitor.f2s3.Text = newText;
						break;
				}
				break;
			case (3):
				switch(_shotNum)
				{
					case 1:
						_monitor.f3s1.Text = newText;
						break;
					case 2:
						_monitor.f3s2.Text = newText;
						break;
					case 3:
						_monitor.f3s3.Text = newText;
						break;
				}
				break;
			case (4):
				switch(_shotNum)
				{
					case 1:
						_monitor.f4s1.Text = newText;
						break;
					case 2:
						_monitor.f4s2.Text = newText;
						break;
					case 3:
						_monitor.f4s3.Text = newText;
						break;
				}
				break;
		}
	} 

	public void AddScore(int amount, bool total = false, bool round = false, bool frame = false, bool shot = false)
	{
		if (total) {_totalScore += amount; GlobalData.Instance.TotalPins += amount;}
		if (round) {_roundScore += amount;}
		if (frame) {_frameScore += amount;}
		if (shot) {_shotScore += amount;}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
