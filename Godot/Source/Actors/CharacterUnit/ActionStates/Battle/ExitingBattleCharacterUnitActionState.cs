// ExitingBattleCharacterUnitActionState. In this state, the character is exiting battle - should transition back to
// the appropriate adventure map state (usually Idle).
using Godot;
using System;

public partial class ExitingBattleCharacterUnitActionState : CharacterUnitActionState
{
    public ExitingBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        CharacterUnit.NavAgent.TargetPosition = CharacterUnit.GlobalPosition;
        CharacterUnit.GetNode<NavigationAgent2D>("NavigationAgent2D").AvoidanceEnabled = true;
        // We are moving back to adventure mode, so unset the obstacle
        CharacterUnit.EmitSignal(CharacterUnit.SignalName.MovingInBattle, CharacterUnit, true);
    }

    public override void Update(double delta)
    {
        base.Update(delta);
        
        // if dead/unconscious do something else, e.g. keep dead/unsconcious sprite with area2d and character info there
        // which can be resurrected in future
        CharacterUnit.SetActionState(CharacterUnit.ActionMode.Idle);

    }
}
