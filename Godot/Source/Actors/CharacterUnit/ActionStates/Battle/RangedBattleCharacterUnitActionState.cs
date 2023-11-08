using Godot;
using System;

public partial class RangedBattleCharacterUnitActionState : CharacterUnitActionState
{
    public RangedBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        Vector2 direction = (CharacterUnit.SpellBeingCast.TargetCharacter.GlobalPosition - CharacterUnit.GlobalPosition).Normalized();
        this.CharacterUnit.AnimationTree.Set("parameters/Melee/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false); // todo RANGED if we ever get ranged anims
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", true);

        RangedAttackTarget();
    }

    private void RangedAttackTarget()
    {
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.CastingEffect, CharacterUnit.SpellBeingCast);
    }

    public override void Update(double delta)
    {
        base.Update(delta);

    }

    public override void OnSpellEffectFinished()
    {
        EndBattleTurn();
    }

    public override void Exit()
    {
        base.Exit();
    }
}
