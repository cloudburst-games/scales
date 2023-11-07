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
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/takingdamage", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/dying", false);
        // We want to remove the obstacle as we are about to move
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.RemoveObstacle, CharacterUnit, false);

        // GD.Print("entering wait ", CharacterUnit.CharacterData.Name);
        // // this is a hack
        // if (CharacterUnit.TurnPending)
        // {
        //     GD.Print("going to battle idle order from wait", CharacterUnit.CharacterData.Name);
        //     BattleIdleOrder();
        // }
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

    public override void TakeDamageOrder()
    {
        base.TakeDamageOrder();
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.TakingDamageBattle);
    }
}
