// WaitingBattleCharacterUnitActionState. In this state, the character is waiting for its turn. It is inactive, and marked
// as an obstacle. This state is entered after a character ends its turn, or when it is awaiting its turn at the start
// of battle. It can be moved out of the state after given permission by the Battler (e.g. to enter Idle -awaiting orders,
// or to exit battle).
using Godot;
using System;

public partial class WaitingBattleCharacterUnitActionState : CharacterUnitActionState
{
    public WaitingBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", true);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        // We want to remove the obstacle as we are about to move
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.MovingInBattle, CharacterUnit, false);

    }

    public override void Update(double delta)
    {
        base.Update(delta);
    }

    public override void BattleIdleOrder()
    {
        base.BattleIdleOrder();
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.IdleBattle);
    }

    public override void BattleEndOrder()
    {
        base.BattleEndOrder();
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.ExitingBattle);
    }
}
