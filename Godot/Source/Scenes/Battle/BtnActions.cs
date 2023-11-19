using Godot;
using System;
using System.Collections.Generic;

public partial class BtnActions : TextureButton
{

    [Signal]
    public delegate void ActionBtnPressedEventHandler(Battler.ActionMode actionMode);

    [Export]
    private Panel _actionPnl;
    // [Export]
    // private Godot.Collections.Dictionary<Battler.ActionMode, NodePath> _actionBtnPaths = new();
    private Battler.ActionMode _currentAction;
    private Dictionary<SpellEffectManager.SpellMode, Texture2D[]> _spellTextures = new();
    public Dictionary<Battler.ActionMode, TextureButton> ActionBtns { get; set; }

    [Signal]
    public delegate void CastSpellActionBtnPressedButNoSpellActiveEventHandler();

    public override void _Ready()
    {

        // Texture2D testTexture2D = GD.Load<Texture2D>("res://Assets/Graphics/Interface/Buttons/Spells/SolarFlare.png");
        // magic strings because EXPORTING DICTIONARIES IS SO BUGGED IN GODOT NKASDNJASJDJSDOSNF bevy pls make editor
        _spellTextures = new Dictionary<SpellEffectManager.SpellMode, Texture2D[]>
        {
            {
                SpellEffectManager.SpellMode.SolarFlare, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/SolarBlast3.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/SolarBlast3.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/SolarBlast3.png")
                }
            },
            {
                SpellEffectManager.SpellMode.SolarBlast, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/SolarBlastFinalNormal.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/SolarBlastFinalNormal.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/SolarBlastFinalNormal.png")
                }
            },
            {
                SpellEffectManager.SpellMode.BlindingLight, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/BlindingLigh.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/BlindingLigh.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/BlindingLigh.png")
                }
            },
            {
                SpellEffectManager.SpellMode.JudgementOfFlame, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/FlameJudgement.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/FlameJudgement.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/FlameJudgement.png")
                }
            },
            {
                SpellEffectManager.SpellMode.VialOfFury, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/VialOfFury.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/VialOfFury.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/VialOfFury.png")
                }
            },
            {
                SpellEffectManager.SpellMode.ElixirOfSwiftness, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Swift.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Swift.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Swift.png")
                }
            },
            {
                SpellEffectManager.SpellMode.ElixirOfVigour, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Vigor.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Vigor.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Vigor.png")
                }
            },
            {
                SpellEffectManager.SpellMode.RegenerativeOintment, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Healing.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Healing.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Healing.png")
                }
            },
            {
                SpellEffectManager.SpellMode.None, new Texture2D[3]
                {
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Spellbook/SpellbookSmall.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Spellbook/SpellbookSmallHover.png"),
                    GD.Load<Texture2D>("res://Assets/Graphics/Sprites/Spellbook/SpellbookSmallPressed.png")
                }
            }
        };

        Pressed += this.OnPressed;
    }

    public void OnCharacterTurnStart(CharacterUnit characterUnit)
    {
        OnActionBtnPressed(characterUnit.UISelectedAction);
        ToggleRangedVisibility(characterUnit.CharacterData);

    }

    private void ToggleRangedVisibility(StoryCharacterData data)
    {
        ActionBtns[Battler.ActionMode.Shoot].Visible = (StoryCharacterData.RangedWeaponMode)data.RangedWeaponEquipped != StoryCharacterData.RangedWeaponMode.None;

    }

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

            TextureButton btn = ActionBtns[actionMode];
            TextureNormal = btn.TextureNormal;
            TextureHover = btn.TextureHover;
            EmitSignal(BtnActions.SignalName.ActionBtnPressed, (int)actionMode);
        }
    }

    private bool ActiveSpellButton()
    {
        TextureButton btn = ActionBtns[Battler.ActionMode.Cast];
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
        TexturePressed = _spellTextures[spellSelected][2];

    }

    internal void SetCastBtnTexture(SpellEffectManager.SpellMode spellSelected)
    {
        TextureButton btn = ActionBtns[Battler.ActionMode.Cast];
        btn.TextureNormal = _spellTextures[spellSelected][0];
        btn.TextureHover = _spellTextures[spellSelected][1];
        btn.TexturePressed = _spellTextures[spellSelected][2];

        if (_currentAction == Battler.ActionMode.Cast)
        {
            SetActionBtnToSpellTexture(spellSelected);
        }
    }

}
