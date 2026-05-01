using Godot;
using System;

public partial class GameManager : Node2D
{
	public enum Challenge { Boss, Night }
	[Export] public PackedScene BallScene;
	[Export] public Vector2 BallSpawnPos = new Vector2(160, 175);
	[Export] public PowerMeter Meter;
	[Export] public int PinHealth = 100;
	[Export] public double PinHealthScale = 1;
	[Export] public bool InputLock = false;

	[Export] public Challenge NextChallenge = Challenge.Night;
	[Export] public bool isNight = false;
	[Export] public bool isBoss = false;

	private Monitor _monitor;
	private Bumpers _bumpers;
	private PlayerMonitor _coach;
	private PointLight2D _spotlight;
	private AudioStreamPlayer2D _switchNoise;

	private Ball _currentBall;
	private BossPin _boss;

	public override void _Ready()
	{
		_monitor = GetNode<Monitor>("../Monitor");
		_bumpers = GetNode<Bumpers>("../Bumpers");
		_coach = GetNode<PlayerMonitor>("../PlayerMonitor");
		_spotlight = GetNode<PointLight2D>("../Spotlight");
		_spotlight.Enabled = false;
		_switchNoise = GetNode<AudioStreamPlayer2D>("../Spotlight/SpotlightNoise");

		// Logic for Pinions
		SetupPinionSignals();

		if (GlobalData.Instance.RoundNum == 0)
		{
			StartRound();
		}
		
		SpawnNewBall();
	}

	public void StartInputLockout(float duration)
	{
		InputLock = true;
		GetTree().CreateTimer(duration).Timeout += () => InputLock = false;
	}

	private void SetupPinionSignals()
	{
		for (int i = 1; i <= 2; i++)
		{
			if (GetTree().Root.FindChild($"Pinion{i}", true, false) is Pinion p)
			{
				p.PinionDied += () => { _monitor.PinionCount.Text = _boss.ActivePinions.ToString(); };
			}
		}
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
				pin._hitThisShot = false;
				pin.SetHealth(PinHealth * PinHealthScale);
				pin.SetHealthBar(PinHealth);

				pin._kineticFlag = false;
			}
		}
		ResetPinsForRound();
	}

	public void ResetDayScoreboard()
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

	public void ResetNightScoreboard()
	{
		for (int shot = 1; shot < 4; shot++)
		{
			_monitor.GetNode<Label>($"NightScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot{shot}").Text = "";
		}
		_monitor.GetNode<Label>($"NightScoreboardControl/ScoreboardVBox/Need").Text = "";
		_monitor.GetNode<Label>($"NightScoreboardControl/ScoreboardVBox/Have").Text = "";

	}

	public void ResetBossScoreboard()
	{
		_monitor.BallsLeft.Visible = false;
		_monitor.TotalBalls.Visible = false;
		_monitor.PinionCount.Visible = false;
		_monitor.Health.Visible = false;
	}


	private void ClearPins()
	{
		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin)
			{
				pin.DieNoScore();
			}
		}
	}

	private void DeactivatePins()
	{
		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin)
			{
				pin.Hitbox.Disabled = true;
			}
		}
	}

	private void ActivatePins()
	{
		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin)
			{
				pin.Hitbox.Disabled = false;
			}
		}
	}

	private void StartRound()
	{
		if (!GlobalData.Instance.FirstFrame) { PinHealthScale += 0.2; }
		GlobalData.Instance.RoundNum += 1;

		GlobalData.Instance.RoundScore = 0;
		GlobalData.Instance.FrameScore = 0;
		GlobalData.Instance.ShotScore = 0;
		GlobalData.Instance.FrameNum = 0;
		GlobalData.Instance.ShotNum = 0;
		
		StartFrame();
	}

	private void StartFrame()
	{
		if (GlobalData.Instance.FrameNum + 1 == 5)
		{
			StartChallenge();
			return;
		}
		else if (GlobalData.Instance.FrameNum + 1 > 5)
		{
			EndChallenge();
			return;
		}

		GlobalData.Instance.FrameScore = 0;
		GlobalData.Instance.FrameNum += 1;
		GlobalData.Instance.ShotNum = 0;
		GlobalData.Instance.ShotScore = 0;

		if (!GlobalData.Instance.FirstFrame) { GetTree().CreateTimer(1.25f).Timeout += ResetPins; }
		GlobalData.Instance.FirstFrame = false;

		StartShot();
	}

	private void StartChallenge()
	{
		if (NextChallenge == Challenge.Night)
		{
			GetTree().CreateTimer(1.25f).Timeout += StartNightFrame; 
		} else
		{
			GetTree().CreateTimer(1.25f).Timeout += StartBossFrame;
		}
	}

	private void EndChallenge()
	{
		if (NextChallenge == Challenge.Night)
		{
			GetTree().CreateTimer(1.25f).Timeout += EndNightFrame;
		} else
		{
			// EndBossFrame();
		}
	}

	private void StartNightFrame()
	{
		isNight = true;
		GlobalData.Instance.NightReq = GlobalData.Instance.RoundScore + 4;
		ResetDayScoreboard();
		FadeToNight();
		ResetPins();

		GlobalData.Instance.FrameScore = 0;
		GlobalData.Instance.FrameNum += 1;
		GlobalData.Instance.ShotNum = 0;
		GlobalData.Instance.ShotScore = 0;

		StartShot();
	}

	private void EndNightFrame()
	{
		isNight = false;
		ResetNightScoreboard();
		ResetPins();
		NextChallenge = (GlobalData.Instance.RoundNum + 1) % 2 == 0 ? Challenge.Boss : Challenge.Night;
		FadeToDay();
		StartRound();
	}

	private void StartShot()
	{
		ResetPinsForRound();
		GlobalData.Instance.ShotScore = 0;
		GlobalData.Instance.ShotNum += 1;
	}

	private void StartBossFrame()
	{
		isBoss = true;
		_boss = GetNode<BossPin>("../PinContainer/BossPin");
		_boss.BossKilled += () =>
		{
			AddScore(10 * GlobalData.Instance.BossBallsLeft, total: true);
			EndBossFrame(true);
		};

		ResetPins();
		DeactivatePins();

		GlobalData.Instance.FrameScore = 0;
		GlobalData.Instance.FrameNum += 1;
		GlobalData.Instance.ShotNum = 3;
		GlobalData.Instance.ShotScore = 0;

		GlobalData.Instance.BossBallsLeft = 6;
		_monitor.BallsLeft.Text = "6";

		StartInputLockout(3.5f);
		GetTree().CreateTimer(1.5f).Timeout += _boss.BossEnter;
		GetTree().CreateTimer(2.2f).Timeout += ResetDayScoreboard;
		GetTree().CreateTimer(2.2f).Timeout += ClearPins;
	}

	private void EndBossFrame(bool win)
	{
		if (win)
		{
			GetTree().CreateTimer(2f).Timeout += () =>
			{
				ResetBossScoreboard();
				ResetPins();
				ActivatePins();
				NextChallenge = (GlobalData.Instance.RoundNum + 1) % 2 == 0 ? Challenge.Boss : Challenge.Night;
				_monitor.TransitionToDay();
				isBoss = false;
				StartRound();
			};
		}
		else
		{
			_monitor._video.Play();
			_monitor._video.Finished += () => GetTree().Paused = true;
		}
	}

	public void StartBossShot()
	{
		if (GlobalData.Instance.BossBallsLeft > 0)
		{
			ResetPinsForRound();
		}
		else
		{
			GetTree().CreateTimer(1f).Timeout += () =>
			{
				if (_boss.Alive)
				{
					if (GetTree().Root.FindChild("ScreenShield", true, false) is Sprite2D shield) shield.Visible = false;
					ResetBossScoreboard();
					EndBossFrame(false);
				}
			};
		}
	}


	// This method resets the "Hit this round" status for all pins or Boss if Boss is active
	private void ResetPinsForRound()
	{
		if (isBoss)
		{
			_boss._hitThisShot = false;
			return;
		}

		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin && pin.Alive)
			{
				pin._hitThisShot = false;
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
		ResetDayScoreboard();

		CanvasModulate lights = GetNode<CanvasModulate>("../Lights");

		Color nightColor = new Color(0.3f, 0.3f, 0.6f);

		_monitor.TransitionToNight();
		Tween tween = CreateTween();

		tween.TweenProperty(lights, "color", nightColor, duration)
		 .SetTrans(Tween.TransitionType.Sine)
		 .SetEase(Tween.EaseType.Out);

		 tween.Finished += ActivateSpotlight;
		 tween.Finished += () => GetTree().CreateTimer(0.10f).Timeout += _monitor.ActivateSpotlight;
		 tween.Finished += () => GetTree().CreateTimer(0.20f).Timeout += Meter.ActivateSpotlight;
		 tween.Finished += () => GetTree().CreateTimer(0.20f).Timeout += _coach.ActivateSpotlight;
		 tween.Finished += UpdateNightReqText;
		 tween.Finished += UpdateFrameText;
	}

	public void FadeToDay(float duration = 2.0f)
	{
		if (GlobalData.Instance.RoundScore < GlobalData.Instance.NightReq)
		{
			DeactivateSpotlight();
			_monitor.DeactivateSpotlight();
			Meter.DeactivateSpotlight();
			_coach.DeactivateSpotlight();
			_monitor._video.Play();
			_monitor._video.Finished += () => GetTree().Paused = true;
			return;
		}

		ResetNightScoreboard();

		CanvasModulate lights = GetNode<CanvasModulate>("../Lights");

		Color DayColor = new Color(1f, 1f, 1f);

		_monitor.TransitionToDay();
		Tween tween = CreateTween();

		DeactivateSpotlight();
		GetTree().CreateTimer(0.10f).Timeout += _monitor.DeactivateSpotlight;
		GetTree().CreateTimer(0.20f).Timeout += Meter.DeactivateSpotlight;
		GetTree().CreateTimer(0.20f).Timeout += _coach.DeactivateSpotlight;

		tween.TweenProperty(lights, "color", DayColor, duration)
		 .SetTrans(Tween.TransitionType.Sine)
		 .SetEase(Tween.EaseType.Out);
	}
	// End Animation / Effect Methods //

	// Scoreboard Methods //
	private void UpdateFrameText()
	{
		string newText = GlobalData.Instance.RoundScore.ToString();
		switch (GlobalData.Instance.FrameNum)
		{
			case 1: _monitor.f1t.Text = newText; break;
			case 2: _monitor.f2t.Text = newText; break;
			case 3: _monitor.f3t.Text = newText; break;
			case 4: _monitor.f4t.Text = newText; break;
			case 5: _monitor.fnt.Text = newText; break;
		}
	}

	private void UpdateShotText()
	{
		string path = isNight 
			? $"NightScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot{GlobalData.Instance.ShotNum}" 
			: $"ScoreboardControl/ScoreboardHBox/Frame{GlobalData.Instance.FrameNum}/Shots/Shot{GlobalData.Instance.ShotNum}";
		
		Label shotLabel = _monitor.GetNode<Label>(path);
		if (shotLabel != null) shotLabel.Text = GlobalData.Instance.ShotScore.ToString();
	}

	private void UpdateNightReqText()
	{
		_monitor.fnn.Text = GlobalData.Instance.NightReq.ToString();
	}
	// End Scoreboard Methods //

	// Ball Methods //
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

	private void OnBallRemoved()
	{
		if (isBoss)
		{
			GlobalData.Instance.BossBallsLeft -= 1;
			_monitor.BallsLeft.Text = GlobalData.Instance.BossBallsLeft.ToString();
			StartBossShot();
			GetTree().CreateTimer(1.25f).Timeout += () => SpawnNewBall();
			return;
		}

		UpdateShotText();
		AddScore(GlobalData.Instance.ShotScore, frame: true, total: true);

		if (GlobalData.Instance.ShotNum == 3)
		{
			AddScore(GlobalData.Instance.FrameScore, round: true);
			UpdateFrameText();
			StartFrame();
		}
		else
		{
			StartShot();
		}

		GetTree().CreateTimer(1.25f).Timeout += () => SpawnNewBall();
	}
	// End Ball Methods //

	// Score Methods //
	public int GetScore()
	{
		return GlobalData.Instance.TotalScore;
	}

	public void AddScore(int amount, bool total = false, bool round = false, bool frame = false, bool shot = false)
	{
		if (total) { GlobalData.Instance.TotalScore += amount; GlobalData.Instance.TotalPins += amount; }
		if (round) { GlobalData.Instance.RoundScore += amount; }
		if (frame) { GlobalData.Instance.FrameScore += amount; }
		if (shot) { GlobalData.Instance.ShotScore += amount; }
	}
	// End Score Methods

	void SkipToNight()
	{
		GlobalData.Instance.FrameNum = 4;
		StartNightFrame();
	}

	void SkipToBoss()
	{
		
		StartBossFrame();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("GiveKineticBall"))
		{
			GlobalData.Instance.KineticBall = true;
		}

		if (Input.IsActionJustPressed("SkipToNight"))
		{
			SkipToNight();
		}
		if (Input.IsActionJustPressed("SkipToBoss"))
		{
			SkipToBoss();
		}
		if (Input.IsActionJustPressed("GiveBumpers"))
		{
			_bumpers.hitsAllowed = 2;
			_bumpers.ShowBumpers();
		}
		if (Input.IsActionJustPressed("NextFrame"))
		{
			UpdateFrameText();
			StartFrame();
		}
	}
}
