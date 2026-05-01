using Godot;
using System;
using System.Dynamic;
using System.Threading;

public partial class GameManager : Node2D
{

	public enum Challenge {Boss, Night}
	[Export] public PackedScene BallScene;
	[Export] public Vector2 BallSpawnPos = new Vector2(160, 175);
	[Export] public PowerMeter Meter;
	[Export] public int PinHealth = 100;
	[Export] public double PinHealthScale = 1;
	[Export] public bool InputLock = false;
 
	// Public Challenge Variables
	[Export] public Challenge NextChallenge = Challenge.Night;
	[Export] public bool isNight = false;
	[Export] public bool isBoss = false;

	private Monitor _monitor;
	private Bumpers _bumpers;
	private PlayerMonitor _coach;
	private PointLight2D _spotlight;
	private AudioStreamPlayer2D _switchNoise;

	// Start Game Tracking Variables ------------------------------------------- //
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

		StartRound();
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
		if (!_firstFrame) {PinHealthScale += 0.2;}
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
		if (_frameNum + 1 == 5) {
			StartChallenge();
			return;
		} else if (_frameNum + 1 > 5)
		{
			EndChallenge();
			return;
		} 
		_frameScore = 0;
		_frameNum += 1;

		_shotNum = 0;
		_shotScore = 0;
			
		if (_firstFrame)
		{
			CallDeferred(nameof(ResetPins)); 
		}
		else
		{
			GetTree().CreateTimer(1.25f).Timeout += ResetPins;
		}
		_firstFrame = false;

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
		_nightReq = _roundScore + 4;
		ResetDayScoreboard();
		FadeToNight();
		ResetPins();

		_frameScore = 0;
		_frameNum += 1;

		_shotNum = 0;
		_shotScore = 0;

		StartShot();
	}

	private void EndNightFrame()
	{
		isNight = false;
		ResetNightScoreboard();
		ResetPins();
		NextChallenge = (_roundNum + 1) % 2 == 0 ? Challenge.Boss : Challenge.Night;
		FadeToDay();

		StartRound();
	}

	private void StartShot()
	{
		ResetPinsForRound();
		_shotScore = 0;
		_shotNum += 1;
	}

	private void StartBossFrame()
	{
		isBoss = true;
		// Eventual will add conditional here for different bosses
		_boss = GetNode<BossPin>("../PinContainer/BossPin");
		_boss.BossKilled += () =>
		{
			AddScore(10 * _bossBallsLeft, shot:true);
			EndBossFrame(true);
		};

		ResetPins();
		DeactivatePins();

		_frameScore = 0;
		_frameNum += 1;
		_shotNum = 3;
		_shotScore = 0;

		_bossBallsLeft = 6;
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
				NextChallenge = (_roundNum + 1) % 2 == 0 ? Challenge.Boss : Challenge.Night;
				_monitor.TransitionToDay();
				isBoss = false;
				
				StartRound();
			};
		} else
		{
			_monitor._video.Play();
			_monitor._video.Finished += () => GetTree().Paused = true;
			return;
		}
	}

	public void StartBossShot()
	{
		if (_bossBallsLeft > 0)
		{
			ResetPinsForRound();
		} else
		{
			GetTree().CreateTimer(1f).Timeout += () => {
				if (_boss.Alive)
				{
					if (GetTree().Root.FindChild("ScreenShield", true, false) is Sprite2D screenShield) {screenShield.Visible = false;}
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
		if (_roundScore < _nightReq)
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
			case (5):
				_monitor.fnt.Text = newText;
				break;
		}
	}

	private void UpdateShotText()
	{
		string path = isNight ? $"NightScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot{_shotNum}" : $"ScoreboardControl/ScoreboardHBox/Frame{_frameNum}/Shots/Shot{_shotNum}";
		Label shotLabel = _monitor.GetNode<Label>(path);
		
		if (shotLabel != null)
		{
			shotLabel.Text = _shotScore.ToString();
		}
	}

	private void UpdateNightReqText()
	{
		_monitor.fnn.Text = _nightReq.ToString();
	}
	// End Scoreboard Methods //

	// Ball Methods //
	public void SpawnNewBall()
	{
		_currentBall = BallScene.Instantiate<Ball>();
		_currentBall.Initialize(BallSpawnPos);
		AddChild(_currentBall);

		_currentBall.Position = BallSpawnPos;
		_currentBall.Meter = Meter;
		Meter.Ball = _currentBall;

		_shotBallsAlive = 1;
		_currentBall.TreeExited += OnShotBallExited;
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

				AddChild(b);
				b.Initialize(startPos + new Vector2(0, 10 * idx)); // spawn slightly behind 

				b.TreeExited += OnShotBallExited;

				b.CallDeferred("FinalizePower", speed, rawX, sweet);
			};
		}
	}

	private void OnBallRemoved() 
	{
		if (isBoss) {
			_bossBallsLeft -= 1;
			_monitor.BallsLeft.Text = _bossBallsLeft.ToString();
			StartBossShot();
			GetTree().CreateTimer(1.25f).Timeout += () => SpawnNewBall(); 
			return;
		}

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

	void SkipToNight()
	{
		_frameNum = 4;
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
