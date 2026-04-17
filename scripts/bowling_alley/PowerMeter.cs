using Godot;
using System;

public partial class PowerMeter : Control
{
	[Export] public float SweetSpotSize = 0.03f;
	[Export] public float SliderSpeed = 400f; // Increased for noticeable movement per frame
	[Export] public float PowerVal = 0f;
	[Export] public Ball Ball;

	private Sprite2D _slider;
	private ColorRect _greenZone;
	[Export] public float GreenZoneSpeed = 30f;

	private ColorRect _yellowZone;
	[Export] public float YellowZoneSpeed = 60f;

	private ColorRect _blueZone;
	[Export] public float BlueZoneSpeed = 80f;

	private ColorRect _redZone;
	[Export] public float RedZoneSpeed = 100f;
	
	private Vector2 _sliderPos = new Vector2(0, 0);
	private Vector2 _targetPos; // The "Home" position on screen
	private bool _meterActive = false;
	private bool _movingLeft = false;
	private bool _sliderStop = false;
	private bool _canStop = false;

	public override void _Ready()
	{
		_slider = GetNode<Sprite2D>("Slider");
		_greenZone = GetNode<ColorRect>("ZonesContainer/ColorRectGreen");
		_yellowZone = GetNode<ColorRect>("ZonesContainer/ColorRectYellow");
		_blueZone = GetNode<ColorRect>("ZonesContainer/ColorRectBlue");
		_redZone = GetNode<ColorRect>("ZonesContainer/ColorRectRed");

		// Save the position you set in the 2D editor as the goal
		_targetPos = Position;
		
		// Start hidden (bottom of screen)
		Visible = false;

		UpdateZoneSizes();
	}


	// Start Meter Show-Hide Functions ------------------------------------------- //
	public void ShowMeter()
	{
		if (_meterActive) return;

		_meterActive = true;
		_canStop = false;
		Visible = true;

		float screenHeight = GetViewportRect().Size.Y;
		Position = new Vector2(_targetPos.X, screenHeight + 100);

		Tween tween = GetTree().CreateTween();
		// Transition to _targetPos over 0.5 seconds with an 'Out' ease
		tween.TweenProperty(this, "position", _targetPos, 0.5f)
			 .SetTrans(Tween.TransitionType.Back)
			 .SetEase(Tween.EaseType.Out);
	
		tween.Finished += () => _canStop = true;
	}

	public void HideMeter()
	{
		
		// float screenHeight = GetViewportRect().Size.Y;
		// Vector2 hidePos = new Vector2(_targetPos.X, screenHeight + 100);
		float screenHeight = GetViewportRect().Size.Y;
		Vector2 hidePos = new Vector2(_targetPos.X, screenHeight + 100);

		Tween tween = GetTree().CreateTween();
		
		tween.TweenInterval(0.25f); 

		tween.TweenProperty(this, "position", hidePos, 0.4f) 
			.SetEase(Tween.EaseType.In); 

		tween.Finished += () => 
		{
			Visible = false;
			_meterActive = false;
			// Reset slider to center for next time
			_sliderPos.X = 0; 
			_slider.Position = _sliderPos;
			_sliderStop = false;
		};
	}

	// End Meter Show-Hide Functions --------------------------------------------- //

	// Start Slider Functions ---------------------------------------------------- //
	public float StopSlider()
	{
		_sliderStop = true;
		HideMeter();

		float finalSpeed = GetSpeedFromZone();
		bool sweet = IsSweet(); 

		Ball.FinalizePower(finalSpeed, _sliderPos.X, sweet);

		return _sliderPos.X;
	}

	public void MoveSlider(double delta)
	{
		// Only move the slider if the meter is actually being played
		if (!_meterActive || _sliderStop) return;

		// Logic to ping-pong the slider back and forth
		if (_sliderPos.X >= 112) _movingLeft = true;
		else if (_sliderPos.X <= -112) _movingLeft = false;

		float direction = _movingLeft ? -1 : 1;
		_sliderPos.X += direction * SliderSpeed * (float)delta;
		
		_slider.Position = _sliderPos;
	}
	// End Slider Functions ------------------------------------------------------ //

	// Start Zone Functions ------------------------------------------------------ //
	public void UpdateZoneSizes()
	{
		float totalWidth = 238f;
		_greenZone.Size = new Vector2(totalWidth, _greenZone.Size.Y);
		_greenZone.Position = new Vector2(0, 0);

		_yellowZone.Size = new Vector2(totalWidth * SweetSpotSize * 12, _yellowZone.Size.Y);
		_yellowZone.Position = new Vector2((totalWidth - _yellowZone.Size.X) / 2, 0);

		_blueZone.Size = new Vector2(totalWidth * SweetSpotSize * 6, _blueZone.Size.Y);
		_blueZone.Position = new Vector2((totalWidth - _blueZone.Size.X) / 2, 0);

		_redZone.Size = new Vector2(totalWidth * SweetSpotSize, _redZone.Size.Y);
		_redZone.Position = new Vector2((totalWidth - _redZone.Size.X) / 2, 0);
	}

	public float GetSpeedFromZone()
	{
		float sliderX = _slider.Position.X;

		if (IsSliderInRect(_redZone, sliderX)) return RedZoneSpeed;
		if (IsSliderInRect(_blueZone, sliderX)) return BlueZoneSpeed;
		if (IsSliderInRect(_yellowZone, sliderX)) return YellowZoneSpeed;
		
		return GreenZoneSpeed; // Default/Miss
	}

	public bool IsSweet()
	{
		float sliderX = _slider.Position.X;

		if (IsSliderInRect(_redZone, sliderX)) return true;
		
		return false; // Default/Miss
	}

	private bool IsSliderInRect(ColorRect rect, float sliderX)
	{
		float leftEdge = rect.Position.X  - (238 / 2f);
		float rightEdge = leftEdge + rect.Size.X;

		return sliderX >= leftEdge && sliderX <= rightEdge;
	}

	// End Zone Functions -------------------------------------------------------- //


	public override void _Process(double delta)
	{
		// Only process slider logic if meter is active
		MoveSlider(delta);

		if (_meterActive && _canStop && Input.IsActionJustPressed("power_meter_stop"))
		{
			PowerVal = StopSlider();
		}
	}
}
