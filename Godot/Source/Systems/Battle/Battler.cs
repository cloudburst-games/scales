// This needs to remain modular in case it is to be used outside of a separate BattleScene
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Battler : Node2D
{
    public enum ActionMode { Melee, Shoot, Cast, Move, Hint, Invalid, None }
    [Export]
    public CursorControl CursorControl { get; set; }

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

    [Signal]
    public delegate void AnimationFinishedEventHandler(string animName);
    [Signal]
    public delegate void BattleEndedEventHandler(bool playerWon);
    [Signal]
    public delegate void LogBattleTextEventHandler(string text, bool persist);

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

    public void Init(Godot.Collections.Array<CharacterUnit> involvedCharacters, HexGrid battleGrid)
    {
        BattleGrid = battleGrid;

        foreach (CharacterUnit cUnit in involvedCharacters)
        {
            cUnit.BattleTargetPosition = battleGrid.GetCorrectedWorldPosition(cUnit.GlobalPosition);
            if (!cUnit.IsConnected(CharacterUnit.SignalName.BattleTurnEnded, _battleTurnEndedCallable))
            {
                cUnit.Connect(CharacterUnit.SignalName.BattleTurnEnded, _battleTurnEndedCallable);
            }
        }

        AllCharacters = involvedCharacters.ToList();

        SetState(BattleMode.Starting);
        // AllCharacters[0].CharacterStats.CurrentHealth -= 1;
        // foreach (CharacterUnit c in AllCharacters)
        // {
        //     GD.Print(c.CharacterStats.CurrentHealth);
        // }
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
        if (!AreAnyAlive(CharacterUnit.StatusToPlayerMode.Hostile) || !AreAnyAlive(CharacterUnit.StatusToPlayerMode.Player))
        {
            SetState(BattleMode.Ending);
        }
        else
        {
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
                if (cUnit.CharacterStats.Alive)
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
        _battleState.ComputeTurnOrder();
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

    public void SetDebugText(string text)
    {
        GetNode<Label>("BattleHUD/DebugLabel").Text = text;
    }

}
