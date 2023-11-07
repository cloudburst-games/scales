using Godot;
using System;

public partial class RangedBattleCharacterUnitActionState : CharacterUnitActionState
{
    public RangedBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        Vector2 direction = (CharacterUnit.RangedTarget.GlobalPosition - CharacterUnit.GlobalPosition).Normalized();
        this.CharacterUnit.AnimationTree.Set("parameters/Melee/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false); // todo RANGED if we ever get ranged anims
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", true);

        RangedAttackTarget();
    }

    public override void Update(double delta)
    {
        base.Update(delta);

    }

    private void RangedAttackTarget()
    {
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.CastingEffect, new BattleSpellData()
        {
            Origin = CharacterUnit.GlobalPosition,
            Destination = CharacterUnit.SpellDestination,
            OriginCharacter = CharacterUnit,
            TargetCharacter = CharacterUnit.RangedTarget,
            AssociatedSpellEffect = (int)CharacterUnit.SpellEffect
        }); // CharacterUnit.GlobalPosition, CharacterUnit.SpellDestination, CharacterUnit.RangedTarget, (int)CharacterUnit.SpellEffect);
    }

    public override void OnSpellEffectFinished()
    {
        EndBattleTurn();
    }

    public override void Exit()
    {
        base.Exit();
        this.CharacterUnit.RangedTarget = null;
    }
}
