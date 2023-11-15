// This needs to remain modular in case it is to be used outside of a separate BattleScene
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Battler : Node2D
{

    public HexGridUserDisplay.DisplayMode CurrentDisplayMode = HexGridUserDisplay.DisplayMode.ShowContextualHexes;
    public SpellEffectManager.SpellMode PlayerSelectedSpell { get; set; }

    public enum ActionMode { Melee, Shoot, Cast, Move, Hint, Invalid, None }
    [Export]
    public CursorControl CursorControl { get; set; }
    [Export]
    private HexGridUserDisplay _hexGridUserDisplay;

    [Export]
    public Color ActiveCharacterOutline { get; set; } = new Color(1, 1, 0);

    [Export]
    public Color EnemiesOutline { get; set; } = new Color(1, 0, 0);

    [Export]
    public Color FriendliesOutline { get; set; } = new Color(0, 1, 1);

    [Export]
    public Color NeutralsOutline { get; set; } = new Color(0, 0, 1);



    public Rect2 UIBounds { get; set; } = new(new Vector2(0, 0), new Vector2(0, 0));
    public List<CharacterUnit> AllCharacters { get; set; } = new();
    public List<CharacterUnit> CharactersAwaitingTurn { get; set; }

    public HexGrid BattleGrid { get; set; }

    public enum BattleMode { Starting, Idle, Processing, Ending }
    private BattleState _battleState;
    private AnimationPlayer _anim;
    private Callable _battleTurnEndedCallable;
    public int Round { get; private set; } = 1;

    // the action that the player selects via the UI
    public ActionMode PlayerSelectedAction { get; set; } = ActionMode.Move;

    public Dictionary<SpellEffectManager.SpellMode, SpellEffectManager.Spell> AllSpells { get; set; }

    [Signal]
    public delegate void AnimationFinishedEventHandler(string animName);
    [Signal]
    public delegate void BattleEndedEventHandler(bool playerWon);
    [Signal]
    public delegate void LogBattleTextEventHandler(string text, bool persist);
    [Signal]
    public delegate void HUDActionRequestedEventHandler(BattleHUD.StateMode hudState);

    [Signal]
    public delegate void TurnStartedEventHandler(Godot.Collections.Array<SpellEffectManager.SpellMode> spells);

    [Signal]
    public delegate void RoundStartedEventHandler();

    public override void _Ready()
    {
        _battleTurnEndedCallable = new Callable(this, nameof(Battler.BattleTurnEnded));
        _anim = GetNode<AnimationPlayer>("BattleHUD/Control/Anim");
        _anim.AnimationFinished += (animName) => this.OnAnimationFinished(animName);
    }



    private void OnAnimationFinished(string animName)
    {
        EmitSignal(SignalName.AnimationFinished, animName);
    }

    public void Init(Godot.Collections.Array<CharacterUnit> involvedCharacters, HexGrid battleGrid, Dictionary<SpellEffectManager.SpellMode, SpellEffectManager.Spell> allSpells)
    {
        BattleGrid = battleGrid;
        AllSpells = allSpells;

        _hexGridUserDisplay.Init(BattleGrid);

        foreach (CharacterUnit cUnit in involvedCharacters)
        {
            cUnit.BattleTargetPosition = battleGrid.GetCorrectedWorldPosition(cUnit.GlobalPosition);
            if (!cUnit.IsConnected(CharacterUnit.SignalName.BattleTurnEnded, _battleTurnEndedCallable))
            {
                cUnit.Connect(CharacterUnit.SignalName.BattleTurnEnded, _battleTurnEndedCallable);
            }
        }

        AllCharacters = involvedCharacters.ToList();

        SetState(BattleMode.Starting); // this is when character obstacles are made
    }

    public void RecalculateUserHexes()
    {
        if (_battleState != null)
        {
            _battleState.RecalculateUserHexes();
        }

    }

    public List<Vector2> GetAllNonObstacleGridPositions()
    {
        List<Vector2> allGridPositions = new List<Vector2>();

        foreach (KeyValuePair<Vector2, Hexagon> kv in BattleGrid.Cells)
        {
            if (kv.Value.Obstacle)
            {
                continue;
            }

            allGridPositions.Add(kv.Key);
        }

        return allGridPositions;
    }

    public void ToggleGrid(bool visible)
    {
        _hexGridUserDisplay.Visible = visible;
    }

    public void SetGridUserHexes(List<Vector2> validMoveHexes, List<Vector2> validHalfMoveHexes, HexGridUserDisplay.DisplayMode displayMode)
    {
        List<Vector2> allGridPositions = GetAllNonObstacleGridPositions();

        _hexGridUserDisplay.SetSprites(validMoveHexes, validHalfMoveHexes, allGridPositions);

        switch (displayMode)
        {
            case HexGridUserDisplay.DisplayMode.HideAllHexes:
                _hexGridUserDisplay.HideAllHexes();
                break;
            case HexGridUserDisplay.DisplayMode.ShowAllHexes:
                _hexGridUserDisplay.ShowHexes(allGridPositions);
                break;
            case HexGridUserDisplay.DisplayMode.ShowContextualHexes:
                _hexGridUserDisplay.ShowContextualHexes(validMoveHexes, validHalfMoveHexes);
                break;

        }
    }

    // public bool AreHexesHidden(HexGridUserDisplay.DisplayMode displayMode, List<Vector2> moveHexes, List<Vector2> allHexes)
    // {
    //     return _hexGridUserDisplay.AreHexesHidden(displayMode, moveHexes, allHexes);
    // }

    internal void SetOutlineColours()
    {
        foreach (CharacterUnit cUnit in _battleState.GetAliveUnits())
        {
            if (cUnit == CharactersAwaitingTurn[0])
            {
                // GD.Print(cUnit.CharacterData.Name);
                cUnit.SetSpriteOutlineColour(ActiveCharacterOutline);
            }
            else if (cUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player || cUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Allied)
            {
                cUnit.SetSpriteOutlineColour(FriendliesOutline);
            }
            else if (cUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Hostile)
            {
                cUnit.SetSpriteOutlineColour(EnemiesOutline);
            }
            else
            {
                cUnit.SetSpriteOutlineColour(NeutralsOutline);
            }


        }
    }

    public void AddCharacter(CharacterUnit character)
    {
        AllCharacters.Add(character);
    }

    public void SetState(BattleMode battleMode)
    {
        switch (battleMode)
        {
            case BattleMode.Starting:
                _battleState = new StartingBattleState(this);
                break;
            case BattleMode.Idle:
                _battleState = new IdleBattleState(this);
                break;
            case BattleMode.Processing:
                _battleState = new ProcessingBattleState(this);
                break;
            case BattleMode.Ending:
                _battleState = new EndingBattleState(this);
                break;
        }
    }

    public BattleMode GetState()
    {
        return _battleState is StartingBattleState ? BattleMode.Starting : _battleState is IdleBattleState ?
            BattleMode.Idle : _battleState is ProcessingBattleState ? BattleMode.Processing : BattleMode.Ending;
    }

    public void BattleTurnEnded(CharacterUnit cUnit)
    {

        // Is there a winner?
        if (!AreAnyAlive(CharacterUnit.StatusToPlayerMode.Hostile) || (!AreAnyAlive(CharacterUnit.StatusToPlayerMode.Player) && !AreAnyAlive(CharacterUnit.StatusToPlayerMode.Allied)))
        {
            GD.Print("end the battle!");
            SetState(BattleMode.Ending);
        }
        else
        {
            // GD.Print("next turn");
            CharactersAwaitingTurn.Remove(cUnit);
            if (CharactersAwaitingTurn.Count == 0)
            {
                NewRound();
            }
            SetState(BattleMode.Idle);
        }
    }

    public bool AreAnyAlive(CharacterUnit.StatusToPlayerMode statusToPlayer)
    {
        foreach (CharacterUnit cUnit in AllCharacters)
        {
            if (cUnit.StatusToPlayer == statusToPlayer)
            {
                if (cUnit.CharacterData.Alive)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void NewRound()
    {
        Round += 1;
        EmitSignal(SignalName.RoundStarted);
        foreach (CharacterUnit cUnit in AllCharacters)
        {
            if (cUnit.CharacterData.Alive)
            {
                ApplyRegen(cUnit);
                cUnit.CharacterData.OnNewRound(); // e.g. applying round effects
            }
        }
        _battleState.ComputeTurnOrder();
    }

    private void ApplyRegen(CharacterUnit characterUnit)
    {
        // if (characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Health] < characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.MaxHealth])
        // {
        //     GD.Print("todo log: " + characterUnit.CharacterData.Name + " restores health by " + characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.HealthRegen]);
        // }

        if (characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Health] + characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.HealthRegen] <= characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.MaxHealth])
        {
            characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Health] += characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.HealthRegen];
        }

        // if (characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Endurance] < characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.MaxEndurance])
        // {
        //     GD.Print("todo log: " + characterUnit.CharacterData.Name + " restores endurance by " + characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.EnduranceRegen]);
        // }
        // characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Endurance] = Math.Min(characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.MaxEndurance],
        //     characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Endurance] + characterUnit.CharacterData.Stats[StoryCharacterData.StatMode.EnduranceRegen]);
    }

    public void PlayAnim(string anim)
    {
        if (_anim.CurrentAnimation == anim && _anim.IsPlaying())
        {
            return;
        }
        _anim.Play(anim);
    }

    public override void _Process(double delta)
    {
        if (_battleState == null)
        {
            return;
        }
        _battleState.ProcessUpdate(delta);
        // GD.Print(GetState());
    }

    public override void _Input(InputEvent ev)
    {
        if (_battleState == null)
        {
            return;
        }
        _battleState.InputUpdate(ev);
    }

    public void OnActionBtnPressed(ActionMode btn)
    {
        _battleState.OnActionBtnPressed(btn);
    }

    public void OnBtnEndTurnPressed()
    {
        _battleState.OnBtnEndTurnPressed();
    }

    public void OnBtnChooseSpellPressed()
    {
        _battleState.OnBtnChooseSpellPressed();
    }

    // public void OnBtnToggleGridPressed()
    // {
    //     _battleState.OnBtnToggleGridPressed();
    // }

    public void OnBtnMenuPressed()
    {
        _battleState.OnBtnMenuPressed();
    }

    public void SetDebugText(string text)
    {
        GetNode<Label>("BattleHUD/DebugLabel").Text = text;
    }

    public void OnCharacterDied(CharacterUnit characterUnit)
    {
        // GD.Print(characterUnit.CharacterData.Name + " died, so removing from the turn queue");
        CharactersAwaitingTurn.Remove(characterUnit);
    }

    internal void SetCharacterLastSelectedSpell(SpellEffectManager.SpellMode spellSelected)
    {
        CharactersAwaitingTurn[0].UISelectedSpell = spellSelected;
    }

    public CharacterUnit CharacterAtGridPos(Vector2 gridPos)
    {
        foreach (CharacterUnit characterUnit in AllCharacters)
        {
            if (BattleGrid.WorldToGrid(characterUnit.GlobalPosition) == gridPos && characterUnit.CharacterData.Alive)
            {
                return characterUnit;
            }
        }
        return null;
    }

    [Signal]
    public delegate void AreaAttackParsedEventHandler(SpellEffectManager.Spell spell);
    [Signal]
    public delegate void HintClickedCharacterEventHandler(bool rightclick, StoryCharacterData data);

    internal void ParseAreaAttack(SpellEffectManager.Spell spell)
    {
        Random rand = spell.OriginCharacter.Rand;
        Vector2 centreGridPoint;
        if (spell.Outcome.RollResult == BattleRoller.RollResult.CriticalMiss)
        {
            spell.AreaAffectedCharacters.Clear();
            spell.Destination = new Vector2((float)rand.Next(-500, 4400), -600);
            spell.TargetCharacter = null;
            EmitSignal(SignalName.AreaAttackParsed, spell);
            return;
            // set destination to random area outside of the attack area
        }
        else if (spell.Outcome.RollResult == BattleRoller.RollResult.Miss)
        {
            centreGridPoint = BattleGrid.GetRandomNeighbouringHex(spell.Destination);
            spell.Destination = BattleGrid.GridToWorld(centreGridPoint);
            // GD.Print("spell dest: ", spell.Destination);
        }
        else
        {
            // spell destination remains the same
            centreGridPoint = BattleGrid.WorldToGrid(spell.Destination);
        }
        spell.TargetCharacter = CharacterAtGridPos(centreGridPoint);


        // List<Vector2> neighbouringHexGridPositions = BattleGrid.HexNavigation.GetNeighbouringGridPositions(centreGridPoint);
        // foreach (Vector2 vec in neighbouringHexGridPositions)
        // {
        //     CharacterUnit affectedCharacter = CharacterAtGridPos(vec);
        //     if (affectedCharacter != null)
        //     {
        //         spell.AreaAffectedCharacters.Add(affectedCharacter);
        //     }
        // }
        // if (spell.TargetCharacter != null)
        // {
        //     spell.AreaAffectedCharacters.Add(spell.TargetCharacter);
        // }

        spell.AreaAffectedCharacters = GetAffectedAreaSpellCharacters(spell, centreGridPoint);

        // GD.Print("spell centre is here: ", centreGridPoint);
        // GD.Print("clicked on here: ", BattleGrid.WorldToGrid(GetGlobalMousePosition()));
        // foreach (CharacterUnit characterUnit in spell.AreaAffectedCharacters)
        // {
        //     GD.Print("affected character is here: ", BattleGrid.WorldToGrid(characterUnit.GlobalPosition) + " and named : " + characterUnit.CharacterData.Name + " and controlled by: " + characterUnit.StatusToPlayer + " and has health of: " + characterUnit.CharacterData.Health);
        // }


        EmitSignal(SignalName.AreaAttackParsed, spell);
    }

    // in future need to modify by the area size (1 = centre + 6 surrounding hexes, 2 = centre + 6 surrounding hexes + each of those hexes 6 more etc.. sounds like a recursive method needed)
    public Godot.Collections.Array<CharacterUnit> GetAffectedAreaSpellCharacters(SpellEffectManager.Spell spell, Vector2 centreGridPoint)
    {
        Godot.Collections.Array<CharacterUnit> result = new();
        List<Vector2> neighbouringHexes = BattleGrid.HexNavigation.GetNeighbouringGridPositions(centreGridPoint);
        foreach (Vector2 vec in neighbouringHexes)
        {
            CharacterUnit affectedCharacter = CharacterAtGridPos(vec);
            if (affectedCharacter != null)
            {
                result.Add(affectedCharacter);
            }
        }
        if (spell.TargetCharacter != null)
        {
            result.Add(spell.TargetCharacter);
        }
        return result;
    }

    internal void OnCharacterRoundEffectApplied(CharacterUnit newChar, CharacterRoundEffect effect)
    {
        if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Berserk)
        {
            newChar.DoBerserkStatusToPlayer();
        }
        if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Stat && effect.StatAffected == StoryCharacterData.StatMode.Health && effect.Magnitude < 0)
        {
            newChar.TakeDamageOrder(new BattleRoller.RollerOutcomeInformation() { FinalDamage = 0 });
        }
    }

    internal void OnMovedButStillHaveAP()
    {
        _battleState.OnMovedButStillHaveAP();
    }
    public void SetPlayerAction(Battler.ActionMode action)
    {
        _battleState.SetPlayerAction(action);
    }

    internal void OnCharacterRoundEffectFaded(CharacterUnit newChar, CharacterRoundEffect roundEffect)
    {
        if (roundEffect.EffectType == CharacterRoundEffect.EffectTypeMode.Berserk)
        {
            newChar.RestoreStatusToPlayer();
        }
    }
}
