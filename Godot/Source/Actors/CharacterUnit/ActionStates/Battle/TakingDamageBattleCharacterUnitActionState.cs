// CharacterUnit script. Coordinates all the various associated scripts. CLEANING UP CURRENTLY

using Godot;

public partial class TakingDamageBattleCharacterUnitActionState : CharacterUnitActionState
{
    public TakingDamageBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        // Vector2 direction = (CharacterUnit.MeleeTarget.GlobalPosition - CharacterUnit.GlobalPosition).Normalized();
        // this.CharacterUnit.AnimationTree.Set("parameters/Ranged/blend_position", direction); // TODO- add ranged anims
        // this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/ranged", true);
        GD.Print("Entering taking damage state ", CharacterUnit.CharacterData.Name);
        TakeDamage();
    }

    // private bool _runOnce = false;

    public override void Update(double delta)
    {
        base.Update(delta);

    }
    private async void TakingDamageAnim()
    {
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/takingdamage", true);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/dying", false);
        await ToSignal(CharacterUnit.AnimationTree, AnimationTree.SignalName.AnimationFinished);
        if (CharacterUnit.TurnPending)
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.IdleBattle);
        }
        else
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.WaitingBattle);
        }
    }

    public void TakeDamage()
    {
        // BattleRoller.RollerOutcomeInformation.DebugPrint(res);
        GD.Print(string.Format("{0} takes {1} damage and has {2} health remaining.", CharacterUnit.CharacterData.Name, CharacterUnit.TakingDamageResult.FinalDamage, CharacterUnit.CharacterData.Health));
        CharacterUnit.CharacterData.Health -= CharacterUnit.TakingDamageResult.FinalDamage;
        if (CharacterUnit.CharacterData.Health <= 0)
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.DyingBattle);
        }
        else
        {
            TakingDamageAnim();
        }
    }

    public override void Exit()
    {
        base.Exit();
        this.CharacterUnit.MeleeTarget = null;
    }
}
