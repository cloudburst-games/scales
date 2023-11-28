using Godot;
using System;

public partial class TurnOrderRoundSeparator : Panel
{
	[Export]
	private Label _lbl;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public void SetRound(int round)
	{
		_lbl.Text = "Round\n" + round.ToString();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
