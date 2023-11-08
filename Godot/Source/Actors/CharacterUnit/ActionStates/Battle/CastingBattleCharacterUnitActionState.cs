using Godot;
using System;
public partial class CastingBattleCharacterUnitActionState : CharacterUnitActionState
{
    public CastingBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        Vector2 direction = (CharacterUnit.SpellBeingCast.Destination - CharacterUnit.GlobalPosition).Normalized();
        this.CharacterUnit.AnimationTree.Set("parameters/Melee/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false); // todo cast if we ever get cast anims
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", true);

        CastAttackTarget();
    }

    public override void Update(double delta)
    {
        base.Update(delta);

    }

    private void CastAttackTarget()
    {
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.CastingEffect, CharacterUnit.SpellBeingCast);
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

