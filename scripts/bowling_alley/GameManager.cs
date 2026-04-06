using Godot;
using System;

public partial class GameManager : Node2D
{

	[Export] public PackedScene BallScene;
	[Export] public Vector2 BallSpawnPos = new Vector2(160, 169);
	[Export] public PowerMeter Meter;

	private Ball _currentBall;
	private int _totalScore = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SpawnNewBall();
	}

	public void SpawnNewBall()
	{
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
		GetTree().CreateTimer(1.0f).Timeout += () => SpawnNewBall();
	}

	public void AddScore(int amount)
	{
		GlobalData.Instance.TotalPins += amount;

		GD.Print($"Pins collected this shot: {amount}");
		GD.Print($"All-time Pins: {GlobalData.Instance.TotalPins}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
