using Godot;
using System;

public partial class GlobalData : Node
{
	public static GlobalData Instance { get; private set; }

	// Persistent Economy/Powerups
	public int TotalPins = 0;
	public float PowerUpSpeedBoost = 0f;
	public bool KineticBall { get; set; } = false;

	// Persistent Game Tracking
	public int TotalScore = 0;
	public int RoundScore = 0;
	public int FrameScore = 0;
	public int ShotScore = 0;

	public int RoundNum = 0;
	public int FrameNum = 0;
	public int ShotNum = 0;
	public int NightReq = 4;
	public bool FirstFrame = true;

	// Boss Tracking
	public int BossBallsLeft = 0;

	public override void _Ready()
	{
		Instance = this;
	}

	public void SpendPins(int amount)
	{
		TotalPins -= amount;
	}

	public void ResetSession()
	{
		TotalScore = 0;
		RoundScore = 0;
		FrameScore = 0;
		ShotScore = 0;
		RoundNum = 0;
		FrameNum = 0;
		ShotNum = 0;
		NightReq = 4;
		FirstFrame = true;
		KineticBall = false;
	}
}
