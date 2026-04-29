using Godot;
using System;

public partial class Monitor : Node2D
{
	
	private Vector2 _hiddenPos;
	private Vector2 _visiblePos = new Vector2(66, 64);

	public VideoStreamPlayer _video { get; set; }
	private AnimatedSprite2D _animSprite;
	private ShaderMaterial _shaderMat;
	private PointLight2D _monitorlight;
	private GameManager _gameManager;

	// Day Scorboard //
	// Frame 1 Nodes
	public Label f1s1 { get; set; }
	public Label f1s2 { get; set; }
	public Label f1s3 { get; set; }
	public Label f1t { get; set; }

	// Frame 2 Nodes
	public Label f2s1 { get; set; }
	public Label f2s2 { get; set; }
	public Label f2s3 { get; set; }
	public Label f2t { get; set; }

	// Frame 3 Nodes
	public Label f3s1 { get; set; }
	public Label f3s2 { get; set; }
	public Label f3s3 { get; set; }
	public Label f3t { get; set; }

	// Frame 4 Nodes
	public Label f4s1 { get; set; }
	public Label f4s2 { get; set; }
	public Label f4s3 { get; set; }
	public Label f4t { get; set; }
	// End Day Scorboard //

	// Night Scoreboard //
	public Label fns1 { get; set; }
	public Label fns2 { get; set; }
	public Label fns3 { get; set; }
	// Night "Need"
	public Label fnn { get; set; }
	// Night "Total"
	public Label fnt { get; set;}
	// End Night Scoreboard //

	public override void _Ready()
	{
		_gameManager = GetNode<GameManager>("../GameManager");

		_video = GetNode<VideoStreamPlayer>("VideoStreamPlayer");
		_monitorlight = GetNode<PointLight2D>("Spotlight");

		_animSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_shaderMat = _animSprite.Material as ShaderMaterial;

		// Day Scorboard //
		f1s1 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot1");
		f1s2 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot2");
		f1s3 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot3");
		f1t = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame1/FrameTotal");

		f2s1 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame2/Shots/Shot1");
		f2s2 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame2/Shots/Shot2");
		f2s3 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame2/Shots/Shot3");
		f2t = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame2/FrameTotal");

		f3s1 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame3/Shots/Shot1");
		f3s2 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame3/Shots/Shot2");
		f3s3 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame3/Shots/Shot3");
		f3t = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame3/FrameTotal");

		f4s1 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame4/Shots/Shot1");
		f4s2 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame4/Shots/Shot2");
		f4s3 = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame4/Shots/Shot3");
		f4t = GetNode<Label>("ScoreboardControl/ScoreboardHBox/Frame4/FrameTotal");
		// End Day Scoreboard //

		// Night Scoreboard //
		fns1 = GetNode<Label>("NightScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot1");
		fns2 = GetNode<Label>("NightScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot2");
		fns3 = GetNode<Label>("NightScoreboardControl/ScoreboardHBox/Frame1/Shots/Shot3");

		fnn = GetNode<Label>("NightScoreboardControl/ScoreboardVBox/Need");
		fnt = GetNode<Label>("NightScoreboardControl/ScoreboardVBox/Have");
		// End Night Scoreboard //

		_hiddenPos = Position;
		ShowMonitor();
	}

	public void ShowMonitor()
	{
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(this, "position", _visiblePos, 2f).SetEase(Tween.EaseType.Out);;
	}

	public void SetText(int value)
	{
		f1s1.Text = value.ToString();
	}

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
	

	public void TransitionToNight(float duration = 1.0f)
	{
		// 1. Get the texture for the first frame of the night animation
		var nightFrames = _animSprite.SpriteFrames;
		Texture2D nightTex = nightFrames.GetFrameTexture("night_scoreboard", 0);

		// 2. Pass that texture to the shader's "target_texture" slot
		_shaderMat.SetShaderParameter("target_texture", nightTex);

		// 3. Tween the weight from 0 (Day) to 1 (Night)
		Tween tween = CreateTween();
		tween.TweenProperty(_shaderMat, "shader_parameter/mix_weight", 1.0f, duration);

		// 4. When finished, swap the actual animation and reset the weight
		tween.Finished += () => {
			_animSprite.Play("night_scoreboard");
			_shaderMat.SetShaderParameter("mix_weight", 0.0f);
		};
	}

	public void TransitionToDay(float duration = 1.0f)
	{
		// 1. Get the texture for the first frame of the day animation
		var dayFrames = _animSprite.SpriteFrames;
		Texture2D dayTex = _gameManager.NextChallenge == GameManager.Challenge.Night ? 
							dayFrames.GetFrameTexture("day_scoreboard", 0) : 
							dayFrames.GetFrameTexture("day_scoreboard_boss", 0);

		// 2. Pass that texture to the shader's "target_texture" slot
		_shaderMat.SetShaderParameter("target_texture", dayTex);

		// 3. Tween the weight from 0 (Day) to 1 (Night)
		Tween tween = CreateTween();
		tween.TweenProperty(_shaderMat, "shader_parameter/mix_weight", 1.0f, duration);

		// 4. When finished, swap the actual animation and reset the weight
		tween.Finished += () => {
			if (_gameManager.NextChallenge == GameManager.Challenge.Night) 
				{_animSprite.Play("day_scoreboard");}
			else
				{_animSprite.Play("day_scoreboard_boss");}
			
			_shaderMat.SetShaderParameter("mix_weight", 0.0f);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
