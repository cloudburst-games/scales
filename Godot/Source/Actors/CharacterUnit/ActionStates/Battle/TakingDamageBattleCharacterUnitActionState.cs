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
        // GD.Print("Entering taking damage state ", CharacterUnit.CharacterData.Name);
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
        this.CharacterUnit.GetNode<AudioContainer>("AudioHurt").Play();
        await ToSignal(CharacterUnit.AnimationTree, AnimationTree.SignalName.AnimationFinished);
        // so modulate doesnt work with the shader, so we first remove the shader during the anim, then reapply it -it needs to be duplicated after the anim as a result
        // temporary hack until we have real taking damage anims that dont rely on modulate
        CharacterUnit.GetNode<Sprite2D>("Sprite").Material = (Godot.Material)CharacterUnit.GetNode<Sprite2D>("Sprite").Material.Duplicate(true);
        CharacterUnit.RestoreLastOutline();

        if (CharacterUnit.TurnPending)
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.IdleBattle); // because BattleIdleOrder doesn't work while we are still in this state
        }
        else
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.WaitingBattle);
        }
    }
    // 🐱
    // 😺
    // 😸
    // 😻
    // 😽
    // 🐾
    public void TakeDamage()
    {
        // BattleRoller.RollerOutcomeInformation.DebugPrint(res);
        CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Health] -= CharacterUnit.TakingDamageResult.FinalDamage;
        CharacterUnit.UpdateBarHealth();
        if (CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Health] <= 0)
        {
            CharacterUnit.SetActionState(CharacterUnit.ActionMode.DyingBattle);
        }
        else
        {
            TakingDamageAnim();
        }

        CharacterUnit.EmitSignal(CharacterUnit.SignalName.TakingDamage, CharacterUnit.TakingDamageResult, CharacterUnit.CharacterData.Name, CharacterUnit.GlobalPosition, CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Health] <= 0);
        // GD.Print(string.Format("{0} takes {1} damage and has {2} health remaining.", CharacterUnit.CharacterData.Name, CharacterUnit.TakingDamageResult.FinalDamage, CharacterUnit.CharacterData.Stats[StoryCharacterData.StatMode.Health]));

    }

    public override void Exit()
    {
        base.Exit();
        // CharacterUnit.TakeDamageQueued = false;
        this.CharacterUnit.MeleeTarget = null;
    }
}
