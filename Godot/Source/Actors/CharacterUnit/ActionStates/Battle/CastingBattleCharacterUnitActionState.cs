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
        if (CharacterUnit.SpellBeingCast.Patron == SpellEffectManager.Spell.PatronMode.Ishtar)
        {
            CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Reagents] = Math.Max(0,
                CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Reagents] - CharacterUnit.SpellBeingCast.ReagentCost);
        }
        else
        {
            CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.FocusCharge] = Math.Max(0,
                CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.FocusCharge] - CharacterUnit.SpellBeingCast.ChargeCost);
        }
    }

    public override void TakeDamageOrder()
    {
        base.TakeDamageOrder();
        EndBattleTurn(whileCasting: true);
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.TakingDamageBattle);

        // CharacterUnit.TakeDamageQueued = true;
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

