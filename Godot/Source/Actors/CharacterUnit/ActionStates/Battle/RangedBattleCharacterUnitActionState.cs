using Godot;
using System;

public partial class RangedBattleCharacterUnitActionState : CharacterUnitActionState
{
    public RangedBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        // Vector2 direction = (CharacterUnit.MeleeTarget.GlobalPosition - CharacterUnit.GlobalPosition).Normalized();
        // this.CharacterUnit.AnimationTree.Set("parameters/Ranged/blend_position", direction); // TODO- add ranged anims
        // this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/ranged", true);

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
        this.CharacterUnit.MeleeTarget = null;
    }
}
