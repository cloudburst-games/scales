using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
// using Godot.Collections;

// lesson for next time: allow multiple attributes or stats or lets say subeffects in one round effect
public partial class CharacterRoundEffect : RefCounted
{
    public enum EffectTypeMode { Attribute, Stat, Berserk, None }
    public string Name { get; private set; }
    public int RoundsRemaining { get; set; }
    public int CumulativeMagnitude { get; set; }
    public StoryCharacterData.AttributeMode AttributeAffected { get; private set; }
    public StoryCharacterData.StatMode StatAffected { get; private set; }
    public bool Permanent { get; private set; }
    public bool Cumulative { get; private set; }
    public EffectTypeMode EffectType { get; private set; }
    public int Magnitude { get; set; }
    public bool Started { get; set; } = false;
    public string AnimName { get; private set; }
    public string From { get; set; } // replace this with a character ID

    public SpellEffectManager.SpellMode FromSpell { get; private set; }

    public CharacterRoundEffect(
        string name,
        StoryCharacterData.AttributeMode attributeAffected,
        StoryCharacterData.StatMode statAffected,
        EffectTypeMode effectType,
        int rounds,
        bool permanent,
        bool cumulative,
        int magnitude,
        string animName,
        SpellEffectManager.SpellMode fromSpell,
        string from)
    {
        Name = name;
        AttributeAffected = attributeAffected;
        StatAffected = statAffected;
        EffectType = effectType;
        RoundsRemaining = rounds;
        Permanent = permanent;
        Cumulative = cumulative;
        Magnitude = magnitude;
        AnimName = animName;
        FromSpell = fromSpell;
        From = from;
    }
}

public partial class StoryCharacterData : RefCounted, IJSONSaveable
{



    public enum AttributeMode { Might, Resilience, Precision, Speed, Intellect, Charisma, Luck }
    public enum StatMode
    {
        Health, Endurance, HealthRegen, EnduranceRegen, MysticResist, PhysicalResist, Dodge,
        PhysicalDamageStrength, PhysicalDamagePrecision, Mysticism, Initiative, Leadership, CriticalThreshold,
        HitBonusStrength, HitBonusPrecision, Reagents, FocusCharge, Persuasion, PersuasionResist, ActionPoints,
        MoveSpeed, HitBonusWeapon, MaxEndurance, MaxHealth, MaxActionPoints,
        MaxReagents, MaxFocusCharge, PhysicalDamageRanged
    }

    public AnimationPlayer RoundEffectAnim { get; private set; }
    public CharacterUnit.TreeLink TreeLink { get; private set; }
    public Dictionary<AttributeMode, int> Attributes { get; set; }
    public Dictionary<StatMode, int> Stats { get; set; }
    public Dictionary<int, List<List<string>>> Barks { get; set; }
    public List<CharacterRoundEffect> CurrentEffects { get; set; } = new();


    public CharacterCheckpointData PackData()
    {
        // remove current effects first
        ReverseAllEffects();
        return new CharacterCheckpointData
        {
            Name = this.Name,
            Description = this.Description,
            PatronGod = this.PatronGod,
            BodyPath = this.BodyPath,
            PortraitPath = this.PortraitPath,
            AudioWalkPath = this.AudioWalkPath,
            AudioHurtPath = this.AudioHurtPath,
            AudioDiePath = this.AudioDiePath,
            AudioMeleePath = this.AudioMeleePath,
            Barks = this.Barks,
            CharacterBtnNormalPath = this.CharacterBtnNormalPath,
            CharacterBtnPressedPath = this.CharacterBtnPressedPath,
            _meleeWeaponEquipped = this._meleeWeaponEquipped,
            _rangedWeaponEquipped = this._rangedWeaponEquipped,
            ArmourClass = this.ArmourClass,
            MeleeWeaponDamageBonus = this.MeleeWeaponDamageBonus,
            RangedWeaponDamageBonus = this.RangedWeaponDamageBonus,
            Perks = this.Perks.Select(x => (int)x).ToList(),
            Level = this.Level,
            Might = this.Attributes[AttributeMode.Might],
            Resilience = this.Attributes[AttributeMode.Resilience],
            Precision = this.Attributes[AttributeMode.Precision],
            Speed = this.Attributes[AttributeMode.Speed],
            Intellect = this.Attributes[AttributeMode.Intellect],
            Charisma = this.Attributes[AttributeMode.Charisma],
            Luck = this.Attributes[AttributeMode.Luck],
        };
    }
    private void DoEffects()
    {
        foreach (CharacterRoundEffect effect in CurrentEffects.ToList()) // because ReverseEffects modifies the collection and occurs at the same time
        {
            if (effect.RoundsRemaining > 0)
            {
                DoEffectSingle(effect);
            }
        }
    }

    private void DoEffectSingle(CharacterRoundEffect effect)
    {
        // GD.Print("doing effect");
        if (!effect.Started || effect.Cumulative)
        {
            // GD.Print("applying effect");
            ApplyEffect(effect);
            effect.Started = true;
        }

        effect.RoundsRemaining -= 1;
        // GD.Print("effect rounds is now: ", effect.RoundsRemaining);
        if (effect.RoundsRemaining <= 0)
        {
            // GD.Print("reversing effect");
            ReverseEffect(effect);
        }
    }

    public void DoEffectInitial(CharacterRoundEffect effect)
    {
        // overwrite prev effect. we dont want to use name ideally in the future when refactoring 
        CharacterRoundEffect existingEffect = CurrentEffects.FirstOrDefault(x => x.Name == effect.Name);
        if (effect.Name.StartsWith("Leadership") || effect.Name.StartsWith("Scales") || effect.Name.StartsWith("Disharmony"))
        {
            existingEffect = CurrentEffects.FirstOrDefault(x => x.From == effect.From);
        }
        if (existingEffect != null)
        {
            ReverseEffect(existingEffect);
        }

        CurrentEffects.Add(effect);
        DoEffectSingle(effect);
    }

    private void ApplyEffect(CharacterRoundEffect effect, bool maintaining = false)
    {
        if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Attribute)
        {
            // GD.Print("att b4: ", Attributes[effect.AttributeAffected]);
            // GD.Print("health b4 should be: ", GetUpdatedHealth());
            // GD.Print(Stats[StatMode.MaxHealth]);
            // GD.Print(GetCorrectHitBonus());
            if (Attributes[effect.AttributeAffected] + effect.Magnitude < 1)
            {
                effect.Magnitude = 1 - Attributes[effect.AttributeAffected];
            }
            Attributes[effect.AttributeAffected] += effect.Magnitude;

            UpdateAllStats(maxOnly: true);
            // GD.Print(Stats[StatMode.MaxHealth]);
            // GD.Print(GetCorrectHitBonus());
            // GD.Print("att now: ", Attributes[effect.AttributeAffected]);
            // GD.Print("health nao: ", GetUpdatedHealth());
        }
        else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Stat)
        {
            if (effect.StatAffected == StatMode.HealthRegen)
            {
                GD.Print("health regen was before: ", Stats[effect.StatAffected]);
            }
            Stats[effect.StatAffected] += effect.Magnitude;
            if (effect.StatAffected == StatMode.HealthRegen)
            {
                GD.Print("and now: ", Stats[effect.StatAffected]);
            }
            // GD.Print("stat now: ", Stats[effect.StatAffected]);
        }
        else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Berserk)
        {
            Berserk = true;
        }
        if (!maintaining)
        {
            effect.CumulativeMagnitude += effect.Magnitude;

            if (RoundEffectAnim.HasAnimation(effect.AnimName))
            {
                RoundEffectAnim.Play(effect.AnimName);
                TreeLink.EmitSignal(CharacterUnit.TreeLink.SignalName.RoundEffectApplied, effect);
            }
        }
    }

    private void ReverseEffect(CharacterRoundEffect effect)
    {
        if (!effect.Permanent)
        {
            if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Attribute)
            {
                Attributes[effect.AttributeAffected] -= effect.CumulativeMagnitude;
                UpdateAllStats(maxOnly: true);
            }
            else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Stat)
            {
                Stats[effect.StatAffected] -= effect.CumulativeMagnitude;
            }
            else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Berserk)
            {
                Berserk = false;
            }
            TreeLink.EmitSignal(CharacterUnit.TreeLink.SignalName.RoundEffectEnded, effect);
        }
        // GD.Print("rev att now: ", Attributes[effect.AttributeAffected]);
        // GD.Print("rev stat now: ", Stats[effect.StatAffected]);
        CurrentEffects.Remove(effect);
    }

    // intended to run after a battle to clear all effects
    public void ReverseAllEffects()
    {
        foreach (CharacterRoundEffect effect in CurrentEffects.ToList())
        {
            ReverseEffect(effect);
        }
        UpdateAllStats();
    }

    public void OnNewRound()
    {
        DoEffects();
        GD.Print("health regen for gilga: ", Name + ", " + Stats[StatMode.HealthRegen]);
    }
    public void Initialise(AnimationPlayer roundEffectAnim, CharacterUnit.TreeLink treeLink)
    {
        RoundEffectAnim = roundEffectAnim;
        TreeLink = treeLink;
        Attributes = new() { { AttributeMode.Might, Might },
            { AttributeMode.Resilience, Resilience },
            { AttributeMode.Precision, Precision },
            { AttributeMode.Speed, Speed },
            { AttributeMode.Intellect, Intellect },
            { AttributeMode.Charisma, Charisma },
            { AttributeMode.Luck, Luck }, };
        Stats = new() {
            { StatMode.HealthRegen, GetUpdatedHealthRegen() },
            { StatMode.EnduranceRegen, GetUpdatedEnduranceRegen() },
            { StatMode.MysticResist, GetUpdatedMysticResist() },
            { StatMode.PhysicalResist, GetUpdatedPhysicalResist() },
            { StatMode.Dodge, GetUpdatedDodge() },
            { StatMode.PhysicalDamageStrength, GetUpdatedPhysicalDamageStrength() },
            { StatMode.PhysicalDamagePrecision, GetUpdatedPhysicalDamagePrecision() },
            { StatMode.Mysticism, GetUpdatedMysticism() },
            { StatMode.Initiative, GetUpdatedInitiative() },
            { StatMode.Leadership, GetUpdatedLeadership() },
            { StatMode.CriticalThreshold, GetUpdatedCriticalThreshold() },
            { StatMode.HitBonusStrength, GetUpdatedHitBonusStrength() },
            { StatMode.HitBonusPrecision, GetUpdatedHitBonusPrecision() },
            { StatMode.Persuasion, GetUpdatedPersuasion() },
            { StatMode.PersuasionResist, GetUpdatedPersuasionResist() },
            { StatMode.MoveSpeed, GetUpdatedMoveSpeed() },
            { StatMode.ActionPoints, GetUpdatedActionPoints() },
            { StatMode.MaxActionPoints, GetUpdatedActionPoints() },
            { StatMode.Health, GetUpdatedHealth() },
            { StatMode.MaxHealth, GetUpdatedHealth() },
            { StatMode.Endurance, GetUpdatedEndurance() },
            { StatMode.MaxEndurance, GetUpdatedEndurance() },
            { StatMode.Reagents, GetUpdatedReagents() },
            { StatMode.MaxReagents, GetUpdatedReagents() },
            { StatMode.FocusCharge, GetUpdatedFocusCharge() },
            { StatMode.MaxFocusCharge, GetUpdatedFocusCharge() },
            { StatMode.PhysicalDamageRanged, GetUpdatedPhysicalDamageRanged() }
        };
        UpdateKnownSpells();
        UpdateAllStats();
    }


    public void UpdateAllStats(bool maxOnly = false)
    {
        Stats[StatMode.MaxHealth] = GetUpdatedHealth();
        Stats[StatMode.MaxEndurance] = GetUpdatedEndurance();
        Stats[StatMode.HealthRegen] = GetUpdatedHealthRegen();
        Stats[StatMode.EnduranceRegen] = GetUpdatedEnduranceRegen();
        Stats[StatMode.MysticResist] = GetUpdatedMysticResist();
        Stats[StatMode.PhysicalResist] = GetUpdatedPhysicalResist();
        Stats[StatMode.Dodge] = GetUpdatedDodge();
        Stats[StatMode.PhysicalDamageStrength] = GetUpdatedPhysicalDamageStrength();
        Stats[StatMode.PhysicalDamagePrecision] = GetUpdatedPhysicalDamagePrecision();
        Stats[StatMode.Mysticism] = GetUpdatedMysticism();
        Stats[StatMode.Initiative] = GetUpdatedInitiative();
        Stats[StatMode.Leadership] = GetUpdatedLeadership();
        Stats[StatMode.CriticalThreshold] = GetUpdatedCriticalThreshold();
        Stats[StatMode.HitBonusStrength] = GetUpdatedHitBonusStrength();
        Stats[StatMode.HitBonusPrecision] = GetUpdatedHitBonusPrecision();
        Stats[StatMode.MaxReagents] = GetUpdatedReagents();
        Stats[StatMode.MaxFocusCharge] = GetUpdatedFocusCharge();
        Stats[StatMode.Persuasion] = GetUpdatedPersuasion();
        Stats[StatMode.PersuasionResist] = GetUpdatedPersuasionResist();
        Stats[StatMode.MaxActionPoints] = GetUpdatedActionPoints();
        Stats[StatMode.MoveSpeed] = GetUpdatedMoveSpeed();
        Stats[StatMode.PhysicalDamageRanged] = GetUpdatedPhysicalDamageRanged();

        if (!maxOnly)
        {
            Stats[StatMode.Health] = GetUpdatedHealth();
            Stats[StatMode.Endurance] = GetUpdatedEndurance();
            Stats[StatMode.Reagents] = GetUpdatedReagents();
            Stats[StatMode.FocusCharge] = GetUpdatedFocusCharge();
            Stats[StatMode.ActionPoints] = GetUpdatedActionPoints();
        }

        // we reset stat so reapply effect
        foreach (CharacterRoundEffect e in CurrentEffects)
        {
            if (e.EffectType == CharacterRoundEffect.EffectTypeMode.Stat && !IsNumeratorStat(e.StatAffected))
            {
                ApplyEffect(e, maintaining: true);
            }
        }
    }

    private bool IsNumeratorStat(StatMode stat)
    {
        return stat == StatMode.Health || stat == StatMode.Endurance || stat == StatMode.Reagents || stat == StatMode.FocusCharge || stat == StatMode.ActionPoints;
    }


    private int GetUpdatedHealth()
    {
        return UpdateStat(Attributes[AttributeMode.Might], 1.75f, 0.025f) + UpdateStat(Attributes[AttributeMode.Resilience], 1f, 0.025f);
    }

    private int GetUpdatedEndurance()
    {
        return UpdateStat(Attributes[AttributeMode.Might], 1, 0.025f) +
            UpdateStat(Attributes[AttributeMode.Resilience], 2, 0.025f);
    }

    private int GetUpdatedHealthRegen()
    {
        return UpdateStat(Attributes[AttributeMode.Resilience], 0.2f, 0.025f);
    }

    private int GetUpdatedEnduranceRegen()
    {
        return UpdateStat(Attributes[AttributeMode.Resilience], 0.4f, 0.025f);
    }

    private int GetUpdatedMysticResist()
    {
        return UpdateStat(Attributes[AttributeMode.Resilience], 0.2f, 0.025f) +
            UpdateStat(Attributes[AttributeMode.Intellect], 0.2f, 0.05f) +
            UpdateStat(Attributes[AttributeMode.Luck], 0.1f, 0.05f);
    }

    private int GetUpdatedPhysicalResist()
    {
        return UpdateStat(ArmourClass, 0.4f, 0.025f) + UpdateStat(Attributes[AttributeMode.Resilience], 0.2f, 0.025f);
    }

    private int GetUpdatedDodge()
    {
        return UpdateStat(Attributes[AttributeMode.Speed], 0.2f, 0.025f) + UpdateStat(Attributes[AttributeMode.Luck], 0.1f, 0.05f);
    }

    private int GetUpdatedPhysicalDamageStrength()
    {
        return UpdateStat(Attributes[AttributeMode.Might], 0.35f, 0.05f);
    }

    private int GetUpdatedPhysicalDamagePrecision()
    {
        return UpdateStat(Attributes[AttributeMode.Precision], 0.4f, 0.05f);
    }

    private int GetUpdatedPhysicalDamageRanged()
    {
        return UpdateStat(Attributes[AttributeMode.Precision], 0.3f, 0.05f);
    }

    private int GetUpdatedMysticism()
    {
        return UpdateStat(Attributes[AttributeMode.Intellect], 0.4f, 0.045f);
    }

    private int GetUpdatedInitiative()
    {
        return UpdateStat(Attributes[AttributeMode.Speed], 1.5f, 0.02f);
    }

    private int GetUpdatedLeadership()
    {
        return UpdateStat(Attributes[AttributeMode.Charisma], 1f, 0.025f);
    }

    private int GetUpdatedCriticalThreshold()
    {
        return Math.Max(11,
            20 - UpdateStat(Attributes[AttributeMode.Precision], 0.1f, 0.025f) - UpdateStat(Attributes[AttributeMode.Luck], 0.2f, 0.02f));
    }

    private int GetUpdatedHitBonusStrength()
    {
        return UpdateStat(Attributes[AttributeMode.Might], 0.85f, 0.02f) + UpdateStat(Attributes[AttributeMode.Precision], 0.25f, 0.02f);
    }
    private int GetUpdatedHitBonusPrecision()
    {
        return UpdateStat(Attributes[AttributeMode.Precision], 1.25f, 0.02f);
    }

    // public int GetCorrectHitBonus(bool mystical = false)
    // {
    //     return mystical ? HitBonusPrecision + (WeaponTypeEquipped == WeaponTypeEquippedMode.Magical ? HitBonusWeapon : 0)
    //         : WeaponTypeEquipped == WeaponTypeEquippedMode.Strength ? HitBonusWeapon + HitBonusStrength
    //         : HitBonusWeapon + HitBonusPrecision;
    // }
    public int GetCorrectHitBonusMelee(bool mystical = false)
    {
        if (mystical)
        {
            // if (WeaponTypeEquipped == WeaponTypeEquippedMode.Magical)
            // {
            //     return Stats[StatMode.HitBonusPrecision] + HitBonusWeaponRanged;
            // }
            // else
            // {
            return Stats[StatMode.HitBonusPrecision];
            // }
        }
        if (WeaponTypeEquipped == WeaponTypeEquippedMode.Strength)
        {
            return Stats[StatMode.HitBonusStrength] + HitBonusWeaponMelee;
        }
        else
        {
            return Stats[StatMode.HitBonusPrecision] + HitBonusWeaponMelee;
        }
    }
    public int GetCorrectHitBonusRanged(bool mystical = false)
    {
        if (mystical)
        {
            // if (WeaponTypeEquipped == WeaponTypeEquippedMode.Magical)
            // {
            return Stats[StatMode.HitBonusPrecision];
        }
        else
        {
            return Stats[StatMode.HitBonusPrecision] + HitBonusWeaponRanged;
        }
        // }
        // else if (WeaponTypeEquipped == WeaponTypeEquippedMode.Strength)
        // {
        //     return Stats[StatMode.HitBonusStrength] + HitBonusWeaponMelee;
        // }
        // else
        // {
        //     return Stats[StatMode.HitBonusPrecision] + HitBonusWeaponMelee;
        // }
    }

    public int GetCorrectMeleeWeaponDamageBonus()
    {
        return WeaponTypeEquipped == WeaponTypeEquippedMode.Strength ? Stats[StatMode.PhysicalDamageStrength] + MeleeWeaponDamageBonus : Stats[StatMode.PhysicalDamagePrecision] + MeleeWeaponDamageBonus;
    }
    public int GetCorrectRangedWeaponDamageBonus()
    {
        return Stats[StatMode.PhysicalDamageRanged] + RangedWeaponDamageBonus;
    }
    private int GetUpdatedReagents()
    {
        return UpdateStat(Attributes[AttributeMode.Intellect], 1f, 0.02f);
    }
    private int GetUpdatedFocusCharge()
    {
        return UpdateStat(Attributes[AttributeMode.Intellect], 2f, 0.015f);
    }
    private int GetUpdatedPersuasion()
    {
        return UpdateStat(Attributes[AttributeMode.Intellect], 0.1f, 0.05f) + UpdateStat(Attributes[AttributeMode.Charisma], 0.4f, 0.01f);
    }
    private int GetUpdatedPersuasionResist()
    {
        return UpdateStat(Attributes[AttributeMode.Intellect], 0.5f, 0.025f);
    }
    private int GetUpdatedActionPoints()
    {
        return UpdateStat(Attributes[AttributeMode.Speed], 1f, 0.03f);
    }
    private int GetUpdatedMoveSpeed()
    {
        return UpdateStat(Attributes[AttributeMode.Speed], 0.75f, 0.03f) * 100;
    }
    private void UpdateKnownSpells()
    {
        // this should be called whenever perks are updated
        KnownSpells.Clear();
        foreach (Perk.PerkMode perkMode in _perks)
        {
            Perk p = PerkFactory.GeneratePerk(perkMode);
            if (p.Category == Perk.PerkCategory.Spell)
            {
                if (!KnownSpells.Contains(p.AssociatedSpell))
                {
                    KnownSpells.Add(p.AssociatedSpell);
                }
            }
        }
    }

    public enum MeleeWeaponMode
    {
        None, WoodenKnuckles, BrassKnuckles, BluntKnife, JewelledDagger,
        Natural
    }

    [JsonProperty("_meleeWeaponEquipped")]
    private int _meleeWeaponEquipped = 0;

    // TODO -set all equipment in json and have it load into this.. or a InventoryCharacterData class. should make equipment classes too and weapon classes that compose etc.
    public MeleeWeaponMode MeleeWeaponEquipped
    {
        set
        {
            _meleeWeaponEquipped = (int)value;
            switch ((MeleeWeaponMode)_meleeWeaponEquipped)
            {
                case MeleeWeaponMode.None:
                    WeaponDiceMelee = new() { new(1, 4) };
                    // MeleeWeaponDamageBonus = 0;
                    HitBonusWeaponMelee = 0;
                    WeaponTypeEquipped = WeaponTypeEquippedMode.Strength;
                    break;
                case MeleeWeaponMode.WoodenKnuckles:
                    WeaponDiceMelee = new() { new(1, 6) };
                    // MeleeWeaponDamageBonus = 1;
                    HitBonusWeaponMelee = 1;
                    WeaponTypeEquipped = WeaponTypeEquippedMode.Strength;
                    break;
                case MeleeWeaponMode.BrassKnuckles:
                    WeaponDiceMelee = new() { new(1, 8) };
                    // MeleeWeaponDamageBonus = 2;
                    HitBonusWeaponMelee = 3;
                    WeaponTypeEquipped = WeaponTypeEquippedMode.Strength;
                    break;
                case MeleeWeaponMode.BluntKnife:
                    WeaponDiceMelee = new() { new(1, 4) };
                    // MeleeWeaponDamageBonus = 2;
                    HitBonusWeaponMelee = 2;
                    WeaponTypeEquipped = WeaponTypeEquippedMode.Precision;
                    break;
                case MeleeWeaponMode.JewelledDagger:
                    WeaponDiceMelee = new() { new(2, 4) };
                    // MeleeWeaponDamageBonus = 3;
                    HitBonusWeaponMelee = 3;
                    WeaponTypeEquipped = WeaponTypeEquippedMode.Precision;
                    break;
                case MeleeWeaponMode.Natural:
                    WeaponDiceMelee = new() { new(2, 4) };
                    // MeleeWeaponDamageBonus = 2;
                    WeaponTypeEquipped = WeaponTypeEquippedMode.Strength;
                    break;
            }
        }
        get
        {
            return (MeleeWeaponMode)_meleeWeaponEquipped;
        }
    }

    public enum RangedWeaponMode { None, Sling, Rock, Lightning, Javelin }

    [JsonProperty("_rangedWeaponEquipped")]
    private int _rangedWeaponEquipped = 0;

    // TODO -set all equipment in json and have it load into this.. or a InventoryCharacterData class
    public RangedWeaponMode RangedWeaponEquipped
    {
        set
        {
            _rangedWeaponEquipped = (int)value;
            switch ((RangedWeaponMode)_rangedWeaponEquipped)
            {
                case RangedWeaponMode.None:
                    WeaponDiceRanged = new() { new(0, 0) };
                    // RangedWeaponDamageBonus = 0;
                    HitBonusWeaponRanged = 0;
                    break;
                case RangedWeaponMode.Sling:
                    WeaponDiceRanged = new() { new(1, 4) };
                    // RangedWeaponDamageBonus = 1;
                    HitBonusWeaponRanged = 1;
                    break;
                case RangedWeaponMode.Rock:
                    WeaponDiceRanged = new() { new(1, 8) };
                    // RangedWeaponDamageBonus = 2;
                    HitBonusWeaponRanged = 2;
                    break;
                case RangedWeaponMode.Lightning:
                    WeaponDiceRanged = new() { new(2, 6) };
                    // RangedWeaponDamageBonus = 3;
                    HitBonusWeaponRanged = 3;
                    break;
                case RangedWeaponMode.Javelin:
                    WeaponDiceRanged = new() { new(1, 8) };
                    // RangedWeaponDamageBonus = 1;
                    HitBonusWeaponRanged = 2;
                    break;
            }
        }
        get
        {
            return (RangedWeaponMode)_rangedWeaponEquipped;
        }
    }


    public double GetAverageWeaponDiceDamage(Tuple<int, int> dice)
    {
        return (dice.Item2 + 1.0) / 2.0 * dice.Item1;
    }

    // Higher 'multiplier' results in a higher output value.
    // Higher 'constant' increases the effect of diminishing returns (i.e. smaller result)
    private int UpdateStat(int att, float multiplier, float constant)
    {
        return (int)(att * multiplier / (1 + (att * constant)));
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public string PatronGod { get; set; }
    public string BodyPath { get; set; }
    public string PortraitPath { get; set; }
    public string CharacterBtnNormalPath { get; set; }
    public string CharacterBtnPressedPath { get; set; }
    public List<string> AudioWalkPath { get; set; }
    public List<string> AudioHurtPath { get; set; }
    public List<string> AudioDiePath { get; set; }
    public List<string> AudioMeleePath { get; set; }
    private List<Perk.PerkMode> _perks = new();
    public List<Perk.PerkMode> Perks
    {
        get
        {
            return _perks;
        }
        set
        {
            _perks = value;
            UpdateKnownSpells();
        }
    } // { get; set; } // 0/1/2/3/4/5/6/7 can be known spells ? .. in future separate perks from spells! (post-jam)


    // in future make a Perk class
    // public enum PerkMode
    // {
    //     SolarFlare, SolarBlast, JudgementOfFlame, BlindingLight, VialOfFury, ElixirOfVigour, ElixirOfSwiftness, RegenerativeOintment,
    //     LesserArmor, GreaterArmor, WeaponSpikes, EnchantedWeapon
    // }


    public Godot.Collections.Array<SpellEffectManager.SpellMode> KnownSpells { get; set; } = new();
    public int Level { get; set; }

    // Attributes set by JSON - todo - change all names to _initialMight etc and make them private (chatgpt says it would still work!)
    public int Might { private get; set; }
    public int Resilience { private get; set; }
    public int Precision { private get; set; }
    public int Speed { private get; set; }
    public int Intellect { private get; set; }
    public int Charisma { private get; set; }
    public int Luck { private get; set; }

    // DERIVED STATS: // Derived stats: ingame, these will be adjusted by attributes and other variables.   
    // public int MaxHealth { get; set; } = 10;
    // public int MaxEndurance { get; set; } = 10; // every action costs endurance (endurance regen will ensure can always do something)
    // public int HealthRegen { get; set; } = 2;
    // public int EnduranceRegen { get; set; } = 3;
    // public int MysticResist { get; set; } = 5;
    // public int PhysicalResist { get; set; } = 4;
    // public int Dodge { get; set; } = 3;
    // public int PhysicalDamageStrength { get; set; } = 3;
    // public int PhysicalDamagePrecision { get; set; } = 4;
    // public int Mysticism { get; set; } = 4; // increases power of magical effects
    // public int Initiative { get; set; } = 10;
    // public int Leadership { get; set; } = 10;
    // public int CriticalThreshold { get; set; } = 20; // This can reduce depending your luck to 19, 18, etc. to a minimum of x (11?)
    public int ArmourClass { get; set; } = 0; // Should be updated when changing armour
    public int MeleeWeaponDamageBonus { get; set; } = 0; // Should be updated when changing weapon
    public List<Tuple<int, int>> WeaponDiceMelee { get; set; } = new() {new Tuple<int,int>(1,4), // should be updated when changing weapon
        new Tuple<int,int>(1,6)}; // e.g. 2d4 + 1d6 -> post-jam will need to change be explicit about damage types, and maybe introduce damage type resistances


    public List<Tuple<int, int>> WeaponDiceRanged { get; set; } = new() {new Tuple<int,int>(1,4), // should be updated when changing weapon
        new Tuple<int,int>(1,4)}; // e.g. 2d4 + 1d6 -> post-jam will need to change be explicit about damage types, and maybe introduce damage type resistances

    // // the below are intended to be modified by attributes and perks
    // public int HitBonusStrength { get; set; } = 0;
    // public int HitBonusPrecision { get; set; } = 0; // used for magic as well

    // // the below are intended to be modified by equipped weapons
    public int HitBonusWeaponMelee { get; set; } = 0;
    public int HitBonusWeaponRanged { get; set; } = 0;

    public enum WeaponTypeEquippedMode { Strength, Precision, Magical }
    // // This should be modified by the weapon equipped, e.g. strength or precision (for most precision weapons or magical weapons)
    public WeaponTypeEquippedMode WeaponTypeEquipped = WeaponTypeEquippedMode.Strength;



    // // modified by the selected action, 
    // // e.g. if attacking with a weapon, associated weapon perks will increase this hit bonus. if casting, associated magical perks will increase this hit bonus.
    // // with strength based weapons, should add StrengthHit, same with dex based weapons and precision, and magic based and mysticism
    // // public int HitBonus { get; set; } = 3;
    // public int MaxReagents { get; set; } = 5; // different spells cost different amount, some cost zero, maybe increased by intellect? + perks like forager. Increases by 10% per round.
    // public int MaxFocusCharge { get; set; } = 10; // increases by 20% per round
    // public int Persuasion { get; set; } = 3; // at 10% health, enemies may surrender at low persuasion: roll a d20 against their persuasion defence
    // public int PersuasionResist { get; set; } = 10;
    // public int MaxActionPoints { get; set; } = 5;
    // public int Experience { get; set; } = 0;
    // public int MoveSpeed { get; set; } = 300; // out of combat move speed, analagous to action points in combat

    // // derived from derived stats, and change during e.g. during battle
    // public int ActionPoints { get; set; } = 5;
    public bool Alive { get; set; } = true;
    public bool Berserk { get; private set; } = false;
    public int RangedWeaponDamageBonus { get; set; }
    // public int Health { get; set; } = 10; // e.g. this will be adjusted by endurance, vigour, etc.
    // public int Endurance { get; set; } = 10;
    // public int Reagents { get; set; } = 5;
    // public int FocusCharge { get; set; } = 10;
    ////


}
