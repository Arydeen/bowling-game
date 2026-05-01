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
	public bool PendingBossAfterShop = false;

	// Saved lanes/scoreboard state
	public bool SavedLaneGame = false;

	// Day scoreboard: 4 frames * 3 shots = 12 shot labels
	public string[] SavedDayShotTexts = new string[12];
	public string[] SavedDayFrameTotals = new string[4];

	// Night scoreboard
	public string[] SavedNightShotTexts = new string[3];
	public string SavedNightNeedText = "";
	public string SavedNightHaveText = "";

	// Saved mode/state
	public bool SavedIsNight = false;
	public bool SavedIsBoss = false;
	public int SavedNextChallenge = 1; // 0 = Boss, 1 = Night

	public double SavedPinHealthScale = 1.3;
	public double SavedPinArmorScale = 1.2;

	public override void _Ready()
	{
		Instance = this;
	}

	public void SpendPins(int amount)
	{
		TotalPins -= amount;
	}

	public void ClearLaneSave()
	{
		SavedLaneGame = false;

		for (int i = 0; i < SavedDayShotTexts.Length; i++)
			SavedDayShotTexts[i] = "";

		for (int i = 0; i < SavedDayFrameTotals.Length; i++)
			SavedDayFrameTotals[i] = "";

		for (int i = 0; i < SavedNightShotTexts.Length; i++)
			SavedNightShotTexts[i] = "";

		SavedNightNeedText = "";
		SavedNightHaveText = "";

		SavedIsNight = false;
		SavedIsBoss = false;
		SavedNextChallenge = 1;

		SavedPinHealthScale = 1.3;
		SavedPinArmorScale = 1.2;
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
		PendingBossAfterShop = false;
		BossBallsLeft = 0;

		ClearLaneSave();
	}
}
