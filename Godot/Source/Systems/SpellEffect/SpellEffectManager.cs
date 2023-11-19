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
        public int HexDistance { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ReagentCost { get; set; }
        public int ChargeCost { get; set; }
        public enum TargetMode { Ally, Enemy, Ground }
        public TargetMode Target { get; set; }
        public int Range { get; set; }
        public int Area { get; set; } = 0;
        public BattleRoller.RollerOutcomeInformation Outcome { get; set; }

        public Godot.Collections.Array<CharacterUnit> AreaAffectedCharacters = new();
        public SpellMode SpellMode { get; set; }
        public enum PatronMode { Shamash, Ishtar, None }
        public PatronMode Patron;
    }

    public partial class SpellEffect : RefCounted
    {
        public string Name { get; set; }
        public List<Tuple<int, int>> DamageDice { get; set; }
        public BattleRoller.AttackType AttackType { get; set; } = BattleRoller.AttackType.Normal;
        public bool Mystical { get; set; }
        public SpellEffectDelegate EffectMethod { get; set; }
        public int NumRounds { get; set; }
        public StoryCharacterData.AttributeMode AttributeAffected { get; set; }
        public StoryCharacterData.StatMode StatAffected { get; set; }
        public CharacterRoundEffect.EffectTypeMode EffectType { get; set; }
        public bool HitPenalty { get; set; } = false;
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
    public enum SpellEffectMode
    {
        MagicalAttackSingle, PhysicalAttackSingle, MagicalAttackArea,
        DamageOverTime,
        DamageAttributeMight,
        DamageAttributePrecision,
        DamageStatHitPrecision,
        DamageStatHitStrength,
        Berserk,
        FortifyAttributeMight,
        FortifyAttributeResilience,
        FortifyAttributePrecision,
        FortifyAttributeSpeed,
        FortifyStatHealthRegen,
        // Lightning
    }
    public enum SpellMode
    {
        SolarFlare, SolarBlast, JudgementOfFlame, BlindingLight, VialOfFury, ElixirOfVigour, ElixirOfSwiftness, RegenerativeOintment, Sling, None,
        Rock, Lightning, Javelin
    }
    public enum SpellEffectVisualMode { Projectile, Self, FromSky }
    public enum SpellEffectTargetMode { Self, Target }
    private Dictionary<SpellEffectMode, SpellEffect> _allSpellEffects = new();
    private Dictionary<SpellMode, SpellVisualController> _allSpellVisuals = new();
    public Dictionary<SpellMode, Spell> AllSpells { get; private set; } = new();

    public static Dictionary<StoryCharacterData.RangedWeaponMode, SpellMode> RangedWeaponSpells = new() {
        {StoryCharacterData.RangedWeaponMode.Sling, SpellMode.Sling},
        {StoryCharacterData.RangedWeaponMode.Rock, SpellMode.Rock},
        {StoryCharacterData.RangedWeaponMode.Lightning, SpellMode.Lightning},
        {StoryCharacterData.RangedWeaponMode.Javelin, SpellMode.Javelin}
    };

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
        _allSpellEffects[SpellEffectMode.MagicalAttackSingle] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 6) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoTargetedAttackEffect,
            Name = "Fire Damage",
        };
        _allSpellEffects[SpellEffectMode.PhysicalAttackSingle] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 6) }, // should be replaced by the character's physical damage
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = false,
            EffectMethod = DoTargetedAttackEffect,
            Name = "Physical Damage"
        };
        _allSpellEffects[SpellEffectMode.MagicalAttackArea] = new()
        {
            DamageDice = new() { new Tuple<int, int>(2, 6) },
            AttackType = BattleRoller.AttackType.Area,
            Mystical = true,
            EffectMethod = DoAreaAttackEffect,
            Name = "Fire Area Damage"
        };
        _allSpellEffects[SpellEffectMode.MagicalAttackSingle] = new()
        {
            DamageDice = new() { new Tuple<int, int>(2, 6) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoTargetedAttackEffect,
            Name = "Shock Damage",
        };
        _allSpellEffects[SpellEffectMode.DamageOverTime] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoDamageOverTimeEffect,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Stat,
            StatAffected = StoryCharacterData.StatMode.Health,
            Name = "Damage Over Time"

        };
        _allSpellEffects[SpellEffectMode.DamageAttributeMight] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatDamage,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Attribute,
            AttributeAffected = StoryCharacterData.AttributeMode.Might,
            Name = "Might Damage"
        };
        _allSpellEffects[SpellEffectMode.DamageAttributePrecision] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatDamage,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Attribute,
            AttributeAffected = StoryCharacterData.AttributeMode.Precision,
            Name = "Precision Damage"
        };
        _allSpellEffects[SpellEffectMode.DamageStatHitStrength] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatDamage,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Stat,
            StatAffected = StoryCharacterData.StatMode.HitBonusStrength,
            Name = "Hit Reduced (strength)"
        };
        _allSpellEffects[SpellEffectMode.DamageStatHitPrecision] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatDamage,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Stat,
            StatAffected = StoryCharacterData.StatMode.HitBonusPrecision,
            Name = "Hit Reduced (precision)"
        };
        _allSpellEffects[SpellEffectMode.Berserk] = new()
        {
            EffectMethod = DoBerserk,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Berserk,
        };
        _allSpellEffects[SpellEffectMode.FortifyAttributeMight] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatFortify,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Attribute,
            AttributeAffected = StoryCharacterData.AttributeMode.Might,
            Name = "Fortify Might"
        };
        _allSpellEffects[SpellEffectMode.FortifyAttributeResilience] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatFortify,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Attribute,
            AttributeAffected = StoryCharacterData.AttributeMode.Resilience,
            Name = "Fortify Resilience"
        };
        _allSpellEffects[SpellEffectMode.FortifyAttributePrecision] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatFortify,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Attribute,
            AttributeAffected = StoryCharacterData.AttributeMode.Precision,
            Name = "Fortify Precision"
        };
        _allSpellEffects[SpellEffectMode.FortifyAttributeSpeed] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatFortify,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Attribute,
            AttributeAffected = StoryCharacterData.AttributeMode.Speed,
            Name = "Fortify Speed"
        };
        _allSpellEffects[SpellEffectMode.FortifyStatHealthRegen] = new()
        {
            DamageDice = new() { new Tuple<int, int>(1, 3) },
            AttackType = BattleRoller.AttackType.Normal,
            Mystical = true,
            EffectMethod = DoAttributeStatFortify,
            NumRounds = 5,
            EffectType = CharacterRoundEffect.EffectTypeMode.Stat,
            StatAffected = StoryCharacterData.StatMode.HealthRegen,
            Name = "Fortify Health Regeneration"
        };
    }

    // todo - construct via json data
    private void GenerateVisualEffects()
    {
        _allSpellVisuals[SpellMode.Sling] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.Sling],
            VisualMode = SpellEffectVisualMode.Projectile,
        };
        _allSpellVisuals[SpellMode.Rock] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.Rock],
            VisualMode = SpellEffectVisualMode.Projectile,
        };
        _allSpellVisuals[SpellMode.Lightning] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.Lightning],
            VisualMode = SpellEffectVisualMode.Projectile,
        };
        _allSpellVisuals[SpellMode.SolarFlare] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.SolarFlare],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.SolarBlast] = new() // todo!
        {
            SpellEffectScn = _spellVisualScns[SpellMode.SolarBlast],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.JudgementOfFlame] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.JudgementOfFlame],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.BlindingLight] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.BlindingLight],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.VialOfFury] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.VialOfFury],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.ElixirOfVigour] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.ElixirOfVigour],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.ElixirOfSwiftness] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.ElixirOfSwiftness],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.RegenerativeOintment] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.RegenerativeOintment],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.Lightning] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.Lightning],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.Rock] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.Rock],
            VisualMode = SpellEffectVisualMode.Projectile
        };
        _allSpellVisuals[SpellMode.Javelin] = new()
        {
            SpellEffectScn = _spellVisualScns[SpellMode.Javelin],
            VisualMode = SpellEffectVisualMode.Projectile
        };
    }

    // todo - construct via json data
    // Description - max characters  = 60
    private void GenerateSpells()
    {
        AllSpells[SpellMode.Sling] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.Sling],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.PhysicalAttackSingle] },
            Name = "Sling",
            Range = 8,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.Sling,
            Description = "Hit the enemy using your slingshot.",
            ReagentCost = 0,
            ChargeCost = 0,
            Patron = Spell.PatronMode.None,
        };
        AllSpells[SpellMode.Rock] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.Rock],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.PhysicalAttackSingle] },
            Name = "Rock",
            Range = 10,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.Rock,
            Description = "Hurl a rock at your opponent.",
            ReagentCost = 0,
            ChargeCost = 0,
            Patron = Spell.PatronMode.None,
        };
        AllSpells[SpellMode.Javelin] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.Javelin],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.PhysicalAttackSingle] },
            Name = "Javelin",
            Range = 10,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.Javelin,
            Description = "Throw a javelin at your opponent.",
            ReagentCost = 0,
            ChargeCost = 0,
            Patron = Spell.PatronMode.None,
        };
        AllSpells[SpellMode.Lightning] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.Lightning],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.MagicalAttackSingle] },
            Name = "Lightning bolt",
            Range = 12,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.Lightning,
            Description = "Launch a bolt of charged enemy at your target.",
            ReagentCost = 0,
            ChargeCost = 0, // THIS IS A SPECIAL ENEMY ATTACK, unavailable to players
            Patron = Spell.PatronMode.None,
        };
        AllSpells[SpellMode.SolarFlare] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.SolarFlare],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.MagicalAttackSingle] },
            Name = "Solar Flare",
            Range = 8,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.SolarFlare,
            Description = "Launch a jolt of solar energy at an enemy.",
            ReagentCost = 0,
            ChargeCost = 2,
            Patron = Spell.PatronMode.Shamash,
        };
        AllSpells[SpellMode.SolarBlast] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.SolarBlast],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.MagicalAttackArea] },
            Name = "Solar Blast",
            Range = 10,
            Target = Spell.TargetMode.Ground,
            Area = 1,
            SpellMode = SpellMode.SolarBlast,
            Description = "Blast your enemies with the power of the sun.",
            ReagentCost = 0,
            ChargeCost = 4,
            Patron = Spell.PatronMode.Shamash,
        };
        AllSpells[SpellMode.JudgementOfFlame] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.JudgementOfFlame],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.DamageOverTime], _allSpellEffects[SpellEffectMode.DamageAttributeMight], _allSpellEffects[SpellEffectMode.DamageAttributePrecision] },
            Name = "Judgement of Flame",
            Range = 8,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.JudgementOfFlame,
            Description = "Burn an enemy over time and reduce their might and precision.",
            ReagentCost = 0,
            ChargeCost = 5,
            Patron = Spell.PatronMode.Shamash,
        };
        AllSpells[SpellMode.BlindingLight] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.BlindingLight],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.DamageStatHitPrecision], _allSpellEffects[SpellEffectMode.DamageStatHitStrength] },
            Name = "Blinding Light",
            Range = 8,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.BlindingLight,
            Description = "Reduces an opponent's hit chance.",
            ReagentCost = 0,
            ChargeCost = 3,
            Patron = Spell.PatronMode.Shamash,
        };
        AllSpells[SpellMode.VialOfFury] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.VialOfFury],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.Berserk] },
            Name = "Vial of Fury",
            Range = 8,
            Target = Spell.TargetMode.Enemy,
            SpellMode = SpellMode.VialOfFury,
            Description = "Cause an enemy to go berserk, attacking the nearest creature.",
            ReagentCost = 5,
            ChargeCost = 0,
            Patron = Spell.PatronMode.Ishtar,
        };
        AllSpells[SpellMode.ElixirOfVigour] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.ElixirOfVigour],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.FortifyAttributeMight], _allSpellEffects[SpellEffectMode.FortifyAttributeResilience] },
            Name = "Elixir of Vigour",
            Range = 8,
            Target = Spell.TargetMode.Ally,
            SpellMode = SpellMode.ElixirOfVigour,
            Description = "Enhances an ally's strength and resilience.",
            ReagentCost = 2,
            ChargeCost = 0,
            Patron = Spell.PatronMode.Ishtar,
        };
        AllSpells[SpellMode.ElixirOfSwiftness] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.ElixirOfSwiftness],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.FortifyAttributePrecision], _allSpellEffects[SpellEffectMode.FortifyAttributeSpeed] },
            Name = "Elixir of Swiftness",
            Range = 8,
            Target = Spell.TargetMode.Ally,
            SpellMode = SpellMode.ElixirOfSwiftness,
            Description = "Grants an ally improved precision and speed.",
            ReagentCost = 2,
            ChargeCost = 0,
            Patron = Spell.PatronMode.Ishtar,
        };
        AllSpells[SpellMode.RegenerativeOintment] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.RegenerativeOintment],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.FortifyStatHealthRegen] },
            Name = "Regenerative Ointment",
            Range = 8,
            Target = Spell.TargetMode.Ally,
            SpellMode = SpellMode.RegenerativeOintment,
            Description = "Improves an ally's health regeneration.",
            ReagentCost = 3,
            ChargeCost = 0,
            Patron = Spell.PatronMode.Ishtar,
        };
        AllSpells[SpellMode.None] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.RegenerativeOintment],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.FortifyStatHealthRegen] },
            Name = "NO SPELL",
            Range = 8,
            Target = Spell.TargetMode.Ally,
            SpellMode = SpellMode.Sling,
            Description = "",
            ReagentCost = 0,
            ChargeCost = 0,
            Patron = Spell.PatronMode.None,
        };
    }


    // signal callback after casting action completed
    public void OnCastingSpellStart(Spell spell)
    {
        foreach (SpellEffect effect in spell.AssociatedEffects)
        {
            if (effect.AttackType == BattleRoller.AttackType.Area)
            {
                CalculateAreaHit(spell);
                return;
            }
        }
        OnCastingSpell(spell);
    }

    public void OnCastingSpell(Spell spell)
    {
        SpellVisualController visualController = spell.SpellEffectVisual;
        SpellVisual spellVisual = visualController.SpellEffectScn.Instantiate<SpellVisual>();
        spellVisual.GlobalPosition = spell.Origin;

        CurrentLevel.GetNode("Entities/Effects").AddChild(spellVisual); // consdier adding under an entity group // 2 days later- what does this comment mean? // 7 days later ohh i got it

        spellVisual.Finished += this.OnSpellFinished;
        spellVisual.SetSpellEffectState(spell.SpellEffectVisual.VisualMode);

        spellVisual.Start(spell);
    }

    [Signal]
    public delegate void AreaHitCalculatedEventHandler(Spell spell);

    public void CalculateAreaHit(Spell spell)
    {

        BattleRoller.RollerInput areaAttack = new();
        areaAttack.CriticalThreshold = spell.OriginCharacter.CharacterData.Stats[StoryCharacterData.StatMode.CriticalThreshold];
        areaAttack.AttackerHitModifier = spell.OriginCharacter.CharacterData.GetCorrectHitBonusMelee(spell.AssociatedEffects[0].Mystical);
        areaAttack.AttackType = BattleRoller.AttackType.Area;
        areaAttack.HexDistance = spell.HexDistance;

        BattleRoller.RollerOutcomeInformation outcome = BattleRoller.CalculateAreaHitOutcome(spell.OriginCharacter.Rand, areaAttack);
        spell.Outcome = outcome;

        EmitSignal(SignalName.AreaHitCalculated, spell);
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

        BattleRoller.RollerInput magicAttack = new()
        {
            AttackerHitModifier = attackerData.GetCorrectHitBonusRanged(spellEffect.Mystical) - (spellEffect.HitPenalty ? 4 : 0),
            DefenderDodgeModifier = defenderData.Stats[StoryCharacterData.StatMode.Dodge],
            AttackerDamageModifier = spellEffect.Mystical ? attackerData.Stats[StoryCharacterData.StatMode.Mysticism] : attackerData.GetCorrectRangedWeaponDamageBonus(),
            DefenderDamageResist = spellEffect.Mystical ? defenderData.Stats[StoryCharacterData.StatMode.MysticResist] : defenderData.Stats[StoryCharacterData.StatMode.PhysicalResist],
            DamageDice = spellEffect.DamageDice,
            CriticalThreshold = attackerData.Stats[StoryCharacterData.StatMode.CriticalThreshold],
            AttackType = spellEffect.AttackType
        };

        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, magicAttack); // can potentially return this to improve the battle log!
        targetCharacter.TakeDamageOrder(res);
    }

    // first, calculate if hit or miss
    // then, calculate the centre hex, and all affected characters
    // then, launch the spell
    private void DoAreaAttackEffect(SpellEffect spellEffect, Spell spell)
    {
        // GD.Print("doing area attack affect");
        // GD.Print("centre of attack: ", spell.TargetCharacter.CharacterData.Name);

        CharacterUnit originCharacter = spell.OriginCharacter;
        StoryCharacterData attackerData = originCharacter.CharacterData;
        CharacterUnit targetCharacter = spell.TargetCharacter; // at centre of the effect- takes full damage

        foreach (CharacterUnit affectedCharacter in spell.AreaAffectedCharacters)
        {
            BattleRoller.AttackType attackType = BattleRoller.AttackType.AreaConfirmed;
            if (affectedCharacter != targetCharacter)
            {
                attackType = BattleRoller.AttackType.AreaConfirmedHalfDamage;
            }
            StoryCharacterData defenderData = affectedCharacter.CharacterData;
            // (int attackerDamageModifier, List<Tuple<int, int>> damageDice, int defenderDamageResist)
            BattleRoller.RollerInput magicAttack = new()
            {
                AttackerHitModifier = attackerData.GetCorrectHitBonusMelee(spellEffect.Mystical),
                DefenderDodgeModifier = defenderData.Stats[StoryCharacterData.StatMode.Dodge],
                AttackerDamageModifier = spellEffect.Mystical ? attackerData.Stats[StoryCharacterData.StatMode.Mysticism] : attackerData.GetCorrectRangedWeaponDamageBonus(),
                DefenderDamageResist = spellEffect.Mystical ? defenderData.Stats[StoryCharacterData.StatMode.MysticResist] : defenderData.Stats[StoryCharacterData.StatMode.PhysicalResist],
                DamageDice = spellEffect.DamageDice,
                CriticalThreshold = attackerData.Stats[StoryCharacterData.StatMode.CriticalThreshold],
                AttackType = attackType
            };
            spell.Outcome.RollerInput.AttackType = attackType; // WHY IS ATTACK TYPE NORMAL 

            // GD.Print(1, spell.Outcome.RollerInput.AttackType);
            // GD.Print(2, magicAttack.AttackType);

            BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, magicAttack, spell.Outcome); // can potentially return this to improve the battle log!

            // GD.Print("take dmg damnit: ", affectedCharacter.CharacterData.Name);
            affectedCharacter.TakeDamageOrder(res);
        }
    }



    private void DoAttributeStatDamage(SpellEffect spellEffect, Spell spell)
    {
        CharacterUnit originCharacter = spell.OriginCharacter;
        StoryCharacterData attackerData = originCharacter.CharacterData;
        CharacterUnit targetCharacter = spell.TargetCharacter;
        BattleRoller.RollerInput unmissable = new()
        {
            AttackerDamageModifier = attackerData.Stats[StoryCharacterData.StatMode.Mysticism] / 2, // or do only dice roll damage?
            DamageDice = spellEffect.DamageDice,
            DefenderDamageResist = 0, // do not reduce attribute damage
            AttackType = BattleRoller.AttackType.Undodgeable,
        };
        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, unmissable);

        string effectNamePrefix = spellEffect.EffectType == CharacterRoundEffect.EffectTypeMode.Attribute ?
            spellEffect.AttributeAffected.ToString() :
            spellEffect.StatAffected.ToString();

        CharacterRoundEffect roundEffect = new(

            name: spell.Name + ": " + spellEffect.Name,
            attributeAffected: spellEffect.AttributeAffected,
            statAffected: spellEffect.StatAffected,
            effectType: spellEffect.EffectType,
            permanent: false,
            cumulative: false,
            magnitude: -res.FinalDamage,
            animName: "DamageAttribute",
            rounds: spellEffect.NumRounds,
            fromSpell: spell.SpellMode,
            from: attackerData.Name
        );

        targetCharacter.CharacterData.DoEffectInitial(roundEffect);
    }

    private void DoAttributeStatFortify(SpellEffect spellEffect, Spell spell)
    {
        CharacterUnit originCharacter = spell.OriginCharacter;
        StoryCharacterData attackerData = originCharacter.CharacterData;
        CharacterUnit targetCharacter = spell.TargetCharacter;
        BattleRoller.RollerInput unmissable = new()
        {
            AttackerDamageModifier = attackerData.Stats[StoryCharacterData.StatMode.Mysticism] / 2, // or do only dice roll damage?
            DamageDice = spellEffect.DamageDice,
            DefenderDamageResist = 0, // do not reduce attribute damage
            AttackType = BattleRoller.AttackType.Undodgeable,
        };
        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, unmissable);
        GD.Print("amount: ", res.FinalDamage);
        CharacterRoundEffect roundEffect = new(

            name: spell.Name + ": " + spellEffect.Name,
            attributeAffected: spellEffect.AttributeAffected,
            statAffected: spellEffect.StatAffected,
            effectType: spellEffect.EffectType,
            permanent: false,
            cumulative: false,
            magnitude: res.FinalDamage,
            animName: "FortifyAttribute",
            rounds: spellEffect.NumRounds,
            fromSpell: spell.SpellMode,
            from: attackerData.Name
        );

        targetCharacter.CharacterData.DoEffectInitial(roundEffect);
    }

    private void DoBerserk(SpellEffect spellEffect, Spell spell)
    {
        CharacterUnit originCharacter = spell.OriginCharacter;
        StoryCharacterData attackerData = originCharacter.CharacterData;
        CharacterUnit targetCharacter = spell.TargetCharacter;
        CharacterRoundEffect roundEffect = new(

            name: spell.Name + ": " + spellEffect.Name,
            attributeAffected: spellEffect.AttributeAffected,
            statAffected: spellEffect.StatAffected,
            effectType: spellEffect.EffectType,
            permanent: false,
            cumulative: false,
            magnitude: 0,
            animName: "Berserk",
            rounds: spellEffect.NumRounds,
            fromSpell: spell.SpellMode,
            from: attackerData.Name
        );
        targetCharacter.CharacterData.DoEffectInitial(roundEffect);
    }

    private void DoDamageOverTimeEffect(SpellEffect spellEffect, Spell spell)
    {
        CharacterUnit originCharacter = spell.OriginCharacter;
        StoryCharacterData attackerData = originCharacter.CharacterData;
        CharacterUnit targetCharacter = spell.TargetCharacter;
        StoryCharacterData defenderData = targetCharacter.CharacterData;
        BattleRoller.RollerInput unmissable = new()
        {
            AttackerDamageModifier = attackerData.Stats[StoryCharacterData.StatMode.Mysticism] / 2, // or do only dice roll damage?
            DamageDice = spellEffect.DamageDice,
            DefenderDamageResist = defenderData.Stats[StoryCharacterData.StatMode.MysticResist],
            AttackType = BattleRoller.AttackType.Undodgeable,
        };

        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, unmissable);

        string effectNamePrefix = spellEffect.EffectType == CharacterRoundEffect.EffectTypeMode.Attribute ?
            spellEffect.AttributeAffected.ToString() :
            spellEffect.StatAffected.ToString();

        CharacterRoundEffect roundEffect = new(

            name: spell.Name + ": " + spellEffect.Name,
            attributeAffected: spellEffect.AttributeAffected,
            statAffected: spellEffect.StatAffected,
            effectType: spellEffect.EffectType,
            permanent: true,
            cumulative: true,
            magnitude: -res.FinalDamage,
            animName: "Damage",
            rounds: spellEffect.NumRounds,
            fromSpell: spell.SpellMode,
            from: attackerData.Name
        );

        targetCharacter.CharacterData.DoEffectInitial(roundEffect);
    }
    // private void DoAreaAttackEffect(SpellEffect spellEffect, Spell spell)
    // {
    //     CharacterUnit originCharacter = spell.OriginCharacter;
    //     CharacterUnit targetCharacter = spell.TargetCharacter;

    //     StoryCharacterData attackerData = originCharacter.CharacterData;
    //     StoryCharacterData defenderData = targetCharacter.CharacterData;

    //     BattleRoller.RollerInput magicAttack = new(
    //         attackerHitModifier: attackerData.GetCorrectHitBonus(spellEffect.Mystical),
    //         attackerDamageModifier: spellEffect.Mystical ? attackerData.Mysticism : attackerData.GetCorrectWeaponDamageBonus(),
    //         defenderDamageResist: spellEffect.Mystical ? defenderData.MysticResist : defenderData.PhysicalResist,
    //         damageDice: spellEffect.DamageDice,
    //         hexDistance: spell.HexDistance,
    //         targetGridPoint: spell.Destination,
    //         criticalThreshold: attackerData.CriticalThreshold,

    //         attackType: spellEffect.AttackType
    //     );
    //     int attackerHitModifier, int attackerDamageModifier, int defenderDamageResist, List< Tuple<int, int> > damageDice, int hexDistance, Vector2 targetGridPoint, List<Vector2> surroundingGridPoints, int criticalThreshold
    //     BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, magicAttack); // can potentially return this to improve the battle log!
    //     targetCharacter.TakeDamageOrder(res);
    // }



}
