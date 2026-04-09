using Godot;
using System;

public partial class Monitor : Node2D
{
	
	private Vector2 _hiddenPos;
	private Vector2 _visiblePos = new Vector2(66, 64);
	private Label _testLabel;

	public override void _Ready()
	{

		// _testLabel = GetNode<Label>("ScoreBoard/Label");

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
		_testLabel.Text = value.ToString();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
