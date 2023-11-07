// CharacterUnit script. Coordinates all the various associated scripts. CLEANING UP CURRENTLY

using System;
using Godot;
// Entered from waiting! (can only die when other people kill you)
public partial class DyingBattleCharacterUnitActionState : CharacterUnitActionState
{
    public DyingBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        // Vector2 direction = (CharacterUnit.MeleeTarget.GlobalPosition - CharacterUnit.GlobalPosition).Normalized();
        // this.CharacterUnit.AnimationTree.Set("parameters/Ranged/blend_position", direction); // TODO- add ranged anims
        // this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/ranged", true);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/takingdamage", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/dying", true);

        CharacterUnit.EmitSignal(CharacterUnit.SignalName.RemoveObstacle, CharacterUnit, true);

        DieAnim();
    }

    private void DieAnim()
    {
        // await ToSignal(CharacterUnit.AnimationTree, AnimationTree.SignalName.AnimationFinished);
        // todo - anything needed after the dying animation completes
        CharacterUnit.CharacterData.Alive = false;


        // signal that died so removed from turn queue
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.Died, CharacterUnit);
    }

    public override void Update(double delta)
    {
        base.Update(delta);

    }
    public override void Exit()
    {
        base.Exit();
    }

    // public override void BattleIdleOrder()
    // {
    //     base.BattleIdleOrder();
    //     EndBattleTurn();
    // }
}
