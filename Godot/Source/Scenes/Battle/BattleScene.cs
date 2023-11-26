using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleScene : Node, ISceneTransitionable
{


    [Export]
    private AudioContainer _audioBattleDefeat;

    [Export]
    private float _musicNormalVol = -10f;

    [Export]
    private float _musicQuietVol = -15f;

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
    [Export]
    private BaseTextureButton _btnMainMenu;
    // [Export]
    // private Godot.Collections.Array<BaseTextureButton> _actionBtns = new();
    // exporting here doesn#t work so we are unfortunately using magic string
    private PackedScene _characterScene = GD.Load<PackedScene>("res://Source/Actors/CharacterUnit/CharacterUnit.tscn");
    private List<Perk.PerkMode> _masterPerkPool = new();

    private Godot.Collections.Array<CharacterUnit> _currentCharacters = new();

    private BattleLevel _lvlToLoad;

    private CheckpointData _loadedCheckpointData = null;

    private CntPnlAdventures.DifficultyMode _difficulty = CntPnlAdventures.DifficultyMode.Medium;
    public void OnReceivedSharedData(ISceneTransitionShareableData sharedData)
    {
        if (sharedData is BattleDataContainer battleData)
        {
            _difficulty = (CntPnlAdventures.DifficultyMode)battleData.Difficulty;
            if (battleData.CheckpointData != null)
            {
                GD.Print("loading checkpoint data");
                _loadedCheckpointData = battleData.CheckpointData;
                _nextLevel = battleData.CheckpointData.CurrentLevel;
                _scales.SetFavourOnLoad(battleData.CheckpointData.Favour);
                // do load stuff here
            }
            else
            {
                GD.Print("loading new level");

                // do new stuff here
                CharacterUnit newChar = NewCharacter(StoryCharacter.StoryCharacterMode.Gilgam, CharacterUnit.StatusToPlayerMode.Player);
                Perk.PerkMode selectedPerk = (Perk.PerkMode)battleData.PerkSelected;
                if (!newChar.CharacterData.Perks.Contains(selectedPerk))
                {
                    newChar.CharacterData.Perks.Add(selectedPerk);

                    Perk perk = PerkFactory.GeneratePerk(selectedPerk);
                    // GD.Print(perk.Name);
                    newChar.ApplyPerk(perk);
                }
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
        // if (_currentCharacters.Count == 0)
        // {
        //     OnReceivedSharedData(new BattleDataContainer()
        //     {
        //         Difficulty = 1,
        //         // CharacterSelected = 0,
        //     });
        // }
        //

        _btnIntro.Pressed += this.OnBtnIntroPressed;
        _HUD.UIPause += (bool paused) => this.OnUIPause(paused);
        _pnlAction.ActionUIHint += (int hint) => _HUD.OnPnlActionUIHint((PnlAction.UIHint)hint);
        // _spellEffectManager.SpellEffectFinished += this.OnSpellEffectFinished;
        _spellEffectManager.AreaHitCalculated += _battler.ParseAreaAttack;
        _battler.UIBounds = _pnlAction.GetRect();
        _battler.ScalesBounds = _pnlScales.GetRect();
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
        _cntSpellBook.SpellUIHint += (int spell, bool canAfford) => _HUD.OnSpellBookUIHint(_spellEffectManager.AllSpells[(SpellEffectManager.SpellMode)spell], canAfford);
        _cntSpellBook.ManaReagentUIHint += (SignalValueHolder values, int display) => _HUD.OnSpellBookCostsUIHint(values, display);
        _battleScalesAnim.CurrentAnimation = "Start";

        _adventureStoriesHandler.DefeatStoryFinished += () => GoToMainMenu();
        _adventureStoriesHandler.FinalVictoryStoryFinished += () => GoToMainMenu();
        _adventureStoriesHandler.VictoryPictureStoryFinished += OnVictoryStoryFinished;
        _battleVictory.FavouredGod += (int which, int scalesImpact, CharacterUnit victim, bool persuadeSuccess) => OnVictoryFavouredGod((Scales.FavourMode)which, scalesImpact, victim, persuadeSuccess);
        // _cntCharacterUpgrade.UpgradeFinished += OnBattleVictoryUpgradesFinished;
        _pnlPerkSelect.FinishedSelectingPerks += OnBattleVictoryUpgradesFinished;
        _masterPerkPool = Enum.GetValues(typeof(Perk.PerkMode)).Cast<Perk.PerkMode>().Where(x => x != Perk.PerkMode.None).ToList();
        _btnMainMenu.Pressed += () => GoToMainMenu();
        _HUD.GridDisplayBtnPressed += (int gridDisplayMode) => _battler.ChangeHexDisplayMode((HexGridUserDisplay.DisplayMode)gridDisplayMode);
        // _adventureStoriesHandler.VictoryPictureStoryFinished += () 
        // _adventureStoriesHandler.FinalVictoryStoryFinished += ()

        GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").Pause("Menu");
        GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").Resume("World");

        LoadLevel(_loadedCheckpointData);

    }

    private void GoToMainMenu()
    {
        GetNode<SettingsManager>("BattleHUD/SettingsManager").Exit();
        _HUD.Exit();
        _mainMenuSceneTransition.Start(SceneTransition.LoadType.Simple);
    }

    [Export]
    private PnlPerkSelect _pnlPerkSelect;

    private void OnVictoryStoryFinished(int level)
    {
        // GD.Print("pic story fin");

        // await ToSignal(GetTree().CreateTimer(3), SceneTreeTimer.SignalName.Timeout);
        _btnIntro.Disabled = false;
        // _adventureStoriesHandler.QueueFree();
        // BaseProject.Utils.Node.SetVisibleRecursive(_adventureStoriesHandler, false);
    }

    [Export]
    private AnimationPlayer _loadingAnim;

    private async void OnBattleVictoryUpgradesFinished(System.Collections.Generic.Dictionary<CharacterUnit, Perk[]> characterPerks)
    {
        _cntCharacterUpgrade.Exit();
        if (characterPerks != null)
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
                    // if (!p.Stackable)
                    // {
                    //     _masterPerkPool.Remove(p.CurrentPerk);
                    // }
                }
            }
        }

        // if (_nextLevel < _levelScenePaths.Count) // if we reached the last level dont bother with unloading/loading levels anymore - we won
        // {

        _loadingLevel = true;
        // This saves the current characters and wipes the current character list
        CheckpointData checkpointData = SaveProgress();

        UnloadLevel();
        await ToSignal(this, SignalName.FinishedUnloadingLevel);
        LoadLevel(checkpointData);
        // }

        while (_loadingLevel)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        _btnIntro.Disabled = true;
        _adventureStoriesHandler.DoVictoryStory(_nextLevel - 1, _scales.GetCurrentFavour(), _levelScenePaths.Count - 1);
    }

    private CheckpointData SaveProgress()
    {
        // Save player chacters
        CheckpointData checkpointData = new();
        checkpointData.PlayerCharacters = _currentCharacters
            .Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
            .Select(x => x.CharacterData.PackData())
            .ToList();
        checkpointData.CurrentLevel = _nextLevel;
        checkpointData.Difficulty = (int)_difficulty;
        checkpointData.Favour = _scales.GetFavourOnSave();
        JSONDataHandler dataHandler = new();
        string savePath = $"/Checkpoint/GilgAdventure"; // for future adventures would need to vary this
        dataHandler.SaveToDisk(checkpointData, savePath);
        // wipe the list
        _currentCharacters.ToList()
            .ForEach(x =>
            {
                _currentCharacters.Remove(x);
                x.QueueFree();
            });
        return checkpointData;
    }

    private void OnVictoryFavouredGod(Scales.FavourMode finalFavour, int scalesImpact, CharacterUnit victim, bool persuadeSuccess)
    {
        if (victim != null && victim.CharacterData.Name == "Enkidu")
        {
            victim.StatusToPlayer = CharacterUnit.StatusToPlayerMode.Player;
            victim.InitStatusToPlayer();
        }
        switch (finalFavour)
        {
            case Scales.FavourMode.Balanced:
                if (victim != null && persuadeSuccess)
                {
                    victim.StatusToPlayer = CharacterUnit.StatusToPlayerMode.Player;
                    victim.InitStatusToPlayer();
                }
                OnBattleVictoryUpgradesFinished(null);
                return;
            // break;
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

        GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").AdjustVolume("World", _musicQuietVol);
        _battler.ProcessMode = ProcessModeEnum.Disabled;
        _pnlAction.ProcessMode = ProcessModeEnum.Disabled;
        // _btnIntro.Disabled = true;
        _cursorControl.SetCursor(CursorControl.CursorMode.Select);
        if (playerWon)
        {
            _audioBattleVictory.Play();
            _nextLevel += 1;
            if (_nextLevel == _levelScenePaths.Count)
            {
                _adventureStoriesHandler.DoVictoryStory(_nextLevel - 1, _scales.GetCurrentFavour(), _levelScenePaths.Count - 1);
            }
            else
            {
                _battleVictory.Start(
                    _currentCharacters.Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player).ToList(),
                    _currentCharacters.Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Hostile).ToList(),
                    _scales.GetScaleAnimationTime());
            }
        }
        else
        {
            _audioBattleDefeat.Play();
            _adventureStoriesHandler.DoDefeatStory(_nextLevel);
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
        _HUD.SetSpellBookDisplayedSpells(spells, _battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.Reagents],
            _battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.FocusCharge]);
        _HUD.OnCharacterStartTurn(_battler.CharactersAwaitingTurn[0].CharacterData);
        _hBoxTurnOrder.OnCharacterTurnStart
            (_battler.CharactersAwaitingTurn.Skip(1)
                .Where(x => x.CharacterData.Alive)
                .Select(x => x.CharacterData)
                .ToList(),
            _battler.AllCharacters
                .Where(x => x.CharacterData.Alive)
                .Select(x => x.CharacterData).ToList(),
            _battler.Round + 1);
        // if (_battler.CharactersAwaitingTurn[0].UISelectedSpell != SpellEffectManager.SpellMode.None)
        // {
        SetSpell(_battler.CharactersAwaitingTurn[0].UISelectedSpell);

        _btnActions.OnCharacterTurnStart(_battler.CharactersAwaitingTurn[0]);
        SetFavourForCharacters();

        _battler.CharactersAwaitingTurn[0].UpdateBarHealth();

        if (_battler.CharactersAwaitingTurn[0].StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player && _nextLevel == 0)
        {
            _HUD.TutorialHint();
        }
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

    private CharacterUnit NewCharacter(StoryCharacter.StoryCharacterMode selectedChar, CharacterUnit.StatusToPlayerMode status)
    {
        CharacterUnit newChar = _characterScene.Instantiate<CharacterUnit>();
        newChar.SetFromJSON(selectedChar);
        NewCharacterCommon(newChar, status);
        return newChar;
    }

    private void NewCharacterCommon(CharacterUnit newChar, CharacterUnit.StatusToPlayerMode status)
    {
        // newChar.Rand = _rand;
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
        foreach (Perk.PerkMode p in newChar.CharacterData.Perks.ToList())
        {
            Perk perk = PerkFactory.GeneratePerk(p);
            // GD.Print(perk.Name);
            newChar.ApplyPerk(perk);
        }
        newChar.CharacterData.RangedWeaponEquipped = newChar.CharacterData.RangedWeaponEquipped; // uhh trust me this does something
        newChar.CharacterData.MeleeWeaponEquipped = newChar.CharacterData.MeleeWeaponEquipped; // uhh trust me this does something


    }

    private void NewPlayerCharactersFromSaved(List<CharacterCheckpointData> playerCharacters)
    {
        foreach (var data in playerCharacters)
        {
            CharacterUnit newChar = _characterScene.Instantiate<CharacterUnit>();
            newChar.SetFromData(data);
            NewCharacterCommon(newChar, CharacterUnit.StatusToPlayerMode.Player);
        }

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
    private async void LoadLevel(CheckpointData checkpointData = null)
    {
        // Load level and obstacle data
        if (!_loadingAnim.IsPlaying())
        {
            _loadingAnim.Play("Start");
            await ToSignal(_loadingAnim, AnimationPlayer.SignalName.AnimationFinished);
            _loadingAnim.Play("Loading");
        }
        _lvlToLoad = _levelScenePaths[_nextLevel].Instantiate<BattleLevel>();
        _cntBattleLevel.AddChild(_lvlToLoad);
        _spellEffectManager.CurrentLevel = _lvlToLoad;
        _HUD.SetIntroText(_lvlToLoad.IntroTitle, _lvlToLoad.IntroMessage);
        _HUD.SetState(BattleHUD.StateMode.BattleIntro);

        GD.Print("marking obstacles");
        if (_lvlToLoad.HexTileInterface.StillMarkingObstacles)
        {
            await ToSignal(_lvlToLoad.HexTileInterface, HexTilemapIsometricInterface.SignalName.FinishedMarkingObstacles);
        }
        // Then initailise characters
        GD.Print("initialising characters");

        foreach (StoryCharacter.StoryCharacterMode enemyStoryChar in _lvlToLoad.StartingEnemies)
        {
            NewCharacter(enemyStoryChar, CharacterUnit.StatusToPlayerMode.Hostile);
        }
        foreach (StoryCharacter.StoryCharacterMode allyStoryChar in _lvlToLoad.StartingAllies)
        {
            NewCharacter(allyStoryChar, CharacterUnit.StatusToPlayerMode.Allied);
        }

        if (checkpointData != null)
        {
            // _difficulty = (CntPnlAdventures.DifficultyMode)checkpointData.Difficulty;
            _btnIntro.Disabled = false;
            NewPlayerCharactersFromSaved(checkpointData.PlayerCharacters);
        }

        // make a pool of non plot enemy types that can be used for difficulty setting
        List<StoryCharacter.StoryCharacterMode> nonPlotChars = _lvlToLoad.StartingEnemies
            .Where(charType => charType != StoryCharacter.StoryCharacterMode.Enkidu && charType != StoryCharacter.StoryCharacterMode.Gilgam)
            .ToList();

        if (nonPlotChars.Count == 0)
        {
            GD.Print("Error, lvl needs at least 1 non-plot character for difficulty settings");
        }
        else
        {
            // do difficulty settings here // TBD
            switch (_difficulty)
            {
                case CntPnlAdventures.DifficultyMode.Easy:
                    NewCharacter(nonPlotChars[_rand.Next(0, nonPlotChars.Count)], CharacterUnit.StatusToPlayerMode.Allied);
                    break;
                case CntPnlAdventures.DifficultyMode.Medium:
                    break;
                case CntPnlAdventures.DifficultyMode.Hard:
                    NewCharacter(nonPlotChars[_rand.Next(0, nonPlotChars.Count)], CharacterUnit.StatusToPlayerMode.Hostile);
                    break;
            }
        }

        foreach (CharacterUnit characterUnit in _currentCharacters)
        {
            characterUnit.CurrentLevel = _nextLevel;
            _lvlToLoad.PlaceCharacterUnit(characterUnit);
            _lvlToLoad.CharacterUnitsContainer.AddChild(characterUnit);
            characterUnit.RemoveObstacle += (playerCharacter, moving) =>
                _lvlToLoad.HexModifier.OnCharacterRemoveObstacle(playerCharacter.GlobalPosition, moving);
        }
        // _lvlToLoad.HexModifier.HexObstacleChanged += _battler.RecalculateUserHexes;
        StoreCurrentCharacterPortraits();


        // while (_cntBattleLevel.GetChildCount() == 0)
        // {
        //     await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // }
        // if (_anim.IsPlaying())
        // {
        //     await ToSignal(_anim, AnimationPlayer.SignalName.AnimationFinished);
        // }

        if (_nextLevel == 0)
        {
            _btnIntro.Disabled = false;
        }
        _battler.Init(_currentCharacters, _lvlToLoad.HexGrid, _spellEffectManager.AllSpells);
        _cntSpellBook.Init(_spellEffectManager.AllSpells);
        if (_loadingAnim.IsPlaying())
        {
            _loadingAnim.Play("Stop");
            // await ToSignal(_loadingAnim, AnimationPlayer.SignalName.AnimationFinished);
        }
        _anim.Play("Loading");
        _loadingLevel = false;

        PrintOrphanNodes();

        // orphaned nodes


    }


    private bool _loadingLevel = false;
    [Export]
    private AudioContainer _audioBattleVictory;

    private void OnBtnIntroPressed()
    {
        _battler.ProcessMode = ProcessModeEnum.Inherit;
        _pnlAction.ProcessMode = ProcessModeEnum.Inherit;
        _btnActions.OnActionBtnPressed(Battler.ActionMode.Melee);
        // _battler.PlayerSelectedAction = Battler.ActionMode.Melee;
        _cursorControl.SetCursor(CursorControl.CursorMode.Wait);
        _battleScalesAnim.Seek(_scales.GetScaleAnimationTime(), true);
        if (!_pnlScales.Visible)
        {
            _animScalesStart.Play("Start");
        }
        GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").AdjustVolume("World", _musicNormalVol);

        // TESTING
        // OnBattleEnded(true);
        //
    }

    private async void UnloadLevel()
    {
        // _anim.Play("Unloading");
        // _battler.Exit();

        _loadingAnim.Play("Start");
        await ToSignal(_loadingAnim, AnimationPlayer.SignalName.AnimationFinished);
        _loadingAnim.Play("Loading");
        // await ToSignal(_anim, AnimationPlayer.SignalName.AnimationFinished);

        // potentially can add script to CntBattleLevel and set the level if this is buggy
        // also note: will have "orphan nodes" if these characterUnits aren't free'd appropriately when no new level afterwards
        // Node2D characterUnitsContainer = ((BattleLevel)_cntBattleLevel.GetChild(0)).CharacterUnitsContainer;
        // foreach (CharacterUnit characterUnit in ((BattleLevel)_cntBattleLevel.GetChild(0)).CharacterUnitsContainer.GetChildren().Cast<CharacterUnit>())
        // {
        //     // if character is player-controlled then preserve for the next level...
        //     if (characterUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
        //     {
        //         characterUnitsContainer.RemoveChild(characterUnit);
        //     }
        // }

        foreach (Node n in _cntBattleLevel.GetChildren())
        {
            n.QueueFree();
        }
        while (_cntBattleLevel.GetChildCount() > 0)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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

    // public override void _Process(double delta)
    // {
    //     base._Process(delta);

    //     // TESTING
    //     // if (Input.IsKeyPressed(Key.Space))
    //     // {
    //     //     UnloadLevel();
    //     // }
    // }

    // public override void _Input(InputEvent ev)
    // {
    //     if (!ev.IsEcho() && ev.IsPressed() && ev is InputEventKey evk)
    //     {
    //         if (evk.Keycode == Key.Space)
    //         {
    //             OnBattleEnded(true);
    //         }
    //     }
    // }

}
