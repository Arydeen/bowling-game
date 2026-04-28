using Godot;
using System;

public partial class GameCamera : Camera2D
{
	
	private float _shakeIntensity = 0.0f;
	private float _shakeDamping = 7.0f;

	public override void _Process(double delta)
	{
		if (_shakeIntensity > 0)
		{
			// Apply random offset
			Offset = new Vector2(
				(float)GD.RandRange(-_shakeIntensity, _shakeIntensity),
				(float)GD.RandRange(-_shakeIntensity, _shakeIntensity)
			);

			// Gradually reduce the shake
			_shakeIntensity = Mathf.MoveToward(_shakeIntensity, 0, (float)delta * _shakeDamping * _shakeIntensity);
			
			if (_shakeIntensity <= 0.1f)
			{
				_shakeIntensity = 0;
				Offset = Vector2.Zero;
			}
		}
	}

	public void Shake(float intensity)
	{
		_shakeIntensity = intensity;
	}
}
