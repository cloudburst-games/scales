// CharacterUnitActionState. Inherited by all the CharacterUnit action states.
using Godot;
using System;

public partial class CharacterUnitActionState : RefCounted
{

    public CharacterUnit CharacterUnit { get; set; }

    public virtual void Update(double delta)
    {

    }

    public virtual void Exit()
    {

    }

    public virtual void BattleIdleOrder()
    { }
    public virtual void BattleEndOrder()
    { }
    public virtual void BattleSkipOrder() { }
    public virtual void BattleMeleeOrder() { }

    // Shared method - as turn can be ended from the moving state, manually by the player in idle state,
    // after attacking or casting, etc.
    public void EndBattleTurn()
    {
        CharacterUnit.TurnPending = false;
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.WaitingBattle);
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.BattleTurnEnded, CharacterUnit);
    }

    public virtual void BattleShootOrder(CharacterUnit targetCharacter)
    {
    }

    public virtual void BattleCastOrder(Vector2 mouseGridPos, CharacterUnit targetCharacter)
    {
    }
    public virtual void OnSpellEffectFinished()
    {
    }
}
