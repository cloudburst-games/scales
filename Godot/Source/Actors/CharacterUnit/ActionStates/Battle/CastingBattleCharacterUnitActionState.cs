// TODO!

public partial class CastingBattleCharacterUnitActionState : CharacterUnitActionState
{
    public CastingBattleCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
        // Vector2 direction = (CharacterUnit.MeleeTarget.GlobalPosition - CharacterUnit.GlobalPosition).Normalized();
        // this.CharacterUnit.AnimationTree.Set("parameters/Ranged/blend_position", direction); // TODO- add ranged anims
        // this.CharacterUnit.AnimationTree.Set("parameters/Idle/blend_position", direction);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/idle", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/moving", false);
        // this.CharacterUnit.AnimationTree.Set("parameters/conditions/ranged", true);

    }

    public override void Update(double delta)
    {
        base.Update(delta);

    }

    public override void Exit()
    {
        base.Exit();
    }
}
