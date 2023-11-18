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
    [Signal]
    public delegate void NewPressedEventHandler(int adventureSelected, int difficultySelected);
    [Signal]
    public delegate void ContinuePressedEventHandler(int adventureSelected, int difficultySelected);

    public enum AdventureSelectedMode { Gilgamesh }

    private DifficultyMode _difficultySelected;

    public enum DifficultyMode { Easy, Medium, Hard }

    public override void _Ready()
    {
        ConnectDifficultySignals();
        _btnContinue.Disabled = !CheckpointDataExists();
        _btnContinue.Pressed += () => EmitSignal(SignalName.ContinuePressed, (int)AdventureSelectedMode.Gilgamesh, (int)_difficultySelected);
        _btnNew.Pressed += () => EmitSignal(SignalName.NewPressed, (int)AdventureSelectedMode.Gilgamesh, (int)_difficultySelected);
        _btnClose.Pressed += () => Visible = false;
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
