using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node2D
{
	public enum Challenge { Boss, Night }
	[Export] public PackedScene BallScene;
	[Export] public Vector2 BallSpawnPos = new Vector2(160, 175);
	[Export] public PowerMeter Meter;
	[Export] public int PinHealth = 100;
	[Export] public double PinHealthScale = 1.3;
	[Export] public double PinArmorScale = 1.2;
	[Export] public bool InputLock = false;

	[Export] public Challenge NextChallenge = Challenge.Night;
	[Export] public bool isNight = false;
	[Export] public bool isBoss = false;

	private Monitor _monitor;
	private Bumpers _bumpers;
	private PlayerMonitor _coach;
	private PointLight2D _spotlight;
	private AudioStreamPlayer2D _switchNoise;
	private bool _pinSlayerUsedThisShot = false;

	private Ball _currentBall;
	private int _totalScore = 0; // Score over the whole game
	private int _roundScore = 0; // Score over is round (5 Frames)
	private int _frameScore = 0; // Score this frame
	private int _shotScore = 0; // Score this shot
	private int _shotBallsAlive = 0; // Current active balls

	private BossPin _boss; // Frame Boss if active

	private int _roundNum = 0; // Current round
	private int _frameNum = 0; // Current frame in round
	private int _shotNum = 0; // Current shot in frame
	private int _nightReq = 4; // Number of pins needed to pass the night
	private bool _firstFrame = true; // Is this the first Frame

	// Boss Tracking Vars //
	private int _bossBallsLeft;

	// End Boss Tracking Vars //

	// Bumper Tracking Vars //
	private Node _stats;
	// End Bumper Tracking Vars //

	// Ball Tracking Vars //
	private readonly List<Ball> _activeBalls = new();

	private bool _splitChainUsedThisShot = false;
	private ulong _splitChainStartedAtMs = 0;
	// End Ball Tracking Vars //

	// End Game Tracking Variables --------------------------------------------- //

	public override void _Ready()
	{
		_monitor = GetNode<Monitor>("../Monitor");
		_bumpers = GetNode<Bumpers>("../Bumpers");
		_coach = GetNode<PlayerMonitor>("../PlayerMonitor");
		_spotlight = GetNode<PointLight2D>("../Spotlight");
		_spotlight.Enabled = false;
		_switchNoise = GetNode<AudioStreamPlayer2D>("../Spotlight/SpotlightNoise");

		_stats = GetNodeOrNull<Node>("/root/Player");
		if (_stats != null)
		{
			GD.Print("[GameManager] Player found at /root/Player");

			if (_stats.HasSignal("stats_changed"))
			{
				if (!_stats.IsConnected("stats_changed", Callable.From(OnStatsChanged)))
					_stats.Connect("stats_changed", Callable.From(OnStatsChanged));
			}

			CallDeferred(nameof(DeferredInitialBumperSync));
		}
		else
		{
			GD.PushWarning("[GameManager] Player autoload not found at /root/Player");
		}

		if (GetTree().Root.FindChild("Pinion1", true, false) is Pinion pinion)
		{
			pinion.PinionDied += () =>
			{
				_monitor.PinionCount.Text = _boss.ActivePinions.ToString();
			};
		}
		if (GetTree().Root.FindChild("Pinion2", true, false) is Pinion pinion2)
		{
			pinion2.PinionDied += () =>
			{
				_monitor.PinionCount.Text = _boss.ActivePinions.ToString();
			};
		}

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

	// Bumper Methods //

	private void DeferredInitialBumperSync()
	{
		if (_bumpers == null || !GodotObject.IsInstanceValid(_bumpers))
		{
			GD.Print("[GameManager] _bumpers is not ready yet");
			return;
		}

		SyncBumpersForNewFrame();
	}

	private int GetBumperCapacity()
	{
		if (_stats == null)
		{
			GD.Print("[GameManager] _stats is NULL");
			return 0;
		}

		if (!_stats.HasMethod("get_bumpers"))
		{
			GD.Print("[GameManager] Stats does NOT have get_bumpers()");
			return 0;
		}

		Variant v = _stats.Call("get_bumpers");

		GD.Print($"[GameManager] get_bumpers returned: {v}");

		double d = (double)v;
		return Math.Max(0, Mathf.RoundToInt((float)d));
	}

	private void SyncBumpersForNewFrame()
	{
		_bumpers.ApplyForNewFrame(GetBumperCapacity());
	}

	private void OnStatsChanged()
	{
		if (_bumpers == null || !GodotObject.IsInstanceValid(_bumpers))
			return;

		_bumpers.ApplyCapacityMidFrame(GetBumperCapacity());
	}

	// Prize Methods //
	private int GetPinSlayerCount()
	{
		if (_stats == null)
			return 0;

		if (!_stats.HasMethod("get_pin_slayer_count"))
			return 0;

		Variant v = _stats.Call("get_pin_slayer_count");
		return Math.Max(0, (int)v);
	}

	public void ApplyPinSlayerIfAvailable(Pin pin)
	{
		if (pin == null)
			return;

		if (_pinSlayerUsedThisShot)
			return;

		int count = GetPinSlayerCount();
		if (count <= 0)
			return;

		_pinSlayerUsedThisShot = true;

		double armorLossPercent = Math.Min(1.0, count * 0.25);

		pin.ReduceArmorByPercent(armorLossPercent);

		GD.Print($"[PinSlayer] count={count}, armor loss={armorLossPercent * 100.0}%");
	}

	public int GetKineticImpactCount()
	{
		if (_stats == null)
			return 0;

		if (!_stats.HasMethod("get_kinetic_impact_count"))
			return 0;

		Variant v = _stats.Call("get_kinetic_impact_count");
		return Math.Max(0, (int)v);
	}

	public bool HasKineticImpactPrize()
	{
		return GetKineticImpactCount() > 0;
	}

	public int GetKineticImpactDamageMultiplier()
	{
		int count = GetKineticImpactCount();

		if (count <= 0)
			return 1;

		return count + 1;
	}

	private void SyncKineticImpactFlag()
	{
		GlobalData.Instance.KineticBall = GetKineticImpactCount() > 0;
	}

	private int GetSplitCount()
	{
		if (_stats == null)
			return 0;

		if (!_stats.HasMethod("get_split_count"))
			return 0;

		Variant v = _stats.Call("get_split_count");
		return Math.Max(0, (int)v);
	}

	private float GetSplitScale(int splitCount)
	{
		// 1 split = 75%
		// 2 splits = 60%
		// 3 splits = 45%
		// 4 splits = 30%
		// 5+ splits = 25% minimum
		return Mathf.Max(0.25f, 0.75f - (0.15f * (splitCount - 1)));
	}

	public void TriggerSplitChain(Ball source)
	{
		if (source == null)
			return;

		if (_splitChainUsedThisShot)
			return;

		int splitCount = GetSplitCount();

		if (splitCount <= 0)
			return;

		_splitChainUsedThisShot = true;
		_splitChainStartedAtMs = Time.GetTicksMsec();

		GD.Print($"[Split] chain started. splitCount={splitCount}");

		ScheduleSplitForBall(source, 0.01f);

		foreach (Ball ball in _activeBalls.ToArray())
		{
			if (ball == null || !GodotObject.IsInstanceValid(ball))
				continue;

			if (ball == source)
				continue;

			if (!ball.IsAfterImage)
				continue;

			if (ball.IsSplitBall || ball.HasSplit || ball.SplitScheduled)
				continue;

			float delay = 0.10f * Math.Max(1, ball.AfterImageIndex);
			ScheduleSplitForBall(ball, delay);
		}
	}

	private void MaybeScheduleSplitForNewAfterImage(Ball ball)
	{
		if (!_splitChainUsedThisShot)
			return;

		if (ball == null)
			return;

		if (!ball.IsAfterImage)
			return;

		if (ball.IsSplitBall || ball.HasSplit || ball.SplitScheduled)
			return;

		float targetDelay = 0.10f * Math.Max(1, ball.AfterImageIndex);

		double elapsed = (Time.GetTicksMsec() - _splitChainStartedAtMs) / 1000.0;
		float remaining = Mathf.Max(0.01f, targetDelay - (float)elapsed);

		ScheduleSplitForBall(ball, remaining);
	}

	private void ScheduleSplitForBall(Ball ball, float delay)
	{
		if (ball == null)
			return;

		if (ball.SplitScheduled || ball.HasSplit || ball.IsSplitBall)
			return;

		ball.SplitScheduled = true;

		GetTree().CreateTimer(delay).Timeout += () =>
		{
			if (ball == null || !GodotObject.IsInstanceValid(ball))
				return;

			ball.PerformSplit();
		};
	}

	public void SpawnSplitBallsFrom(Ball source, int splitCount)
	{
		if (source == null || !GodotObject.IsInstanceValid(source))
			return;

		splitCount = Math.Max(1, splitCount);

		float splitScale = GetSplitScale(splitCount);

		int ballsToSpawn = splitCount * 2;
		_shotBallsAlive += ballsToSpawn;

		GD.Print($"[Split] spawning {ballsToSpawn} balls from {source.Name}, scale={splitScale}");

		float xSpacing = 10f;
		float baseKick = 120f;
		float kickStep = 45f;

		for (int i = 1; i <= splitCount; i++)
		{
			float kick = baseKick + ((i - 1) * kickStep);

			SpawnOneSplitBall(source, new Vector2(-xSpacing * i, 0), splitScale, -kick);
			SpawnOneSplitBall(source, new Vector2(xSpacing * i, 0), splitScale, kick);
		}
	}

	private void SpawnOneSplitBall(Ball source, Vector2 offset, float splitScale, float addedPowerVal)
	{
		var b = BallScene.Instantiate<Ball>();

		AddChild(b);

		b.InitializeSplitCloneFrom(
			source,
			source.GlobalPosition + offset,
			splitScale,
			addedPowerVal
		);

		RegisterShotBall(b);
	}

	// Reset Methods //
	private void ResetPins()
	{
		SyncKineticImpactFlag();
		var allPins = GetTree().GetNodesInGroup("Pins");

		foreach (Node node in allPins)
		{
			if (node is Pin pin)
			{
				if (!pin.Alive) { pin.FadeIn(); }

				pin.Alive = true;
				pin._hitThisShot = false;

				double scaledHealth = PinHealth * PinHealthScale;

				pin.SetHealth(scaledHealth);
				pin.SetHealthBarMax(scaledHealth);
				pin.SetHealthBar(scaledHealth);

				pin.SetScaledPinArmor(PinArmorScale);

				pin._kineticFlag = false;
			}
		}

		ResetPinsForRound();
		SyncBumpersForNewFrame();
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
		_pinSlayerUsedThisShot = false;
		_splitChainUsedThisShot = false;
		_splitChainStartedAtMs = 0;

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

	private void RegisterShotBall(Ball ball)
	{
		if (ball == null)
			return;

		_activeBalls.Add(ball);

		ball.TreeExited += () =>
		{
			_activeBalls.Remove(ball);
			OnShotBallExited();
		};
	}

	public void SpawnNewBall()
	{
		_currentBall = BallScene.Instantiate<Ball>();
		_currentBall.Initialize(BallSpawnPos);
		AddChild(_currentBall);

		_currentBall.Position = BallSpawnPos;
		_currentBall.Meter = Meter;
		Meter.Ball = _currentBall;

		_shotBallsAlive = 1;
		RegisterShotBall(_currentBall);
	}

	private void OnShotBallExited()
	{
		_shotBallsAlive--;
		if (_shotBallsAlive <= 0)
			OnBallRemoved(); 
	}

	public void SpawnAfterImages(Vector2 startPos, float speed, float rawX, bool sweet, int count)
	{
		if (count <= 0) return;

		_shotBallsAlive += count;

		for (int i = 1; i <= count; i++)
		{
			int idx = i; 
			float delay = 0.10f * idx; 

			GetTree().CreateTimer(delay).Timeout += () =>
			{
				var b = BallScene.Instantiate<Ball>();
				b.IsAfterImage = true;
				b.AfterImageIndex = idx;

				AddChild(b);
				b.Initialize(startPos + new Vector2(0, 10 * idx));

				RegisterShotBall(b);

				b.CallDeferred("FinalizePower", speed, rawX, sweet);

				MaybeScheduleSplitForNewAfterImage(b);
			};
		}
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
			GD.Print($"[KineticImpact] count={GetKineticImpactCount()}, mult={GetKineticImpactDamageMultiplier()}x");
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
