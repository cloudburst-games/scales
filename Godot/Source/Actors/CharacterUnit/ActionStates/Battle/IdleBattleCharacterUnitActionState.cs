// IdleBattleCharacterUnitActionState. In this state, the character is actively awaiting player/AI input, and ready to act.
using Godot;
using System;

public partial class IdleBattleCharacterUnitActionState : CharacterUnitActionState
{
    public IdleBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", true);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/takingdamage", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/dying", false);
        // We want to remove the obstacle as we are about to move
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.RemoveObstacle, CharacterUnit, true);
        CharacterUnit.Modulate = new Color(1, 0, 0);
    }

    public override void Update(double delta)
    {
        base.Update(delta);

        if (CharacterUnit.BattlePath.Count > 0)
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.MovingBattle);
        }
        // TODO -also transition to other battle states such as melee? casting? ranged?
    }

    public override void Exit()
    {
        base.Exit();
        CharacterUnit.Modulate = new Color(1, 1, 1);
    }

    public override void BattleSkipOrder()
    {
        base.BattleSkipOrder();
        EndBattleTurn();
        GD.Print("going to end turn from idle ", CharacterUnit.CharacterData.Name);
    }
    public override void BattleMeleeOrder()
    {
        base.BattleMeleeOrder();
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.MeleeBattle);
    }

    public override void BattleShootOrderOld(CharacterUnit targetCharacter)
    {
        base.BattleShootOrderOld(targetCharacter);
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.RangedBattle);
    }

    public override void BattleShootOrder()
    {
        base.BattleShootOrder();
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.RangedBattle);
    }
    public override void BattleCastOrder()
    {
        base.BattleCastOrder();
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.CastingBattle);
    }
}
