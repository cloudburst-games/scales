using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleScene : Node, ISceneTransitionable
{

    private Random _rand = new();

    [Export]
    private AnimationPlayer _anim;
    [Export]
    private Node2D _cntBattleLevel;
    [Export]
    private BattleHUD _HUD;
    [Export]
    private Battler _battler;
    [Export]
    private Panel _pnlAction;
    [Export]
    private BaseButton _btnIntro;
    [Export]
    private BaseButton _btnChooseSpell;
    [Export]
    private BaseButton _btnEndTurn;
    [Export]
    private BaseButton _btnToggleGrid;
    [Export]
    private BaseButton _btnMenu;

    [Export]
    private Godot.Collections.Array<BaseTextureButton> _actionBtns = new();
    // exporting here doesn#t work so we are unfortunately using magic string
    private PackedScene _characterScene = GD.Load<PackedScene>("res://Source/Actors/CharacterUnit/CharacterUnit.tscn");

    private Godot.Collections.Array<CharacterUnit> _currentCharacters = new();

    private BattleLevel _lvlToLoad;

    private int _difficulty = 1;
    public void OnReceivedSharedData(ISceneTransitionShareableData sharedData)
    {
        if (sharedData is BattleDataContainer battleData)
        {
            _difficulty = battleData.Difficulty;
            NewCharacter((StoryCharacter.StoryCharacterMode)battleData.CharacterSelected).StatusToPlayer = CharacterUnit.StatusToPlayerMode.Player;
        }
    }

    [Export]
    private Godot.Collections.Array<PackedScene> _levelScenePaths = new();

    [Export]
    private int _nextLevel = 0;


    [Export]
    private CursorControl _cursorControl;
    [Export]
    private SpellEffectManager _spellEffectManager;
    public override void _Ready()
    {
        // TESTING
        if (_currentCharacters.Count == 0)
        {
            OnReceivedSharedData(new BattleDataContainer()
            {
                Difficulty = 1,
                CharacterSelected = 0,
            });
        }
        //
        _btnIntro.Pressed += this.OnBtnIntroPressed;
        _HUD.UIPause += (bool paused) => this.OnUIPause(paused);
        // _spellEffectManager.SpellEffectFinished += this.OnSpellEffectFinished;
        _battler.UIBounds = _pnlAction.GetRect();
        _battler.LogBattleText += (string text, bool persist) => _HUD.OnBattleLogEntry(text, persist);

        for (int i = 0; i < _actionBtns.Count; i++)
        {
            int btnIndex = i;
            _actionBtns[i].Pressed += () => _battler.OnActionBtnPressed((Battler.ActionMode)btnIndex);
        }

        _btnChooseSpell.Pressed += _battler.OnBtnChooseSpellPressed;
        _btnEndTurn.Pressed += _battler.OnBtnEndTurnPressed;
        _btnToggleGrid.Pressed += _battler.OnBtnToggleGridPressed;
        _btnMenu.Pressed += _battler.OnBtnMenuPressed;




        LoadLevel();
    }

    // private void OnSpellEffectFinished(SpellEffectData spellEffectData)
    // {
    //     BattleSpell spell = _spellEffectManager.AllSpells[(SpellEffectManager.SpellEffectMode)spellEffectData.AssociatedSpellEffect];

    //     // need to input into a dice roll system - TODO!

    //     _

    // }

    private void OnUIPause(bool paused)
    {
        _battler.ProcessMode = _pnlAction.ProcessMode = paused ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
        _cursorControl.SetCursor(paused ? CursorControl.CursorMode.Select : _cursorControl.GetCursor());
    }

    private CharacterUnit NewCharacter(StoryCharacter.StoryCharacterMode selectedChar)
    {
        CharacterUnit newChar = _characterScene.Instantiate<CharacterUnit>();
        newChar.SetFromJSON(selectedChar);
        newChar.Rand = _rand;
        _currentCharacters.Add(newChar);
        newChar.CastingEffect += _spellEffectManager.OnCastingSpell;
        newChar.Died += _battler.OnCharacterDied;
        return newChar;
    }

    // Player characters must be initialised before this is called
    private async void LoadLevel()
    {
        // if this takes long, consider making a loading animation and then activating animationplayer once complete
        _lvlToLoad = _levelScenePaths[_nextLevel].Instantiate<BattleLevel>();
        _anim.Play("Loading");
        _cntBattleLevel.AddChild(_lvlToLoad);
        _spellEffectManager.CurrentLevel = _lvlToLoad;
        _HUD.SetIntroText(_lvlToLoad.IntroMessage);
        _HUD.SetState(BattleHUD.StateMode.BattleIntro);

        foreach (StoryCharacter.StoryCharacterMode enemyStoryChar in _lvlToLoad.StartingEnemies)
        {
            NewCharacter(enemyStoryChar).StatusToPlayer = CharacterUnit.StatusToPlayerMode.Hostile;
        }
        foreach (StoryCharacter.StoryCharacterMode allyStoryChar in _lvlToLoad.StartingAllies)
        {
            NewCharacter(allyStoryChar).StatusToPlayer = CharacterUnit.StatusToPlayerMode.Allied;
        }

        foreach (CharacterUnit characterUnit in _currentCharacters)
        {
            _lvlToLoad.PlaceCharacterUnit(characterUnit);
            _lvlToLoad.CharacterUnitsContainer.AddChild(characterUnit);
            characterUnit.RemoveObstacle += (playerCharacter, moving) =>
                _lvlToLoad.HexModifier.OnCharacterRemoveObstacle(playerCharacter.GlobalPosition, moving);
        }

        await ToSignal(_anim, AnimationPlayer.SignalName.AnimationFinished);
        _btnIntro.Disabled = false;
        _battler.Init(_currentCharacters, _lvlToLoad.HexGrid);
    }

    private void OnBtnIntroPressed()
    {
        _battler.ProcessMode = ProcessModeEnum.Inherit;
        _pnlAction.ProcessMode = ProcessModeEnum.Inherit;
        _actionBtns[0]._Pressed();
        _battler.PlayerSelectedAction = Battler.ActionMode.Melee;
        _cursorControl.SetCursor(CursorControl.CursorMode.Wait);
    }

    private async void UnloadLevel()
    {
        _anim.Play("Unloading");
        await ToSignal(_anim, AnimationPlayer.SignalName.AnimationFinished);

        // potentially can add script to CntBattleLevel and set the level if this is buggy
        // also note: will have "orphan nodes" if these characterUnits aren't free'd appropriately when no new level afterwards
        Node2D characterUnitsContainer = ((BattleLevel)_cntBattleLevel.GetChild(0)).CharacterUnitsContainer;
        foreach (CharacterUnit characterUnit in ((BattleLevel)_cntBattleLevel.GetChild(0)).CharacterUnitsContainer.GetChildren().Cast<CharacterUnit>())
        {
            // if character is player-controlled then preserve for the next level...
            if (characterUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
            {
                characterUnitsContainer.RemoveChild(characterUnit);
            }
        }

        foreach (Node n in _cntBattleLevel.GetChildren())
        {
            n.QueueFree();
        }
    }

    // initialise the battle by passing in the involved characters and the battle grid
    // make sure the battle loops for all 3 categories of character team
    // implement move action
    // implement attack action
    // implement other actions
    // shrines
    // UI

    // character barks - need to be able to set in inspector an array of barks which consist of: associated battle level,
    // present character required, round from when it can occur, and trigger (including start of round, on taking damage, on dealing damage, etc.),
    // and the bark string itself
    // maybe the data for this should be in the character JSON!

    public override void _Process(double delta)
    {
        base._Process(delta);

        // TESTING
        // if (Input.IsKeyPressed(Key.Space))
        // {
        //     UnloadLevel();
        // }
    }

}
