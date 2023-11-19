using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class CntSpellBook : Control
{
    [Export]
    private Godot.Collections.Dictionary<SpellEffectManager.SpellMode, NodePath> _allSpellBtns = new();

    [Signal]
    public delegate void SpellBtnPressedEventHandler(SpellEffectManager.SpellMode spellSelected);

    private int _reagents;
    private int _mana;
    private System.Collections.Generic.Dictionary<SpellEffectManager.SpellMode, SpellEffectManager.Spell> _allSpells;

    [Export]
    private AudioContainer _audioFizzle;

    public override void _Ready()
    {
        foreach (KeyValuePair<SpellEffectManager.SpellMode, NodePath> kv in _allSpellBtns)
        {
            var btn = GetNode<BaseTextureButton>(kv.Value);
            btn.Pressed += () => OnSpellBtnPressed(kv.Key);
            btn.MouseEntered += () => OnMouseEntered(kv.Key);
            btn.MouseExited += () => OnMouseExited();
        }
    }

    [Signal]
    public delegate void SpellUIHintEventHandler(int spell, bool canAfford = true);

    private void OnMouseEntered(SpellEffectManager.SpellMode spell)
    {
        EmitSignal(SignalName.SpellUIHint, (int)spell, CanAffordSpell(spell));
    }

    private void OnMouseExited()
    {
        EmitSignal(SignalName.SpellUIHint, (int)SpellEffectManager.SpellMode.None);
    }

    private BaseTextureButton GetSpellButton(SpellEffectManager.SpellMode spell)
    {
        return GetNode<BaseTextureButton>(_allSpellBtns[spell]);
    }

    private void OnSpellBtnPressed(SpellEffectManager.SpellMode spellSelected)
    {
        if (CanAffordSpell(spellSelected))
        {
            EmitSignal(SignalName.SpellBtnPressed, (int)spellSelected);
        }
        else
        {
            _audioFizzle.Play();
            EmitSignal(SignalName.SpellUIHint, (int)SpellEffectManager.SpellMode.None);
        }
        // this needs to: change the icon on the HUD, and set the selected action to cast spell, and set the character's selected spell
    }


    private bool CanAffordSpell(SpellEffectManager.SpellMode spell)
    {
        return _allSpells[spell].ReagentCost <= _reagents && _allSpells[spell].ChargeCost <= _mana;
    }

    private void HideAllSpellBtns()
    {
        foreach (NodePath v in _allSpellBtns.Values)
        {
            GetNode<BaseTextureButton>(v).Visible = false;
        }
    }


    public void ShowSpells(Array<SpellEffectManager.SpellMode> spells, int reagents, int mana)
    {
        _reagents = reagents;
        _mana = mana;
        HideAllSpellBtns();
        foreach (SpellEffectManager.SpellMode spell in spells)
        {
            GetNode<BaseTextureButton>(_allSpellBtns[spell]).Visible = true;
        }
    }

    internal void Init(System.Collections.Generic.Dictionary<SpellEffectManager.SpellMode, SpellEffectManager.Spell> allSpells)
    {
        this._allSpells = allSpells;
    }
}
