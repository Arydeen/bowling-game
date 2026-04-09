using Godot;
using System;

public partial class GameManager : Node2D
{

	[Export] public PackedScene BallScene;
	[Export] public Vector2 BallSpawnPos = new Vector2(160, 175);
	[Export] public PowerMeter Meter;

	private Monitor _monitor;
	

	private Ball _currentBall;
	private int _totalScore = 0;
	private int _roundScore = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
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
		//_monitor.SetText(GlobalData.Instance.TotalPins);
		GetTree().CreateTimer(1.0f).Timeout += () => SpawnNewBall();
		_roundScore = 0;
	}

	public void AddScore(int amount)
	{
		_roundScore += amount;
		GlobalData.Instance.TotalPins += amount;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
