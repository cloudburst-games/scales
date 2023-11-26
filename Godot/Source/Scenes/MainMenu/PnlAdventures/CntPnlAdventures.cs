using Godot;
using System;

public partial class CntPnlAdventures : Control
{
	[Export]
	private BaseTextureButton _btnEasy;
	[Export]
	private BaseTextureButton _btnMedium;
	[Export]
	private BaseTextureButton _btnHard;
	[Export]
	private BaseTextureButton _btnContinue;
	[Export]
	private BaseTextureButton _btnNew;
	[Export]
	private BaseTextureButton _btnClose;
	[Export]
	private BaseTextureButton _btnClosePerks;
	[Signal]
	public delegate void NewPressedEventHandler(int adventureSelected, int difficultySelected, int perkSelected);
	[Signal]
	public delegate void ContinuePressedEventHandler(int adventureSelected, int difficultySelected);

	[Export]
	private BaseTextureButton _btnSolarFlare;
	[Export]
	private BaseTextureButton _btnElixirVigour;
	[Export]
	private BaseTextureButton _btnSling;
	[Export]
	private BaseTextureButton _btnFinalNew;
	[Export]
	private BasePanel _pnlPerks;

	public enum AdventureSelectedMode { Gilgamesh }

	private DifficultyMode _difficultySelected = DifficultyMode.Medium;
	private Perk.PerkMode _perkSelected = Perk.PerkMode.Sling;

	public enum DifficultyMode { Easy, Medium, Hard }

	public override void _Ready()
	{
		ConnectDifficultySignals();
		ConnectPerkSignals();
		_btnContinue.Disabled = !CheckpointDataExists();
		_btnContinue.Pressed += () => EmitSignal(SignalName.ContinuePressed, (int)AdventureSelectedMode.Gilgamesh, (int)_difficultySelected);
		_btnNew.Pressed += () => _pnlPerks.Open(); //EmitSignal(SignalName.NewPressed, (int)AdventureSelectedMode.Gilgamesh, (int)_difficultySelected, (int)_perkSelected);
		_btnFinalNew.Pressed += OnFinalNew;
		_btnClose.Pressed += () =>
		{
			_pnlPerks.Close();
			Visible = false;
		};
		_btnClosePerks.Pressed += () =>
		{
			_pnlPerks.Close();
			// Visible = false;
		};
	}

	private void ConnectPerkSignals()
	{
		_btnSling.Pressed += () => _perkSelected = Perk.PerkMode.Sling;
		_btnElixirVigour.Pressed += () => _perkSelected = Perk.PerkMode.ElixirOfVigour;
		_btnSolarFlare.Pressed += () => _perkSelected = Perk.PerkMode.SolarFlare;
	}

	private void OnFinalNew()
	{
		EmitSignal(SignalName.NewPressed, (int)AdventureSelectedMode.Gilgamesh, (int)_difficultySelected, (int)_perkSelected);
	}

	private void ConnectDifficultySignals()
	{
		_btnEasy.Pressed += () => _difficultySelected = DifficultyMode.Easy;
		_btnMedium.Pressed += () => _difficultySelected = DifficultyMode.Medium;
		_btnHard.Pressed += () => _difficultySelected = DifficultyMode.Hard;
	}

	private bool CheckpointDataExists()
	{
		string savePath = $"/Checkpoint/GilgAdventure"; // for future adventures would need to vary this
		string absolutePath = OS.GetUserDataDir() + savePath;
		return System.IO.File.Exists(absolutePath);
	}

}
