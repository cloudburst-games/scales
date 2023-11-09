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

    public override void _Ready()
    {
        foreach (KeyValuePair<SpellEffectManager.SpellMode, NodePath> kv in _allSpellBtns)
        {
            GetNode<BaseTextureButton>(kv.Value).Pressed += () => OnSpellBtnPressed(kv.Key);
        }
    }

    private void OnSpellBtnPressed(SpellEffectManager.SpellMode spellSelected)
    {
        EmitSignal(SignalName.SpellBtnPressed, (int)spellSelected);
        // this needs to: change the icon on the HUD, and set the selected action to cast spell, and set the character's selected spell
    }

    private void HideAllSpellBtns()
    {
        foreach (NodePath v in _allSpellBtns.Values)
        {
            GetNode<BaseTextureButton>(v).Visible = false;
        }
    }

    public void ShowSpells(Array<SpellEffectManager.SpellMode> spells)
    {
        HideAllSpellBtns();
        foreach (SpellEffectManager.SpellMode spell in spells)
        {
            GetNode<BaseTextureButton>(_allSpellBtns[spell]).Visible = true;
        }
    }
}
