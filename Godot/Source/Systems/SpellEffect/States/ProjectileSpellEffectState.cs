using Godot;
public partial class ProjectileSpellEffectState : SpellEffectState
{

    private BattleSpellData _spellEffectData;

    public ProjectileSpellEffectState(SpellEffect spellEffect)
    {
        this.SpellEffect = spellEffect;
    }

    private bool _finishing = false;
    public override void Start(BattleSpellData spellEffectData)
    {

        // if projectile, then go from origin to destination then stop at destination (anim would loop)
        // otherwise originate at destination and play through the animation (anim does not loop)
        _spellEffectData = spellEffectData;
        if (SpellEffect.Anim != null)
        {
            SpellEffect.Anim.Play("Start");
        }
        SpellEffect.LookAt(_spellEffectData.Destination);
        SpellEffect.SetPhysicsProcess(true);
    }

    public override void Update(double delta)

    {
        Vector2 direction = (_spellEffectData.Destination - SpellEffect.GlobalPosition).Normalized();
        SpellEffect.GlobalPosition += direction * SpellEffect.Speed * (float)delta;
        if (SpellEffect.GlobalPosition.DistanceTo(_spellEffectData.Destination) < 10 && !_finishing)
        {
            Finish();
        }
    }

    public async void Finish()
    {
        _finishing = true;
        SpellEffect.Anim.Play("Finish");
        await ToSignal(SpellEffect.Anim, AnimationPlayer.SignalName.AnimationFinished);
        SpellEffect.EmitSignal(SpellEffect.SignalName.Finished, _spellEffectData);
        SpellEffect.QueueFree();
    }
}