using Godot;
using Godot.Collections;
using System;
using System.Linq.Expressions;

public partial class Pin : Area2D
{

	public enum PinType { R1, R2, R3, R4 }

	[Export]
	public PinType Type;
	[Export]
	public Array<Texture2D> TextureLibrary;

	[Export]
	public int MaxHealth = 100;

	private int _currentHealth;
	private ProgressBar _healthBar;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		// Texture Handling
		Sprite2D sprite = GetNode<Sprite2D>("Pin_Image");

		int index = (int)Type;

		if (TextureLibrary != null && index < TextureLibrary.Count)
		{
			sprite.Texture = TextureLibrary[index];
		}
		
		// Health Bar Handling
		_currentHealth = MaxHealth;
		_healthBar = GetNode<ProgressBar>("ProgressBar");
		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = _currentHealth;

	}

	public void TakeDamage(int amount)
	{
		_currentHealth -= amount;
		_healthBar.Value = _currentHealth;

		if (_currentHealth <= 0)
		{
			Die();
		}

	}

	public void Die()
	{
		QueueFree();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
