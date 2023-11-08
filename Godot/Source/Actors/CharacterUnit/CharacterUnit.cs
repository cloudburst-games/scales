// CharacterUnit script. Coordinates all the various associated scripts. CLEANING UP CURRENTLY

// TODO for AI: //
// AI VERSION - set up different modes - patrol mode, stationary mode, waypoint mode
// Should be a STACK FSM to logically switch behaviours
// Can switch between moving and barking - BarkState - pauses x number of seconds and says something -
// - if at certain radius from CharacterUnit type Y, activate the bark state, which consists of pause X seconds,
// CharacterUnit trigger type Y, and list of strings (each would be separated by 1 second or something hardcoded)
// Each bark should have a cooldown (not repeated), e.g. 1 minute
// - Export this data in a list
// - would need to store the previous movement mode to return to it
// - export movement behaviours (mode, position vectors, gamestate trigger (TBD - global variable) (or ?level variable -int LevelState), time trigger (if between X and Y time)


using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterUnit : CharacterBody2D
{

    public StoryCharacterData CharacterData { get; set; }

    public Vector2 StartingBattleAnimDirection { get; set; } = new();

    // WIP
    // public SpellEffectData SelectedSpell { get; set; } = new SpellEffectData { Name = "Test Spell", Target = SpellEffectData.TargetMode.Enemy };

    // This should be deprecated by CharacterData
    // [Export]
    // public CharacterStats CharacterStats { get; set; }

    // To determine target position when right clicking in adventure map mode
    [Export]
    public int FormationPosition { get; set; } = 1;
    [Export]
    public StatusToPlayerMode StatusToPlayer { get; set; } = StatusToPlayerMode.Neutral;

    [Export]
    private PackedScene _body = GD.Load<PackedScene>("res://Source/Actors/CharacterUnit/Bodies/PlayerBody.tscn");

    // For adventure map selection
    [Signal]
    public delegate void CharacterClickedEventHandler(CharacterUnit characterUnit, bool shift);
    // Used by Battle map mode. To determine whether to set an obstacle on the hex or not. Can feasibly send
    // the global position rather than the whole characterunit!
    [Signal]
    public delegate void RemoveObstacleEventHandler(CharacterUnit characterUnit, bool isMoving);
    // Used by the Battler to determine what to do next, e.g. new round, next character turn, end the battle
    [Signal]
    public delegate void BattleTurnEndedEventHandler(CharacterUnit characterUnit);

    [Signal]
    public delegate void CastingEffectEventHandler(SpellEffectManager.Spell spell);// Vector2 origin, Vector2 destination, CharacterUnit targetCharacter, SpellEffectManager.BattleSpellMode spellEffect);

    [Signal]
    public delegate void DiedEventHandler(CharacterUnit characterUnit);

    public Random Rand { get; set; }

    public enum StatusToPlayerMode { Player, Allied, Neutral, Hostile }

    // Adventure map selection
    public bool Selected { get; set; } = false;
    // I dont think this is used - delete if no errors.
    // private List<Vector2> _currentGridPath = new();
    // public List<Vector2> CurrentPath {
    //     get {
    //         return _currentGridPath;
    //     }
    //     set {
    //         _currentGridPath = value;
    //     }
    // }

    // For when a path is set in battle states
    public List<Vector2> BattlePath { get; set; } = new();
    public Vector2 BattleTargetPosition;
    public AnimationTree AnimationTree { get; set; }
    public AnimationPlayer Anim { get; set; }
    public NavigationAgent2D NavAgent { get; set; }

    // Not yet used, but can be useful for activating nearby items, dialogue, etc.
    private Area2D _proximityArea;

    public enum ControlMode { Player, AI }
    private CharacterUnitControlState _controlState;
    public enum ActionMode
    {
        Idle, Moving, Dying, EnteringBattle, ExitingBattle, IdleBattle, MovingBattle, WaitingBattle, MeleeBattle, RangedBattle, CastingBattle,
        TakingDamageBattle,
        DyingBattle
    }
    private CharacterUnitActionState _actionState;
    // This is used for error correction - when a character is stuck in movement state.
    public Vector2 PreviousVelocity = new Vector2();

    public void SetControlState(ControlMode controlMode)
    {
        switch (controlMode)
        {
            case ControlMode.Player:
                _controlState = new PlayerCharacterUnitControlState(this);
                break;
            case ControlMode.AI:
                _controlState = new AICharacterUnitControlState(this);
                break;
        }
    }

    public ControlMode GetControlState()
    {
        return _controlState is PlayerCharacterUnitControlState ? ControlMode.Player : ControlMode.AI;
    }
    public void SetActionState(ActionMode actionMode)
    {
        if (_actionState != null)
        {
            _actionState.Exit();
        }
        switch (actionMode)
        {
            case ActionMode.Idle:
                _actionState = new IdleCharacterUnitActionState(this);
                break;
            case ActionMode.Moving:
                _actionState = new MovingCharacterUnitActionState(this);
                break;
            case ActionMode.Dying:
                _actionState = new DyingCharacterUnitActionState(this);
                break;
            case ActionMode.EnteringBattle:
                _actionState = new EnteringBattleCharacterUnitActionState(this);
                break;
            case ActionMode.ExitingBattle:
                _actionState = new ExitingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.IdleBattle:
                _actionState = new IdleBattleCharacterUnitActionState(this);
                break;
            case ActionMode.MovingBattle:
                _actionState = new MovingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.MeleeBattle:
                _actionState = new MeleeBattleCharacterUnitActionState(this);
                break;
            case ActionMode.WaitingBattle:
                _actionState = new WaitingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.RangedBattle:
                _actionState = new RangedBattleCharacterUnitActionState(this);
                break;
            case ActionMode.TakingDamageBattle:
                _actionState = new TakingDamageBattleCharacterUnitActionState(this);
                break;
            case ActionMode.DyingBattle:
                _actionState = new DyingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.CastingBattle:
                _actionState = new CastingBattleCharacterUnitActionState(this);
                break;
        }
        // GD.Print(CharacterData.Name + " CHANGED STATE TO " + actionMode);
    }

    // Can make this more readable, e.g. with a switch...
    public ActionMode GetActionState()
    {
        return _actionState is IdleCharacterUnitActionState ? ActionMode.Idle :
            _actionState is MovingCharacterUnitActionState ? ActionMode.Moving :
            _actionState is DyingCharacterUnitActionState ? ActionMode.Dying :
            _actionState is EnteringBattleCharacterUnitActionState ? ActionMode.EnteringBattle :
            _actionState is ExitingBattleCharacterUnitActionState ? ActionMode.ExitingBattle :
            _actionState is IdleBattleCharacterUnitActionState ? ActionMode.IdleBattle :
            _actionState is WaitingBattleCharacterUnitActionState ? ActionMode.WaitingBattle :
            _actionState is RangedBattleCharacterUnitActionState ? ActionMode.RangedBattle :
            _actionState is TakingDamageBattleCharacterUnitActionState ? ActionMode.TakingDamageBattle :
            _actionState is DyingBattleCharacterUnitActionState ? ActionMode.DyingBattle :
            _actionState is MeleeBattleCharacterUnitActionState ? ActionMode.MeleeBattle :
            _actionState is CastingBattleCharacterUnitActionState ? ActionMode.CastingBattle : ActionMode.MovingBattle;
    }

    public override void _Ready()
    {
        Init();
        InitStatusToPlayer();
        // important to set this to the current position, to avoid errors
        NavAgent.TargetPosition = GlobalPosition;
    }

    private void Init()
    {
        _body.Instantiate<CharacterBody>().SetBody(this);
        NavAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        AnimationTree = GetNode<AnimationTree>("AnimTree");
        Anim = GetNode<AnimationPlayer>("Anim");
        _proximityArea = GetNode<Area2D>("ProximityArea");
    }

    private void InitStatusToPlayer()
    {
        switch (StatusToPlayer)
        {
            case StatusToPlayerMode.Player:
                SetControlState(ControlMode.Player);
                ValidEnemyTargets = new() { StatusToPlayerMode.Hostile, StatusToPlayerMode.Neutral };
                ValidAllyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Neutral, StatusToPlayerMode.Allied };
                break;
            default:
                SetControlState(ControlMode.AI);
                ValidEnemyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Allied };
                ValidAllyTargets = new() { StatusToPlayerMode.Hostile };
                break;
        }
    }

    public List<StatusToPlayerMode> ValidEnemyTargets = new();
    public List<StatusToPlayerMode> ValidAllyTargets = new();

    private void InitCharacterStats()
    {
        // CharacterStats.ResourceLocalToScene = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_controlState == null || _actionState == null)
        {
            return;
        }
        base._PhysicsProcess(delta);
        SetDebugText(GetActionState().ToString());
        if (NavAgent.Velocity != new Vector2(0, 0))
        {
            PreviousVelocity = NavAgent.Velocity;
        }

        _controlState.Update(delta);
        _actionState.Update(delta);
    }

    private void OnNavAgentVelocityComputed(Vector2 safeVelocity)
    {
        MoveAndCollide(safeVelocity);

    }

    // For GUI input such as character selection with the mouse
    public void OnClickAreaGUIInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton btn)
        {
            if (btn.ButtonIndex == MouseButton.Left && btn.Pressed)
            {
                EmitSignal(SignalName.CharacterClicked, this, Input.IsPhysicalKeyPressed(Key.Shift));
            }
        }
    }

    // Check if a given area is near the character unit
    public bool IsAreaNearCharacterUnit(Area2D area)
    {
        foreach (Area2D a in _proximityArea.GetOverlappingAreas())
        {
            if (a.GetParent() == this)
            {
                continue;
            }
            if (a == area)
            {
                return true;
            }
        }
        return false;
    }

    public CharacterUnit MeleeTarget { get; set; } = null;
    // public CharacterUnit RangedTarget { get; set; } = null;

    // public Vector2 SpellDestination { get; set; }
    // public SpellEffectManager.BattleSpellMode SpellEffect { get; set; }
    // public BattleSpellData SpellBeingCastOld { get; set; }

    public void BattleMoveOrder(int moveCost, List<Vector2> worldPath, CharacterUnit targetCharacter = null)
    {
        worldPath.RemoveAt(0);
        CharacterData.ActionPoints -= moveCost;
        BattlePath = worldPath;
        MeleeTarget = targetCharacter;
    }

    public void BattleMeleeOrder(CharacterUnit targetCharacter)
    {
        MeleeTarget = targetCharacter;
        _actionState.BattleMeleeOrder();
    }

    public SpellEffectManager.Spell SpellBeingCast { get; private set; }
    // internal void BattleShootOrder(CharacterUnit targetCharacter) // DEPRECATED
    // {
    //     RangedTarget = targetCharacter;
    //     SpellDestination = targetCharacter.GlobalPosition;
    //     SpellEffect = SpellEffectManager.BattleSpellMode.Arrow;
    //     _actionState.BattleShootOrderOld(targetCharacter);
    // }

    // consider unifying the shoot and cast methods as they utilise same underpinnings
    internal void BattleShootOrder(SpellEffectManager.Spell spell)
    {
        SpellBeingCast = spell;
        _actionState.BattleShootOrder();
    }

    internal void BattleCastOrder(SpellEffectManager.Spell spell)
    {
        SpellBeingCast = spell;
        _actionState.BattleCastOrder();
    }

    public void OnSpellEffectFinished()
    {
        _actionState.OnSpellEffectFinished();
    }
    public void BattleSkipOrder()
    {
        _actionState.BattleSkipOrder(); // will do something if IdleBattle state
    }

    public bool TurnPending { get; set; } = false;

    public void CharacterStartBattleTurn()
    {
        // wait for any outstanding HIT animations
        // todo - a proper fix for this
        // GD.Print(CharacterData.Name + " startbattleturn method " +
        // AnimationTree.Get("parameters/conditions/takingdamage"));
        // GD.Print(Anim.CurrentAnimation);
        // if (Anim.IsPlaying() && (bool)AnimationTree.Get("parameters/conditions/takingdamage") == true)
        // {

        //     await ToSignal(AnimationTree, AnimationTree.SignalName.AnimationFinished);
        //     //     await ToSignal(Anim, AnimationPlayer.SignalName.AnimationFinished);
        // }
        // if resuming turn (i.e. NOT ended), then don't reset points etc
        if (!TurnPending)
        {
            CharacterData.ActionPoints = CharacterData.MaxActionPoints;
            TurnPending = true;
        }
        _actionState.BattleIdleOrder();
    }

    // public bool 

    // When the battle ends, the units (in waiting state) will be directed to transition to ExitingBattle (= > Idle)
    public void OnBattleEnd()
    {
        _actionState.BattleEndOrder();
    }

    public void SetDebugText(string text)
    {
        GetNode<Label>("LblDebug").Text = text;
    }

    public void SetFromJSON(StoryCharacter.StoryCharacterMode selectedChar)
    {
        CharacterData = StoryCharacterJSONInterface.GetStoryCharacterJSONData(selectedChar);
        CharacterData.Initialise();
    }

    public BattleRoller.RollerOutcomeInformation TakingDamageResult { get; set; }

    public void TakeDamageOrder(BattleRoller.RollerOutcomeInformation res)
    {
        TakingDamageResult = res;
        _actionState.TakeDamageOrder();
    }

}
