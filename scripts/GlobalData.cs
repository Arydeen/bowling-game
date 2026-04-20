using Godot;
using System;

public partial class GlobalData : Node
{
	public static GlobalData Instance { get; private set; }

	public int TotalPins = 0;
	public float PowerUpSpeedBoost = 0f; // Example, I'm not sure how we will actually implement powerups
	public bool KineticBall {get; set;} = false;

	public int Frame = 0;

	public override void _Ready()
	{
		Instance = this;
	}

	public void SpendPind(int amount)
	{
		TotalPins -= amount;
	}

}
