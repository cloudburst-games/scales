using Godot;
using System;

public partial class MeleeBattleCharacterUnitActionState : CharacterUnitActionState
{

    private float _animHitTime = 0.2f; // in future, would be nice to populate this separately for each body type
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
        this.CharacterUnit.GetNode<AudioContainer>("AudioMelee").Play();
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
            AttackerHitModifier = attackerData.GetCorrectHitBonusMelee(),
            DefenderDodgeModifier = defenderData.Stats[StoryCharacterData.StatMode.Dodge],
            AttackerDamageModifier = attackerData.GetCorrectMeleeWeaponDamageBonus(),
            DefenderDamageResist = defenderData.Stats[StoryCharacterData.StatMode.PhysicalResist],
            DamageDice = attackerData.WeaponDiceMelee,
            CriticalThreshold = attackerData.Stats[StoryCharacterData.StatMode.CriticalThreshold]
        };

        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(CharacterUnit.Rand, meleeAttack); // can potentially return this to improve the battle log!
        await ToSignal(CharacterUnit.GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        CharacterUnit.MeleeTarget.TakeDamageOrder(res); // this should force them into TakeDamage state and they take the damage
        // // if (CharacterUnit.Anim.IsPlaying())
        // {
        //     CharacterUnit.AnimationTree.GetCurr
        //     }
        await ToSignal(CharacterUnit.AnimationTree, AnimationTree.SignalName.AnimationFinished);
        EndBattleTurn();
    }

    public override void Exit()
    {
        base.Exit();
        this.CharacterUnit.MeleeTarget = null;
    }
}
