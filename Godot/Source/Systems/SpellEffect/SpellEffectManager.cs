using Godot;
using System;
using System.Collections.Generic;

public partial class SpellEffectManager : Node
{
    public partial class Spell : RefCounted
    {
        public Vector2 Origin;
        public Vector2 Destination;
        public SpellVisualController SpellEffectVisual;
        public List<SpellEffect> AssociatedEffects = new();
        public CharacterUnit TargetCharacter;
        public CharacterUnit OriginCharacter;
        public string Name { get; set; }
        public enum TargetMode { Ally, Enemy, Ground }
        public TargetMode Target { get; set; }
    }

    public partial class SpellEffect : RefCounted
    {
        public List<Tuple<int, int>> DamageDice { get; set; }
        public BattleRoller.AttackType AttackType { get; set; } = BattleRoller.AttackType.Normal;
        public bool Mystical { get; set; }
        public SpellEffectDelegate EffectMethod { get; set; }
    }

    public partial class SpellVisualController : RefCounted
    {
        public PackedScene SpellEffectScn { get; set; }
        public SpellEffectManager.SpellEffectVisualMode VisualMode = SpellEffectManager.SpellEffectVisualMode.Projectile;
    }

    // public enum BattleSpellMode
    // {
    //     Arrow,
    //     SolarFlare
    // }
    public BattleLevel CurrentLevel { get; set; }

    // public Dictionary<BattleSpellMode, List<SpellEffectData>> AllSpellsOld = new();

    // [Export]
    // private Godot.Collections.Array<PackedScene> _spellEffectScns = new();

    // [Signal]
    // public delegate void SpellEffectFinishedEventHandler(BattleSpellData battleSpellData);

    [Export]
    private Godot.Collections.Dictionary<SpellMode, PackedScene> _spellVisualScns = new();
    public enum SpellEffectMode { Fire, Physical }
    public enum SpellMode { Arrow, SolarFlare }
    public enum SpellEffectVisualMode { Projectile, Self, FromSky }
    public enum SpellEffectTargetMode { Self, Target }
    private Dictionary<SpellEffectMode, SpellEffect> _allSpellEffects = new();
    private Dictionary<SpellMode, SpellVisualController> _allSpellVisuals = new();
    public Dictionary<SpellMode, Spell> AllSpells { get; private set; } = new();

    public delegate void SpellEffectDelegate(SpellEffect effect, Spell spell);

    public override void _Ready()
    {

        // These two MUST run before GenerateSpells()
        GenerateSpellEffects(); // 1.
        GenerateVisualEffects(); // 2.
        GenerateSpells();
    }

    // todo - construct via json data
    private void GenerateSpellEffects()
    {
        _allSpellEffects[SpellEffectMode.Fire] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 6) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoTargetedAttackEffect,
        };
        _allSpellEffects[SpellEffectMode.Physical] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 6) }, // should be replaced by the character's physical damage
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = false,
            EffectMethod = DoTargetedAttackEffect,
        };
    }

    // todo - construct via json data
    private void GenerateVisualEffects()
    {
        _allSpellVisuals[SpellMode.Arrow] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.Arrow],
            VisualMode = SpellEffectVisualMode.Projectile,
        };
        _allSpellVisuals[SpellMode.SolarFlare] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.SolarFlare],
            VisualMode = SpellEffectVisualMode.Projectile
        };

    }

    // todo - construct via json data
    private void GenerateSpells()
    {
        AllSpells[SpellMode.Arrow] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.Arrow],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.Physical] },
            Name = "Arrow"
        };
        AllSpells[SpellMode.SolarFlare] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.SolarFlare],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.Fire] },
            Name = "Solar Flare"
        };
    }

    // signal callback after casting action completed
    public void OnCastingSpell(Spell spell)
    {
        SpellVisualController visualController = spell.SpellEffectVisual;
        SpellVisual spellVisual = visualController.SpellEffectScn.Instantiate<SpellVisual>();
        spellVisual.GlobalPosition = spell.Origin;
        CurrentLevel.AddChild(spellVisual); // consdier adding under an entity group // 2 days later- what does this comment mean?
        spellVisual.Finished += this.OnSpellFinished;
        spellVisual.SetSpellEffectState(spell.SpellEffectVisual.VisualMode);
        spellVisual.Start(spell);
    }


    public void OnSpellFinished(Spell spell)
    {
        foreach (SpellEffect spellEffect in spell.AssociatedEffects)
        {
            spellEffect.EffectMethod(spellEffect, spell);
        }
        spell.OriginCharacter.OnSpellEffectFinished();
    }

    private void DoTargetedAttackEffect(SpellEffect spellEffect, Spell spell)//Spell spell, SpellEffect spellEffect)
    {
        CharacterUnit originCharacter = spell.OriginCharacter;
        CharacterUnit targetCharacter = spell.TargetCharacter;

        StoryCharacterData attackerData = originCharacter.CharacterData;
        StoryCharacterData defenderData = targetCharacter.CharacterData;

        BattleRoller.RollerInput magicAttack = new(
            attackerHitModifier: attackerData.GetCorrectHitBonus(spellEffect.Mystical),
            defenderDodgeModifier: defenderData.Dodge,
            attackerDamageModifier: spellEffect.Mystical ? attackerData.Mysticism : attackerData.GetCorrectWeaponDamageBonus(),
            defenderDamageResist: spellEffect.Mystical ? defenderData.MysticResist : defenderData.PhysicalResist,
            damageDice: spellEffect.DamageDice,
            criticalThreshold: attackerData.CriticalThreshold,
            attackType: spellEffect.AttackType
        );

        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, magicAttack); // can potentially return this to improve the battle log!
        targetCharacter.TakeDamageOrder(res);
    }



}
