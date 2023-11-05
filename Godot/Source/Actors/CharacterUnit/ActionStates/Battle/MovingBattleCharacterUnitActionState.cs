// MovingBattleCharacterUnitActionState. In this state, the character is currently doing a move action, and will
// transition back to Idle if any action points remain, or otherwise transition to Waiting
using Godot;
using System;

public partial class MovingBattleCharacterUnitActionState : CharacterUnitActionState
{
    public MovingBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", true);

    }

    public override void Update(double delta)
    {
        base.Update(delta);

        // Here we don't use physics/navigation to move the character, but simply move the 2D position.
        // We get the A* path calculated and set via IdleBattleState, via the BattleMoveOrder method in CharacterUnit.cs
        // In this method we handle any necessary movement tasks such as setting the current path, subtracting action points

        // Set the target position to the first point on the path, or if the path is now empty, either go back to idle
        // (if action points remain), or end turn.
        if (CharacterUnit.BattlePath.Count > 0)
        {
            CharacterUnit.BattleTargetPosition = CharacterUnit.BattlePath[0];
        }
        else
        {
            CharacterUnit.BattleTargetPosition = CharacterUnit.GlobalPosition;
            if (CharacterUnit.MeleeTarget != null)
            {
                CharacterUnit.SetActionState(CharacterUnit.ActionMode.MeleeBattle);
            }
            else
            {
                if (CharacterUnit.CharacterStats.ActionPoints > 0)
                {
                    CharacterUnit.SetActionState(CharacterUnit.ActionMode.IdleBattle);
                }
                else
                {
                    EndBattleTurn(); // sets state to waiting and announces that turn ended
                }
            }
        }

        // Do the usual movement calculations and set the appropriate animation vector
        Vector2 direction = (CharacterUnit.BattleTargetPosition - CharacterUnit.GlobalPosition).Normalized();
        CharacterUnit.GlobalPosition += direction * CharacterUnit.CharacterStats.MoveSpeed * (float)delta;
        this.CharacterUnit.AnimationTree.Set("parameters/Moving/blend_position", direction);
        if (CharacterUnit.GlobalPosition.DistanceTo(CharacterUnit.BattleTargetPosition) < 5)
        {
            CharacterUnit.GlobalPosition = CharacterUnit.BattleTargetPosition;
            if (CharacterUnit.BattlePath.Count > 0)
            {
                CharacterUnit.BattlePath.RemoveAt(0);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        // We set the idle animation position to the movement anim position, so it faces the same direction when stopping moving.
        this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position",
            this.CharacterUnit.AnimationTree.Get("parameters/Moving/blend_position"));
    }

}
