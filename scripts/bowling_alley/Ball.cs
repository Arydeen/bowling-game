using Godot;
using System;

public partial class Ball : CharacterBody2D
{
	[Export] public float Speed = 30.0f;
	private Vector2 _velocity = Vector2.Zero;

	public void CheckForHits()
	{
		Area2D hitbox = GetNode<Area2D>("Hitbox");

		var bodies = hitbox.GetOverlappingAreas();

		foreach (Area2D area in bodies)
		{
			if (area is Pin pin)
			{
				pin.TakeDamage(10);
			}
		}
	}

	public void FadeOutAndRemove()
	{
		// 1. Create a Tween object
		Tween tween = CreateTween();

		// 2. Tell the tween to change the "modulate" property
		// Change alpha over a duration of 0.5 seconds
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.5f);

		// 3. Automatically delete the ball once the fade is finished
		tween.Finished += () => QueueFree();
	}
	public override void _PhysicsProcess(double delta)
	{

		CheckForHits();

		float targetY = 81.0f;

		if (GlobalPosition.Y <= targetY)
		{
			
			// 2. Stop movement
			Velocity = Vector2.Zero;
			SetPhysicsProcess(false); 

			// 3. Start the fade!
			FadeOutAndRemove();

		}
		// Example: Move the ball upward (toward pins)
		Velocity = new Vector2(0, -Speed);
		
		// This handles the actual movement and pixel-snapping
		MoveAndSlide();
	}

	public override void _Process(double delta)
	{
		// Example: The higher the ball is on screen, the smaller it gets.
		// You'll need to tune these numbers to your specific lane size.
		float startY = 169; // Bottom of lane
		float endY = 81;   // Top of lane (the pins)
		float minScale = 0.5f;
		float maxScale = 1.0f;

		// Remap the current Y position to a scale value
		float t = Mathf.Remap(GlobalPosition.Y, endY, startY, minScale, maxScale);
		Scale = new Vector2(t, t);
	}

}
