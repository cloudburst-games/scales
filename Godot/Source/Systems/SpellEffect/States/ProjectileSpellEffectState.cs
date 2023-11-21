using Godot;
public partial class ProjectileSpellEffectState : SpellEffectState
{

    private SpellEffectManager.Spell _spell;

    public ProjectileSpellEffectState(SpellVisual spellEffect)
    {
        this.SpellEffect = spellEffect;
    }

    private bool _finishing = false;
    private Vector2 _direction;
    public override void Start(SpellEffectManager.Spell spell)
    {
        // if projectile, then go from origin to destination then stop at destination (anim would loop)
        // otherwise originate at destination and play through the animation (anim does not loop)
        _spell = spell;
        if (SpellEffect.Anim != null)
        {
            SpellEffect.Anim.Play("Start");
        }
        SpellEffect.LookAt(_spell.Destination);
        _direction = (_spell.Destination - SpellEffect.GlobalPosition).Normalized();
        SpellEffect.SetPhysicsProcess(true);

        // SpellEffect.Speed = 150; // this is for testing
    }

    public override void Update(double delta)

    {
        if (_spell == null)
            return;
        // GD.Print(5, SpellEffect.GlobalPosition);
        // GD.Print(SpellEffect.GlobalPosition.DistanceTo(_spellEffectData.Destination));
        // GD.Print(_spellEffectData.Destination);
        SpellEffect.GlobalPosition += _direction * SpellEffect.Speed * (float)delta;
        if (SpellEffect.GlobalPosition.DistanceTo(_spell.Destination) < SpellEffect.Speed / 50 && !_finishing)
        {
            SpellEffect.Speed = 0;
            Finish();
        }
    }

    public async void Finish()
    {
        _finishing = true;
        SpellEffect.Anim.Play("Finish");
        SpellEffect.EmitSignal(SpellVisual.SignalName.Finished, _spell);
        await ToSignal(SpellEffect.Anim, AnimationPlayer.SignalName.AnimationFinished);
        SpellEffect.QueueFree();
    }
}