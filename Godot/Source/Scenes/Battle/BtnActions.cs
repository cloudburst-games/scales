using Godot;
using System;
using System.Collections.Generic;

public partial class BtnActions : TextureButton
{

    [Signal]
    public delegate void ActionBtnPressedEventHandler(Battler.ActionMode actionMode);

    [Export]
    private Panel _actionPnl;
    [Export]
    private Godot.Collections.Dictionary<Battler.ActionMode, NodePath> _actionBtns = new();
    private Battler.ActionMode _currentAction;
    private Dictionary<SpellEffectManager.SpellMode, Texture2D[]> _spellTextures = new();

    [Signal]
    public delegate void CastSpellActionBtnPressedButNoSpellActiveEventHandler();

    public override void _Ready()
    {
        // Texture2D testTexture2D = GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/SolarFlare.png");
        // magic strings because EXPORTING DICTIONARIES IS SO BUGGED IN GODOT NKASDNJASJDJSDOSNF bevy pls make editor
        _spellTextures = new()  {
            {SpellEffectManager.SpellMode.SolarFlare, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/SolarFlare.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/SolarFlareHover.png")}},
            {SpellEffectManager.SpellMode.SolarBlast, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/SolarBlast.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/SolarBlastHover.png")}},
            {SpellEffectManager.SpellMode.BlindingLight, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/BlindingLight.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/BlindingLightHover.png")}},
            {SpellEffectManager.SpellMode.JudgementOfFlame, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/JudgementFlame.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/JudgementFlameHover.png")}},
            {SpellEffectManager.SpellMode.VialOfFury, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/VialFury.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/VialFuryHover.png")}},
            {SpellEffectManager.SpellMode.ElixirOfSwiftness, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/ElixirSwiftness.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/ElixirSwiftnessHover.png")}},
            {SpellEffectManager.SpellMode.ElixirOfVigour, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/ElixirVigour.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/ElixirVigourHover.png")}},
            {SpellEffectManager.SpellMode.RegenerativeOintment, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/RegenOintment.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/Hover/OintmentHover.png")}},
            {SpellEffectManager.SpellMode.None, new Texture2D[2]
                {GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Actions/Spell.png"),GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Actions/SpellHover.png")}},

        };
        Pressed += this.OnPressed;

        foreach (Battler.ActionMode actionMode in _actionBtns.Keys)
        {
            TextureButton btn = GetNode<TextureButton>(_actionBtns[actionMode]);
            btn.Pressed += () => OnActionBtnPressed(actionMode);
        }
    }

    // public void Start(Battler.ActionMode action)
    // {
    //     OnActionBtnPressed(Battler.ActionMode.Melee);
    // }

    private void OnPressed()
    {
        _actionPnl.Visible = true;
    }

    public void OnActionBtnPressed(Battler.ActionMode actionMode)
    {
        _actionPnl.Visible = false;
        if (actionMode == Battler.ActionMode.Cast && !ActiveSpellButton())
        {
            EmitSignal(SignalName.CastSpellActionBtnPressedButNoSpellActive);
        }
        else
        {
            _currentAction = actionMode;

            TextureButton btn = GetNode<TextureButton>(_actionBtns[actionMode]);
            TextureNormal = btn.TextureNormal;
            TextureHover = btn.TextureHover;
            EmitSignal(BtnActions.SignalName.ActionBtnPressed, (int)actionMode);
        }
    }

    private bool ActiveSpellButton()
    {
        TextureButton btn = GetNode<TextureButton>(_actionBtns[Battler.ActionMode.Cast]);
        Texture2D castTexture = btn.TextureNormal;

        foreach (KeyValuePair<SpellEffectManager.SpellMode, Texture2D[]> kv in _spellTextures)
        {
            if (kv.Key == SpellEffectManager.SpellMode.None)
            {
                continue;
            }
            if (castTexture == kv.Value[0])
            {
                return true;
            }
        }
        return false;
    }

    private void SetActionBtnToSpellTexture(SpellEffectManager.SpellMode spellSelected)
    {

        TextureNormal = _spellTextures[spellSelected][0];
        TextureHover = _spellTextures[spellSelected][1];

    }

    internal void SetCastBtnTexture(SpellEffectManager.SpellMode spellSelected)
    {
        TextureButton btn = GetNode<TextureButton>(_actionBtns[Battler.ActionMode.Cast]);
        btn.TextureNormal = _spellTextures[spellSelected][0];
        btn.TextureHover = _spellTextures[spellSelected][1];

        if (_currentAction == Battler.ActionMode.Cast)
        {
            SetActionBtnToSpellTexture(spellSelected);
        }
    }

}