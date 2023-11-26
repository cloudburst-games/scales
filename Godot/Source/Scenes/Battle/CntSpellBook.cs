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

    // private int _reagents;
    // private int _mana;
    private SignalValueHolder _manaReagents = new();
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
        GetNode<BaseTextureButton>("PnlSpellBook/BtnMana").MouseEntered += () => EmitSignal(SignalName.ManaReagentUIHint, _manaReagents, (int)SignalValueHolder.DisplayMode.Mana);
        GetNode<BaseTextureButton>("PnlSpellBook/BtnMana").MouseExited += () => EmitSignal(SignalName.ManaReagentUIHint, _manaReagents, (int)SignalValueHolder.DisplayMode.None);
        GetNode<BaseTextureButton>("PnlSpellBook/BtnReagents").MouseEntered += () => EmitSignal(SignalName.ManaReagentUIHint, _manaReagents, (int)SignalValueHolder.DisplayMode.Reagent);
        GetNode<BaseTextureButton>("PnlSpellBook/BtnReagents").MouseExited += () => EmitSignal(SignalName.ManaReagentUIHint, _manaReagents, (int)SignalValueHolder.DisplayMode.None);
    }

    [Signal]
    public delegate void SpellUIHintEventHandler(int spell, bool canAfford = true);
    [Signal]
    public delegate void ManaReagentUIHintEventHandler(SignalValueHolder manaReagentTotal, int displayMode);

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
        return _allSpells[spell].ReagentCost <= _manaReagents.Reagent && _allSpells[spell].ChargeCost <= _manaReagents.Mana;
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
        _manaReagents.Reagent = reagents;
        _manaReagents.Mana = mana;
        GetNode<Label>("PnlSpellBook/BtnReagents/LblReagents").Text = _manaReagents.Reagent.ToString();
        GetNode<Label>("PnlSpellBook/BtnMana/LblMana").Text = _manaReagents.Mana.ToString();
        HideAllSpellBtns();
        foreach (SpellEffectManager.SpellMode spell in spells)
        {
            GetNode<BaseTextureButton>(_allSpellBtns[spell]).Visible = true;
            SetSpellLabel(spell);
        }
    }

    private void SetSpellLabel(SpellEffectManager.SpellMode spell)
    {
        if (_allSpells.ContainsKey(spell))
        {
            SpellEffectManager.Spell s = _allSpells[spell];
            string costType = s.ReagentCost > 0 ? "Reagents: " : "Mana: ";
            int cost = s.ReagentCost > 0 ? s.ReagentCost : s.ChargeCost;
            BaseTextureButton btn = GetNode<BaseTextureButton>(_allSpellBtns[spell]);
            if (btn.GetChildCount() > 0 && btn.GetChildren()[0] is Label l)
            {
                l.Text = $"{s.Name}\n{costType}{cost.ToString()}";
            }
        }
    }

    internal void Init(System.Collections.Generic.Dictionary<SpellEffectManager.SpellMode, SpellEffectManager.Spell> allSpells)
    {
        this._allSpells = allSpells;
    }
}

public partial class SignalValueHolder : RefCounted
{
    public enum DisplayMode { Mana, Reagent, None }
    public int Mana { get; set; }

    public int Reagent { get; set; }
}