using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
// using Godot.Collections;

public partial class CharacterRoundEffect : RefCounted
{
    public enum EffectTypeMode { Attribute, Stat, Berserk }
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

    public CharacterRoundEffect(
        string name,
        StoryCharacterData.AttributeMode attributeAffected,
        StoryCharacterData.StatMode statAffected,
        EffectTypeMode effectType,
        int rounds,
        bool permanent,
        bool cumulative,
        int magnitude,
        string animName)
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
    }
}

public class StoryCharacterData : IJSONSaveable
{



    public enum AttributeMode { Might, Resilience, Precision, Speed, Intellect, Charisma, Luck }
    public enum StatMode
    {
        Health, Endurance, HealthRegen, EnduranceRegen, MysticResist, PhysicalResist, Dodge,
        PhysicalDamageStrength, PhysicalDamagePrecision, Mysticism, Initiative, Leadership, CriticalThreshold,
        HitBonusStrength, HitBonusPrecision, Reagents, FocusCharge, Persuasion, PersuasionResist, ActionPoints,
        MoveSpeed,

        HitBonusWeapon,
        MaxEndurance,
        MaxHealth,
        MaxActionPoints,
        MaxReagents,
        MaxFocusCharge
    }

    public AnimationPlayer RoundEffectAnim { get; private set; }
    public CharacterUnit.TreeLink TreeLink { get; private set; }
    public Dictionary<AttributeMode, int> Attributes { get; set; }
    public Dictionary<StatMode, int> Stats { get; set; }

    public List<CharacterRoundEffect> CurrentEffects { get; set; } = new();

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
        if (existingEffect != null)
        {
            ReverseEffect(existingEffect);
        }

        CurrentEffects.Add(effect);
        DoEffectSingle(effect);
    }

    private void ApplyEffect(CharacterRoundEffect effect)
    {
        if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Attribute)
        {
            Attributes[effect.AttributeAffected] += effect.Magnitude; // can be negative!
            GD.Print("att now: ", Attributes[effect.AttributeAffected]);
        }
        else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Stat)
        {
            Stats[effect.StatAffected] += effect.Magnitude;
            GD.Print("stat now: ", Stats[effect.StatAffected]);
        }
        else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Berserk)
        {
            Berserk = true;
        }
        effect.CumulativeMagnitude += effect.Magnitude;

        if (RoundEffectAnim.HasAnimation(effect.AnimName))
        {
            RoundEffectAnim.Play(effect.AnimName);
            TreeLink.EmitSignal(CharacterUnit.TreeLink.SignalName.RoundEffectApplied, effect);
        }
    }

    private void ReverseEffect(CharacterRoundEffect effect)
    {
        if (!effect.Permanent)
        {
            if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Attribute)
            {
                Attributes[effect.AttributeAffected] -= effect.CumulativeMagnitude;
            }
            else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Stat)
            {
                Stats[effect.StatAffected] -= effect.CumulativeMagnitude;
            }
            else if (effect.EffectType == CharacterRoundEffect.EffectTypeMode.Berserk)
            {
                Berserk = false;
            }
        }
        GD.Print("rev att now: ", Attributes[effect.AttributeAffected]);
        GD.Print("rev stat now: ", Stats[effect.StatAffected]);
        CurrentEffects.Remove(effect);
    }

    // intended to run after a battle to clear all effects
    public void ReverseAllEffects()
    {
        foreach (CharacterRoundEffect effect in CurrentEffects)
        {
            ReverseEffect(effect);
        }
    }

    public void OnNewRound()
    {
        DoEffects();
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
        };
        UpdateKnownSpells();
        UpdateAllStats();
    }

    public void UpdateAllStats()
    {
        Stats[StatMode.Health] = Stats[StatMode.MaxHealth] = GetUpdatedHealth();
        Stats[StatMode.Endurance] = Stats[StatMode.MaxEndurance] = GetUpdatedEndurance();
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
        Stats[StatMode.Reagents] = Stats[StatMode.MaxReagents] = GetUpdatedReagents();
        Stats[StatMode.FocusCharge] = Stats[StatMode.MaxFocusCharge] = GetUpdatedFocusCharge();
        Stats[StatMode.Persuasion] = GetUpdatedPersuasion();
        Stats[StatMode.PersuasionResist] = GetUpdatedPersuasionResist();
        Stats[StatMode.ActionPoints] = Stats[StatMode.MaxActionPoints] = GetUpdatedActionPoints();
        Stats[StatMode.MoveSpeed] = GetUpdatedMoveSpeed();
    }


    // todo
    // updatearmour - based on equipped
    // updateweapon - based on equipped

    private int GetUpdatedHealth()
    {
        return UpdateStat(Might, 3, 0.025f);
    }

    private int GetUpdatedEndurance()
    {
        return UpdateStat(Might, 1, 0.025f) +
            UpdateStat(Resilience, 2, 0.025f);
    }

    private int GetUpdatedHealthRegen()
    {
        return UpdateStat(Resilience, 0.2f, 0.025f);
    }

    private int GetUpdatedEnduranceRegen()
    {
        return UpdateStat(Resilience, 0.4f, 0.025f);
    }

    private int GetUpdatedMysticResist()
    {
        return UpdateStat(Resilience, 0.2f, 0.025f) +
            UpdateStat(Intellect, 0.1f, 0.05f) +
            UpdateStat(Luck, 0.1f, 0.05f);
    }

    private int GetUpdatedPhysicalResist()
    {
        return UpdateStat(ArmourClass, 1f, 0.025f);
    }

    private int GetUpdatedDodge()
    {
        return UpdateStat(Speed, 0.2f, 0.025f) + UpdateStat(Luck, 0.1f, 0.05f);
    }

    private int GetUpdatedPhysicalDamageStrength()
    {
        return UpdateStat(Might, 0.25f, 0.05f);
    }

    private int GetUpdatedPhysicalDamagePrecision()
    {
        return UpdateStat(Precision, 0.5f, 0.05f);
    }

    private int GetUpdatedMysticism()
    {
        return UpdateStat(Intellect, 0.6f, 0.035f);
    }

    private int GetUpdatedInitiative()
    {
        return UpdateStat(Speed, 1.5f, 0.02f);
    }

    private int GetUpdatedLeadership()
    {
        return UpdateStat(Charisma, 1f, 0.025f);
    }

    private int GetUpdatedCriticalThreshold()
    {
        return Math.Max(11,
            20 - UpdateStat(Precision, 0.1f, 0.025f) - UpdateStat(Luck, 0.2f, 0.02f));
    }

    private int GetUpdatedHitBonusStrength()
    {
        return UpdateStat(Might, 0.5f, 0.02f) + UpdateStat(Precision, 0.25f, 0.02f);
    }
    private int GetUpdatedHitBonusPrecision()
    {
        return UpdateStat(Precision, 1f, 0.02f);
    }

    // public int GetCorrectHitBonus(bool mystical = false)
    // {
    //     return mystical ? HitBonusPrecision + (WeaponTypeEquipped == WeaponTypeEquippedMode.Magical ? HitBonusWeapon : 0)
    //         : WeaponTypeEquipped == WeaponTypeEquippedMode.Strength ? HitBonusWeapon + HitBonusStrength
    //         : HitBonusWeapon + HitBonusPrecision;
    // }
    public int GetCorrectHitBonus(bool mystical = false)
    {
        if (mystical)
        {
            if (WeaponTypeEquipped == WeaponTypeEquippedMode.Magical)
            {
                return Stats[StatMode.HitBonusPrecision] + HitBonusWeapon;
            }
            else
            {
                return Stats[StatMode.HitBonusPrecision];
            }
        }
        else if (WeaponTypeEquipped == WeaponTypeEquippedMode.Strength)
        {
            return Stats[StatMode.HitBonusStrength] + HitBonusWeapon;
        }
        else
        {
            return Stats[StatMode.HitBonusPrecision] + HitBonusWeapon;
        }
    }

    public int GetCorrectWeaponDamageBonus()
    {
        return WeaponTypeEquipped == WeaponTypeEquippedMode.Strength ? Stats[StatMode.PhysicalDamageStrength] + WeaponDamageBonus : Stats[StatMode.PhysicalDamagePrecision] + WeaponDamageBonus;
    }
    private int GetUpdatedReagents()
    {
        return UpdateStat(Intellect, 1f, 0.02f);
    }
    private int GetUpdatedFocusCharge()
    {
        return UpdateStat(Intellect, 2f, 0.015f);
    }
    private int GetUpdatedPersuasion()
    {
        return UpdateStat(Intellect, 1f, 0.05f) + UpdateStat(Charisma, 2f, 0.01f);
    }
    private int GetUpdatedPersuasionResist()
    {
        return UpdateStat(Intellect, 2f, 0.025f);
    }
    private int GetUpdatedActionPoints()
    {
        return UpdateStat(Speed, 1f, 0.03f);
    }
    private int GetUpdatedMoveSpeed()
    {
        return UpdateStat(Speed, 1f, 0.03f) * 100;
    }
    private void UpdateKnownSpells()
    {
        // this should be called whenever perks are updated
        KnownSpells.Clear();
        foreach (int i in _perks)
        {
            if (i >= 0 && i < 8)
            {
                if (!KnownSpells.Contains((SpellEffectManager.SpellMode)i))
                {
                    KnownSpells.Add((SpellEffectManager.SpellMode)i);
                }
            }
        }
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
    private List<int> _perks = new();
    public List<int> Perks
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
    } // { get; set; } // 0/1/2/3/4/5/6/7 can be known spells ?
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
    public int WeaponDamageBonus { get; set; } = 2; // Should be updated when changing weapon
    public List<Tuple<int, int>> WeaponDice { get; set; } = new() {new Tuple<int,int>(1,8), // should be updated when changing weapon
        new Tuple<int,int>(1,6)}; // e.g. 2d4 + 1d6 -> post-jam will need to change be explicit about damage types, and maybe introduce damage type resistances

    // // the below are intended to be modified by attributes and perks
    // public int HitBonusStrength { get; set; } = 0;
    // public int HitBonusPrecision { get; set; } = 0; // used for magic as well

    // // the below are intended to be modified by equipped weapons
    public int HitBonusWeapon { get; set; } = 0;

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
    // public int Health { get; set; } = 10; // e.g. this will be adjusted by endurance, vigour, etc.
    // public int Endurance { get; set; } = 10;
    // public int Reagents { get; set; } = 5;
    // public int FocusCharge { get; set; } = 10;
    ////


}
