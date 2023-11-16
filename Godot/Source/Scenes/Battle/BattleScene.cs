using Godot;
using Godot.Collections;
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
    private PnlAction _pnlAction;
    [Export]
    private BaseButton _btnIntro;
    [Export]
    private BaseButton _btnChooseSpell;
    [Export]
    private BaseButton _btnEndTurn;
    [Export]
    private BaseButton _btnMenu;
    [Export]
    private BtnActions _btnActions;
    [Export]
    private AnimationPlayer _battleScalesAnim;
    // [Export]
    // private Godot.Collections.Array<BaseTextureButton> _actionBtns = new();
    // exporting here doesn#t work so we are unfortunately using magic string
    private PackedScene _characterScene = GD.Load<PackedScene>("res://Source/Actors/CharacterUnit/CharacterUnit.tscn");
    private List<Perk.PerkMode> _masterPerkPool = new();

    private Godot.Collections.Array<CharacterUnit> _currentCharacters = new();

    private BattleLevel _lvlToLoad;

    private CntPnlAdventures.DifficultyMode _difficulty = CntPnlAdventures.DifficultyMode.Medium;
    public void OnReceivedSharedData(ISceneTransitionShareableData sharedData)
    {
        if (sharedData is BattleDataContainer battleData)
        {
            _difficulty = (CntPnlAdventures.DifficultyMode)battleData.Difficulty;
            if (battleData.CheckpointData != null)
            {
                GD.Print("loading checkpoint data");
                // do load stuff here
            }
            else
            {
                GD.Print("loading new level");
                // do new stuff here
                NewCharacter(StoryCharacter.StoryCharacterMode.Gilgam, CharacterUnit.StatusToPlayerMode.Player);
            }
            GD.Print("difficulty is: ", _difficulty);

            // NewCharacter((StoryCharacter.StoryCharacterMode)3, CharacterUnit.StatusToPlayerMode.Player);
        }
    }

    [Export]
    private Godot.Collections.Array<PackedScene> _levelScenePaths = new();

    [Export]
    private int _nextLevel = 0;
    [Export]
    private CntSpellBook _cntSpellBook;

    [Export]
    private CursorControl _cursorControl;
    [Export]
    private SpellEffectManager _spellEffectManager;
    [Export]
    private HBoxTurnOrder _hBoxTurnOrder;
    [Export]
    private PnlCharacterInfo _pnlCharacterInfo;

    private Scales _scales = new();
    [Export]
    private Panel _pnlScales;
    [Export]
    private AnimationPlayer _animScalesStart;
    [Export]
    private AdventureStoriesHandler _adventureStoriesHandler; // in future with multiple adventures this will need to be set dynamically
    [Export]
    private BattleVictory _battleVictory;
    [Export]
    private SceneTransition _mainMenuSceneTransition;
    [Export]
    private CntCharacterUpgrade _cntCharacterUpgrade;

    [Signal]
    public delegate void FinishedUnloadingLevelEventHandler();
    public override void _Ready()
    {
        // TESTING
        if (_currentCharacters.Count == 0)
        {
            OnReceivedSharedData(new BattleDataContainer()
            {
                Difficulty = 1,
                // CharacterSelected = 0,
            });
        }
        //

        _btnIntro.Pressed += this.OnBtnIntroPressed;
        _HUD.UIPause += (bool paused) => this.OnUIPause(paused);
        _pnlAction.ActionUIHint += (int hint) => _HUD.OnPnlActionUIHint((PnlAction.UIHint)hint);
        // _spellEffectManager.SpellEffectFinished += this.OnSpellEffectFinished;
        _spellEffectManager.AreaHitCalculated += _battler.ParseAreaAttack;
        _battler.UIBounds = _pnlAction.GetRect();
        _battler.LogBattleText += (string text, bool persist) => _HUD.OnBattleLogEntry(text, persist);
        _battler.HUDActionRequested += _HUD.SetState;
        _battler.TurnStarted += this.OnCharacterTurnStarted;//_HUD.OnTurnStarted;
        _battler.RoundStarted += this.OnRoundStarted;
        _battler.AreaAttackParsed += _spellEffectManager.OnCastingSpell;
        _battler.HintClickedCharacter += _HUD.OnHintClickCharacter;
        _battler.BattleEnded += this.OnBattleEnded;
        _btnActions.ActionBtnPressed += _battler.OnActionBtnPressed; // 1. Melee 2. Shoot 3. Cast spell 4. Move
        _btnActions.CastSpellActionBtnPressedButNoSpellActive += () => _HUD.SetState(BattleHUD.StateMode.SpellBookOpened);

        _btnChooseSpell.Pressed += _battler.OnBtnChooseSpellPressed;
        _btnEndTurn.Pressed += _battler.OnBtnEndTurnPressed;
        _btnMenu.Pressed += _battler.OnBtnMenuPressed;

        _cntSpellBook.SpellBtnPressed += this.OnSpellSelected;
        _cntSpellBook.SpellUIHint += (int spell) => _HUD.OnSpellBookUIHint(_spellEffectManager.AllSpells[(SpellEffectManager.SpellMode)spell]);
        _battleScalesAnim.CurrentAnimation = "Start";

        _adventureStoriesHandler.DefeatStoryFinished += () => _mainMenuSceneTransition.Start(SceneTransition.LoadType.Simple);
        _adventureStoriesHandler.FinalVictoryStoryFinished += () => _mainMenuSceneTransition.Start(SceneTransition.LoadType.Simple);
        _adventureStoriesHandler.VictoryPictureStoryFinished += OnVictoryStoryFinished;
        _battleVictory.FavouredGod += (int which, int scalesImpact, CharacterUnit victim) => OnVictoryFavouredGod((Scales.FavourMode)which, scalesImpact, victim);
        _cntCharacterUpgrade.UpgradeFinished += OnBattleVictoryUpgradesFinished;
        _masterPerkPool = Enum.GetValues(typeof(Perk.PerkMode)).Cast<Perk.PerkMode>().ToList();
        // _adventureStoriesHandler.VictoryPictureStoryFinished += () 
        // _adventureStoriesHandler.FinalVictoryStoryFinished += ()

        LoadLevel();
    }

    private void OnVictoryStoryFinished(int level)
    {
        // GD.Print("pic story fin");

        // await ToSignal(GetTree().CreateTimer(3), SceneTreeTimer.SignalName.Timeout);
        _btnIntro.Disabled = false;
        // _adventureStoriesHandler.QueueFree();
        // BaseProject.Utils.Node.SetVisibleRecursive(_adventureStoriesHandler, false);
    }

    private async void OnBattleVictoryUpgradesFinished(Godot.Collections.Dictionary<CharacterUnit, Array<Perk>> characterPerks)
    {
        // give characters their perks in the perkmode list
        foreach (CharacterUnit cUnit in characterPerks.Keys)
        {
            foreach (Perk p in characterPerks[cUnit])
            {
                if (p == null)
                {
                    continue;
                }
                cUnit.CharacterData.Perks.Add(p.CurrentPerk);
                cUnit.ApplyPerk(p);
                if (!p.Stackable)
                {
                    _masterPerkPool.Remove(p.CurrentPerk);
                }
            }
        }

        // Remove non player characters
        _currentCharacters.ToList()
            .Where(x => x.StatusToPlayer != CharacterUnit.StatusToPlayerMode.Player)
            .ToList()
            .ForEach(x =>
            {
                _currentCharacters.Remove(x);
                x.QueueFree();
            });
        _adventureStoriesHandler.DoVictoryStory(_nextLevel, _scales.GetCurrentFavour(), _levelScenePaths.Count - 1);
        _nextLevel += 1;
        // TODO - increment level unless max level, in which case this shouldn't be reached (final cutscene will have played instead)
        // TODO - save progress

        if (_nextLevel == _levelScenePaths.Count) // we reached the last level so dont bother with unloading/loading levels anymore
        {
            return;
        }
        UnloadLevel();
        await ToSignal(this, SignalName.FinishedUnloadingLevel);
        LoadLevel();


    }

    private void OnVictoryFavouredGod(Scales.FavourMode finalFavour, int scalesImpact, CharacterUnit victim)
    {
        switch (finalFavour)
        {
            case Scales.FavourMode.Balanced:
                if (victim != null)
                {
                    victim.StatusToPlayer = CharacterUnit.StatusToPlayerMode.Player;
                }
                break;
            case Scales.FavourMode.Shamash:
                _scales.FavourShamash(scalesImpact);
                break;
            case Scales.FavourMode.Ishtar:
                _scales.FavourIshtar(scalesImpact);
                break;
        }

        _battleScalesAnim.Seek(_scales.GetScaleAnimationTime(), true);


        // Upgrade player characters, with perks selected that:
        // - Are not currently in use by a player character unless they are stackable
        // - Who have the patron of the pleased God (or neither were pleased)
        // - Which are not powerful unless one of the gods were pleased
        // - Ordered randomly
        // - Limited to 3 (or 6 if a God was pleased)
        _cntCharacterUpgrade.Start(
            _currentCharacters
                .Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
                .ToList(),
            _masterPerkPool
                .Where(x => (!_currentCharacters
                    .Where(c => c.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
                    .SelectMany(c => c.CharacterData.Perks)
                    .ToList()
                    .Contains(x)) || PerkFactory.GeneratePerk(x).Stackable)
                .Select(x => PerkFactory.GeneratePerk(x))
                .Where(x => finalFavour == Scales.FavourMode.Balanced || x.Patron == finalFavour)
                .Where(x => finalFavour != Scales.FavourMode.Balanced || !x.Powerful)
                .OrderBy(x => _currentCharacters[0].Rand.Next())
                .Take(finalFavour == Scales.FavourMode.Balanced ? 3 : 6)
                .ToList(),
            finalFavour == Scales.FavourMode.Balanced ? 1 : 2 // Allow 2 per character if a God was pleased
        );
    }

    private void OnBattleEnded(bool playerWon)
    {
        _battler.ProcessMode = ProcessModeEnum.Disabled;
        _pnlAction.ProcessMode = ProcessModeEnum.Disabled;
        _btnIntro.Disabled = true;
        _cursorControl.SetCursor(CursorControl.CursorMode.Select);
        if (playerWon)
        {
            _battleVictory.Start(
                _currentCharacters.Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player).ToList(),
                _currentCharacters.Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Hostile).ToList(),
                _scales.GetScaleAnimationTime());
        }
        else
        {
            _adventureStoriesHandler.DoDefeatStory();
        }
    }

    private void OnRoundStarted()
    {
        SetFavourForCharacters();
    }

    private void SetFavourForCharacters()
    {

        _currentCharacters
            .Where(x => x.CharacterData.Alive)
            .Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
            .ToList()
            .ForEach(x => x.OnBattleFavour(_scales.GetCurrentFavour(), _scales.IsExtreme()));
    }

    private void StoreCurrentCharacterPortraits()
    {
        System.Collections.Generic.Dictionary<string, Texture2D> charPortDict = new();
        // IEnumerable<StoryCharacterData> characterDataList = allCharacters.Select(x => x.CharacterData);
        foreach (StoryCharacterData data in _currentCharacters.Select(x => x.CharacterData))
        {
            Texture2D tex = GD.Load<Texture2D>(data.PortraitPath);
            charPortDict[data.Name] = tex;
        }
        // _hBoxTurnOrder.Initialise()
        _pnlCharacterInfo.StoreAllPortraits(charPortDict);
        _hBoxTurnOrder.StoreAllPortraits(charPortDict);
    }


    private void OnCharacterTurnStarted(Array<SpellEffectManager.SpellMode> spells)
    {
        _HUD.SetSpellBookDisplayedSpells(spells);
        _HUD.OnCharacterStartTurn(_battler.CharactersAwaitingTurn[0].CharacterData);
        _hBoxTurnOrder.OnCharacterTurnStart
            (_battler.CharactersAwaitingTurn.Skip(1)
                .Where(x => x.CharacterData.Alive)
                .Select(x => x.CharacterData)
                .ToList(),
            _battler.AllCharacters
                .Where(x => x.CharacterData.Alive)
                .Select(x => x.CharacterData).ToList());
        // if (_battler.CharactersAwaitingTurn[0].UISelectedSpell != SpellEffectManager.SpellMode.None)
        // {
        SetSpell(_battler.CharactersAwaitingTurn[0].UISelectedSpell);

        _btnActions.OnCharacterTurnStart(_battler.CharactersAwaitingTurn[0]);
        SetFavourForCharacters();


    }


    private void OnSpellSelected(SpellEffectManager.SpellMode spellSelected)
    {
        _battler.SetPlayerAction(Battler.ActionMode.Cast);
        SetSpell(spellSelected);
        _btnActions.OnActionBtnPressed(Battler.ActionMode.Cast);
        _HUD.SetState(BattleHUD.StateMode.SpellBookClosed);
    }

    private void SetSpell(SpellEffectManager.SpellMode spellSelected)
    {
        // _btnActions.SetActionBtnToSpellTexture(spellSelected);
        _battler.SetCharacterLastSelectedSpell(spellSelected);
        _btnActions.SetCastBtnTexture(spellSelected);
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

    private void NewCharacter(StoryCharacter.StoryCharacterMode selectedChar, CharacterUnit.StatusToPlayerMode status)
    {
        CharacterUnit newChar = _characterScene.Instantiate<CharacterUnit>();
        newChar.SetFromJSON(selectedChar);
        newChar.Rand = _rand;
        newChar.StatusToPlayer = status;
        _currentCharacters.Add(newChar);
        newChar.CastingEffect += _spellEffectManager.OnCastingSpellStart;
        if (status == CharacterUnit.StatusToPlayerMode.Player)
        {
            newChar.CastingEffect += this.CastingSpellFavourEffect;
        }
        newChar.CastingEffect += (SpellEffectManager.Spell spell) => _HUD.UpdateBars(newChar.CharacterData);
        newChar.Died += _battler.OnCharacterDied;
        newChar.MovedButStillHaveAP += _battler.OnMovedButStillHaveAP;
        newChar.CharacterDataTreeLink.RoundEffectApplied += (CharacterRoundEffect roundEffect) => _HUD.OnCharacterRoundEffectApplied(newChar, roundEffect);
        newChar.CharacterDataTreeLink.RoundEffectApplied += (CharacterRoundEffect roundEffect) => _battler.OnCharacterRoundEffectApplied(newChar, roundEffect);
        newChar.CharacterDataTreeLink.RoundEffectEnded += (CharacterRoundEffect roundEffect) => _HUD.OnCharacterRoundEffectFaded(newChar, roundEffect);
        newChar.CharacterDataTreeLink.RoundEffectEnded += (CharacterRoundEffect roundEffect) => _battler.OnCharacterRoundEffectFaded(newChar, roundEffect);
        newChar.TakingDamage += _HUD.OnCharacterTakingDamage;
        // return newChar;
    }

    private void CastingSpellFavourEffect(SpellEffectManager.Spell spell)
    {

        if (spell.Patron == SpellEffectManager.Spell.PatronMode.Ishtar)
        {
            _scales.FavourIshtar();
        }
        else if (spell.Patron == SpellEffectManager.Spell.PatronMode.Shamash)
        {
            _scales.FavourShamash();
        }
        SetFavourForCharacters();
        _battleScalesAnim.Seek(_scales.GetScaleAnimationTime(), true);
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
            NewCharacter(enemyStoryChar, CharacterUnit.StatusToPlayerMode.Hostile);
        }
        foreach (StoryCharacter.StoryCharacterMode allyStoryChar in _lvlToLoad.StartingAllies)
        {
            NewCharacter(allyStoryChar, CharacterUnit.StatusToPlayerMode.Allied);
        }

        foreach (CharacterUnit characterUnit in _currentCharacters)
        {
            _lvlToLoad.PlaceCharacterUnit(characterUnit);
            _lvlToLoad.CharacterUnitsContainer.AddChild(characterUnit);
            characterUnit.RemoveObstacle += (playerCharacter, moving) =>
                _lvlToLoad.HexModifier.OnCharacterRemoveObstacle(playerCharacter.GlobalPosition, moving);
        }
        // _lvlToLoad.HexModifier.HexObstacleChanged += _battler.RecalculateUserHexes;
        StoreCurrentCharacterPortraits();

        await ToSignal(_anim, AnimationPlayer.SignalName.AnimationFinished);
        if (_nextLevel == 0)
            _btnIntro.Disabled = false;
        _battler.Init(_currentCharacters, _lvlToLoad.HexGrid, _spellEffectManager.AllSpells);
    }

    private void OnBtnIntroPressed()
    {
        _battler.ProcessMode = ProcessModeEnum.Inherit;
        _pnlAction.ProcessMode = ProcessModeEnum.Inherit;
        _btnActions.OnActionBtnPressed(Battler.ActionMode.Melee);
        _battler.PlayerSelectedAction = Battler.ActionMode.Melee;
        _cursorControl.SetCursor(CursorControl.CursorMode.Wait);
        _battleScalesAnim.Seek(_scales.GetScaleAnimationTime(), true);
        if (!_pnlScales.Visible)
        {
            _animScalesStart.Play("Start");
        }

        // TESTING
        // OnBattleEnded(true);
        //
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
        EmitSignal(SignalName.FinishedUnloadingLevel);

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
