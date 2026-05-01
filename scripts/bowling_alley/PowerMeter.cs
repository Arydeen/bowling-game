using Godot;
using System;

public partial class PowerMeter : Control
{
	[Export] public float SweetSpotSize = 0.03f;
	[Export] public float SliderSpeed = 400f; // Increased for noticeable movement per frame
	[Export] public float PowerVal = 0f;
	[Export] public Ball Ball;

	[Export] public NodePath PlayerPath = new NodePath("/root/Player");
	
	// Sound Effects
	[ExportGroup("Sounds")]
	[Export] public AudioStream SoundGood;
	[Export] public AudioStream SoundPerfect;
	[Export] public AudioStream SliderAppear;


	private Sprite2D _slider;
	private ColorRect _greenZone;
	[Export] public float GreenZoneSpeed = 30f;

	private ColorRect _yellowZone;
	[Export] public float YellowZoneSpeed = 60f;

	private ColorRect _blueZone;
	[Export] public float BlueZoneSpeed = 80f;

	private ColorRect _redZone;
	[Export] public float RedZoneSpeed = 100f;

	private PointLight2D _meterlight;
	private AudioStreamPlayer2D _meterSounds;
	private GameManager _gameManager;
	private Node _player;
	
	private Vector2 _sliderPos = new Vector2(0, 0);
	private Vector2 _targetPos; // The "Home" position on screen
	private bool _meterActive = false;
	private bool _movingLeft = false;
	private bool _sliderStop = false;
	private bool _canStop = false;

	public override void _Ready()
	{
		_meterlight = GetNode<PointLight2D>("Spotlight");
		_meterSounds = GetNode<AudioStreamPlayer2D>("MeterSounds");
		_gameManager = GetNode<GameManager>("../GameManager");

		_player = GetNodeOrNull<Node>(PlayerPath);

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

	// HoneyBeer Methods --------------------------------------------------------- //
	private float GetPowerMeterSpeedMult()
	{
		if (_player == null)
			_player = GetNodeOrNull<Node>(PlayerPath);

		if (_player == null)
			return 1f;

		if (!_player.HasMethod("get_power_meter_speed_mult"))
			return 1f;

		Variant v = _player.Call("get_power_meter_speed_mult");
		return Mathf.Clamp((float)(double)v, 0f, 1f);
	}
	// End HoneyBeer Methods ----------------------------------------------------- //


	// Start Meter Show-Hide Functions ------------------------------------------- //
	public void ShowMeter()
	{
		if (_meterActive) return;

		PlaySound(SliderAppear);

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

	public void ActivateSpotlight()
	{
		_meterlight.Enabled = true;
		// _switchNoise.Play();
		Tween tween = CreateTween();
		tween.TweenProperty(_meterlight, "energy", 0.8f, 0.2f).From(0f);
	}

	public void DeactivateSpotlight()
	{
		// _switchNoise.Play();
		Tween tween = CreateTween();
		tween.TweenProperty(_meterlight, "energy", 0f, 0.2f).From(0.8f);
		tween.Finished += () => _meterlight.Enabled = false;
	}
	// End Meter Show-Hide Functions --------------------------------------------- //

	// Start Slider Functions ---------------------------------------------------- //
	public float StopSlider()
	{

		if (IsSweet())
		{
			PlaySound(SoundPerfect);
		}

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

		float honeyBeerMult = GetPowerMeterSpeedMult();
		float adjustedSpeed = SliderSpeed * honeyBeerMult;

		_sliderPos.X += direction * adjustedSpeed * (float)delta;
		
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

	// Helper Functions ---------------------------------------------------------- //
	private void PlaySound(AudioStream stream)
	{
		if (stream == null) return;
		_meterSounds.Stream = stream;
		_meterSounds.Play();
	}
	// End Helper Funcitons ------------------------------------------------------ //

	public override void _Process(double delta)
	{
		// Only process slider logic if meter is active
		MoveSlider(delta);

		if (_meterActive && _canStop && Input.IsActionJustPressed("power_meter_stop"))
		{
			if (_gameManager.InputLock) {return;}
			PowerVal = StopSlider();
		}
	}
}
