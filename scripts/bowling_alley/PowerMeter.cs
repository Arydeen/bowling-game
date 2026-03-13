using Godot;
using System;

public partial class PowerMeter : Control
{

	[Export] public float SweetSpotSize = 0.2f;
	[Export] public float SliderSpeed = 1.5f;

	private Sprite2D _slider;
	private ColorRect _greenZone;
	private float _timer = 0f;
	private float _currentPower = 0.5f;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_timer += 1;
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
