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
        public enum TargetMode { Ally, Enemy, Ground }
        public TargetMode Target { get; set; }
        public int Range { get; set; }
        public int Area { get; set; } = 0;
        public BattleRoller.RollerOutcomeInformation Outcome { get; set; }

        public Godot.Collections.Array<CharacterUnit> AreaAffectedCharacters = new();
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
    public enum SpellEffectMode { Fire, Physical, Fireball }
    public enum SpellMode { SolarFlare, SolarBlast, JudgementOfFlame, BlindingLight, VialOfFury, ElixirOfVigour, ElixirOfSwiftness, RegenerativeOintment, Arrow, None }
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
        _allSpellEffects[SpellEffectMode.Fireball] = new()
        {
            DamageDice = new() { new Tuple<int, int>(2, 6) },
            AttackType = BattleRoller.AttackType.Area,
            Mystical = true,
            EffectMethod = DoAreaAttackEffect,
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
        _allSpellVisuals[SpellMode.SolarBlast] = new() // todo!
        {
            SpellEffectScn = _spellVisualScns[SpellMode.SolarBlast],
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
            Name = "Arrow",
            Range = 8,
            Target = Spell.TargetMode.Enemy
        };
        AllSpells[SpellMode.SolarFlare] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.SolarFlare],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.Fire] },
            Name = "Solar Flare",
            Range = 8,
            Target = Spell.TargetMode.Enemy
        };
        AllSpells[SpellMode.SolarBlast] = new()
        {
            SpellEffectVisual = _allSpellVisuals[SpellMode.SolarBlast],
            AssociatedEffects = new() { _allSpellEffects[SpellEffectMode.Fireball] },
            Name = "Solar Blast",
            Range = 10,
            Target = Spell.TargetMode.Ground,
            Area = 1,
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

        CurrentLevel.AddChild(spellVisual); // consdier adding under an entity group // 2 days later- what does this comment mean?

        spellVisual.Finished += this.OnSpellFinished;
        spellVisual.SetSpellEffectState(spell.SpellEffectVisual.VisualMode);

        spellVisual.Start(spell);
    }

    [Signal]
    public delegate void AreaHitCalculatedEventHandler(Spell spell);

    public void CalculateAreaHit(Spell spell)
    {

        BattleRoller.RollerInput areaAttack = new();
        areaAttack.CriticalThreshold = spell.OriginCharacter.CharacterData.CriticalThreshold;
        areaAttack.AttackerHitModifier = spell.OriginCharacter.CharacterData.GetCorrectHitBonus(spell.AssociatedEffects[0].Mystical);
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
            AttackerHitModifier = attackerData.GetCorrectHitBonus(spellEffect.Mystical),
            DefenderDodgeModifier = defenderData.Dodge,
            AttackerDamageModifier = spellEffect.Mystical ? attackerData.Mysticism : attackerData.GetCorrectWeaponDamageBonus(),
            DefenderDamageResist = spellEffect.Mystical ? defenderData.MysticResist : defenderData.PhysicalResist,
            DamageDice = spellEffect.DamageDice,
            CriticalThreshold = attackerData.CriticalThreshold,
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
                AttackerHitModifier = attackerData.GetCorrectHitBonus(spellEffect.Mystical),
                DefenderDodgeModifier = defenderData.Dodge,
                AttackerDamageModifier = spellEffect.Mystical ? attackerData.Mysticism : attackerData.GetCorrectWeaponDamageBonus(),
                DefenderDamageResist = spellEffect.Mystical ? defenderData.MysticResist : defenderData.PhysicalResist,
                DamageDice = spellEffect.DamageDice,
                CriticalThreshold = attackerData.CriticalThreshold,
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
