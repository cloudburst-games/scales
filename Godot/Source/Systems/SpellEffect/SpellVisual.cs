using Godot;
using System;

public partial class SpellVisual : Node2D
{
    // the godot way of signals doesnt seem to play well with c# interfaces
    // public event EffectFinished EffectFinishedEvent;

    [Export]
    public AnimationPlayer Anim;

    [Export]
    public float Speed = 200f;

    [Signal]
    public delegate void FinishedEventHandler(SpellEffectManager.Spell spell);

    private SpellEffectState _spellEffectState;

    public enum SpellEffectMode { Projectile }

    // public enum SpellEffectVisualMode { Projectile, Self, FromSky }
    public void SetSpellEffectState(SpellEffectManager.SpellEffectVisualMode mode)
    {
        switch (mode)
        {
            case SpellEffectManager.SpellEffectVisualMode.Projectile:
                _spellEffectState = new ProjectileSpellEffectState(this);
                break;
        }
    }

    public override void _Ready()
    {
        SetPhysicsProcess(false);
        // DEBUGGING
        // SetSpellEffectState(SpellEffectMode.Projectile);
        // Start(new BattleSpellData()
        // {
        //     Origin = GlobalPosition,
        //     Destination = new Vector2(3000, 3000),
        // });

    }

    public override void _PhysicsProcess(double delta)
    {
        _spellEffectState.Update(delta);
    }

    public void Start(SpellEffectManager.Spell spell)
    {
        _spellEffectState.Start(spell);
    }
}
