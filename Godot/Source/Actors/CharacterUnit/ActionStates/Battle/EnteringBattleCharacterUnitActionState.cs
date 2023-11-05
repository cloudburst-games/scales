// EnteringBattleCharacterUnitActionState. In this state, the character is entering battle - moving from an
// initial world position to the centre of the designated hex when starting battle.

// Battle State diagram //
// EnteringBattle <==== (from any eligible state - battle can be triggered)
//    ||
//    ||    |=========> ExitingBattle ====> Idle
//    \/    ||
// WaitingBattle <===================================|
//      ||                                          ||
//      \/                               (end turn) ||
// IdleBattle ==> TODO other battle actions======== ||
//     ||/\   <==                                   ||
//     ||||                                         ||
//     \/||                                         ||
// MovingBattle                                     ||
//      ||                 (End turn)               ||
//      |=============================================
// //

using Godot;
using System;

public partial class EnteringBattleCharacterUnitActionState : CharacterUnitActionState
{
    public EnteringBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/melee", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", true);
        // When about to move to the starting position, announce that we are moving, so that the obstacle is unset
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.MovingInBattle, CharacterUnit, true);
        // when entering battle, disable avoidance (we will navigate manually hex by hex)
        CharacterUnit.GetNode<NavigationAgent2D>("NavigationAgent2D").AvoidanceEnabled = false;
    }

    public override void Update(double delta)
    {
        base.Update(delta);

        // Move to the target position - centre of the hex usually, or a nearby hex if overlapping with another character.
        Vector2 direction = (CharacterUnit.BattleTargetPosition - CharacterUnit.GlobalPosition).Normalized();
        CharacterUnit.GlobalPosition += direction * CharacterUnit.CharacterStats.MoveSpeed * (float)delta;

        this.CharacterUnit.AnimationTree.Set("parameters/Moving/blend_position", direction);
        if (CharacterUnit.GlobalPosition.DistanceTo(CharacterUnit.BattleTargetPosition) < 5)
        {
            CharacterUnit.GlobalPosition = CharacterUnit.BattleTargetPosition;
            // THIS IS ONLY IF WE DO NOT USE AN ADVENTURE MAP... DEPENDS ON SCOPE!
            CharacterUnit.AnimationTree.Set("parameters/Moving/blend_position", CharacterUnit.StartingBattleAnimDirection);
            CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", CharacterUnit.StartingBattleAnimDirection);
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.WaitingBattle);
        }
    }
}
