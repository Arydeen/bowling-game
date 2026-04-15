using Godot;
using System;

public partial class Monitor : Node2D
{
	
	private Vector2 _hiddenPos;
	private Vector2 _visiblePos = new Vector2(66, 64);

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

	public override void _Ready()
	{

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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
