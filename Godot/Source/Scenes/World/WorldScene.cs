using Godot;
using System.Collections.Generic;
using System;

public partial class WorldScene : Node, ISceneTransitionable
{
    private int _difficulty = 1;
    private HUD _HUD;
    private Battler _battler;
    private Level _currentLevel;

    public override void _Ready()
    {
        _HUD = GetNode<HUD>("HUD");
        _currentLevel = GetNode<Level>("Level");
        _battler = GetNode<Battler>("Battler");
        _battler.BattleEnded += (bool playerWon) => this.OnBattleEnded(playerWon);
        GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").Pause("Menu");
        GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").Resume("World");

        // will change
        InitialiseLevel();
    }

    public void Restart()
    {
        // _currentLevel.GetNode("Navigation").Free();
        // Die();
        GetNode<SceneTransition>("WorldSceneTransition").SharedData = new BattleDataContainer() { Difficulty = _difficulty };
        GetNode<SceneTransition>("WorldSceneTransition").Start(SceneTransition.LoadType.AnimatedAuto);
    }

    public void MainMenu()
    {
        // Die();
        GetNode<SceneTransition>("MainMenuSceneTransition").Start(SceneTransition.LoadType.Simple);
    }

    public void OnReceivedSharedData(ISceneTransitionShareableData sharedData)
    {
        if (sharedData is BattleDataContainer difficultyData)
        {
            _difficulty = difficultyData.Difficulty;
        }
    }

    private void InitialiseLevel() // level args etc
    {
        ConnectLevelSignals();
    }

    private void ConnectLevelSignals()
    {
        _currentLevel.BattleCommenced += (characterUnits, hexGrid) => _battler.Init(characterUnits, hexGrid);
    }

    private void OnBattleEnded(bool playerWon) // remove bool if not needed
    {
        _currentLevel.OnBattleEnded();
    }

    // public void Die()
    // {
    //     GetNode<Level>("Level").Die();
    // }

}
