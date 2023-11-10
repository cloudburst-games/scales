// MovingCharacterUnitActionState. This is the moving state in adventure mode. Responsible for moving the character
// when a new target position is set (transitions from Idle).
using Godot;
using System;

public partial class MovingCharacterUnitActionState : CharacterUnitActionState
{


    public MovingCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", true);
    }

    public override void Update(double delta)
    {
        base.Update(delta);

        // Get the Vector towards the next nav point, and move via the NavAgent.
        Vector2 direction = CharacterUnit.NavAgent.GetNextPathPosition() - CharacterUnit.GlobalPosition;
        direction = direction.Normalized();
        CharacterUnit.NavAgent.Velocity = direction * CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.MoveSpeed] * (float)delta;

        // If we are very close to the target (end) position, go back to Idle
        if (CharacterUnit.NavAgent.TargetPosition.DistanceTo(CharacterUnit.GlobalPosition) < 20)
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.Idle);
        }
        // Error handling - otherwise gets stuck in Movement state: if close to target, but velocity unchanging, change
        // target position to current position
        else if (CharacterUnit.NavAgent.TargetPosition.DistanceTo(CharacterUnit.GlobalPosition) < 60 &&
            CharacterUnit.NavAgent.Velocity == CharacterUnit.PreviousVelocity)
        {
            CharacterUnit.NavAgent.TargetPosition = CharacterUnit.GlobalPosition;
        }

        this.CharacterUnit.AnimationTree.Set("parameters/Moving/blend_position", CharacterUnit.NavAgent.Velocity);
    }

    public override void Exit()
    {
        base.Exit();
        // The idle blend position should be the same as when we stopped moving
        this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position",
            this.CharacterUnit.AnimationTree.Get("parameters/Moving/blend_position"));
    }
}
