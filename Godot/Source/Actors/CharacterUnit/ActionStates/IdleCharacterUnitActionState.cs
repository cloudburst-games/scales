// IdleCharacterUnitActionState. This is the idle state in adventure map mode. Currently it transitions to and from Moving,
// and is the initial and default state.
using Godot;
using System;

public partial class IdleCharacterUnitActionState : CharacterUnitActionState
{

    public IdleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", true);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
    }

    public override void Update(double delta)
    {
        base.Update(delta);

        // Detect whether we have a far enough away target position, and if so, move to it.
        if (CharacterUnit.NavAgent.TargetPosition.DistanceTo(CharacterUnit.GlobalPosition) >= 20)
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.Moving);
        }
    }

    public override void Exit()
    {
    }
}
