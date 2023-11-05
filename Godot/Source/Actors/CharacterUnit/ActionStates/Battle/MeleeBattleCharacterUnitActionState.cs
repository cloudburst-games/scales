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
    {

        // GD.Print(String.Format("This {0} attacks enemy {1}", CharacterUnit.CharacterData.Name, CharacterUnit.MeleeTarget.CharacterData.Name));
        // await ToSignal  - melee animation
        // await ToSignal(CharacterUnit.GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(CharacterUnit.AnimationTree, AnimationTree.SignalName.AnimationFinished);
        EndBattleTurn();
    }

    public override void Exit()
    {
        base.Exit();
        this.CharacterUnit.MeleeTarget = null;
    }
}
