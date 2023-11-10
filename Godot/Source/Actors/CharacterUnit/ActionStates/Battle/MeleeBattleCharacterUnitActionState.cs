using Godot;
using System;

public partial class MeleeBattleCharacterUnitActionState : CharacterUnitActionState
{
    public MeleeBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        Vector2 direction = (CharacterUnit.MeleeTarget.GlobalPosition - CharacterUnit.GlobalPosition).Normalized();
        this.CharacterUnit.AnimationTree.Set("parameters/Melee/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false); // todo change this to melee true moving false idle false etc
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", true);

        MeleeAttackTarget();
    }

    public override void Update(double delta)
    {
        base.Update(delta);

    }

    private async void MeleeAttackTarget()
    { // hit bonus, 
        StoryCharacterData attackerData = CharacterUnit.CharacterData;
        StoryCharacterData defenderData = CharacterUnit.MeleeTarget.CharacterData;
        // RollerOutcomeInformation res1 = CalculateAttack(rand, targetedPhysical);
        BattleRoller.RollerInput meleeAttack = new()
        {
            AttackerHitModifier = attackerData.GetCorrectHitBonus(),
            DefenderDodgeModifier = defenderData.Stats[StoryCharacterData.StatMode.Dodge],
            AttackerDamageModifier = attackerData.GetCorrectWeaponDamageBonus(),
            DefenderDamageResist = defenderData.Stats[StoryCharacterData.StatMode.PhysicalResist],
            DamageDice = attackerData.WeaponDice,
            CriticalThreshold = attackerData.Stats[StoryCharacterData.StatMode.CriticalThreshold]
        };

        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(CharacterUnit.Rand, meleeAttack); // can potentially return this to improve the battle log!

        await ToSignal(CharacterUnit.AnimationTree, AnimationTree.SignalName.AnimationFinished);
        CharacterUnit.MeleeTarget.TakeDamageOrder(res); // this should force them into TakeDamage state and they take the damage

        EndBattleTurn();
    }

    public override void Exit()
    {
        base.Exit();
        this.CharacterUnit.MeleeTarget = null;
    }
}
