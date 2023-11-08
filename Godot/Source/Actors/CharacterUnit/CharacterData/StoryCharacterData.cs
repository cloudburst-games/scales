using System;
using System.Collections.Generic;
using System.ComponentModel;

public class StoryCharacterData : IJSONSaveable
{

    // public enum AttributeMode { Might, Resilience, Precision, Speed, Intellect, Charisma, Luck }
    // public Dictionary<AttributeMode, int> Attributes { get; set; }

    // public StoryCharacterData()
    // {
    // Initialise();
    // UpdateAllStats();
    // }

    public void Initialise()
    {
        //     Attributes = new() { { AttributeMode.Might, Might },
        //         { AttributeMode.Resilience, Resilience },
        //         { AttributeMode.Precision, Precision },
        //         { AttributeMode.Speed, Speed },
        //         { AttributeMode.Intellect, Intellect },
        //         { AttributeMode.Charisma, Charisma },
        //         { AttributeMode.Luck, Luck }, };
        UpdateAllStats();
    }

    public void UpdateAllStats()
    {
        Health = MaxHealth = GetUpdatedHealth();
        Endurance = MaxEndurance = GetUpdatedEndurance();
        HealthRegen = GetUpdatedHealthRegen();
        EnduranceRegen = GetUpdatedEnduranceRegen();
        MysticResist = GetUpdatedMysticResist();
        PhysicalResist = GetUpdatedPhysicalResist();
        Dodge = GetUpdatedDodge();
        PhysicalDamageStrength = GetUpdatedPhysicalDamageStrength();
        PhysicalDamagePrecision = GetUpdatedPhysicalDamagePrecision();
        Mysticism = GetUpdatedMysticism();
        Initiative = GetUpdatedInitiative();
        Leadership = GetUpdatedLeadership();
        CriticalThreshold = GetUpdatedCriticalThreshold();
        HitBonusStrength = GetUpdatedHitBonusStrength();
        HitBonusPrecision = GetUpdatedHitBonusPrecision();
        Reagents = MaxReagents = GetUpdatedReagents();
        FocusCharge = MaxFocusCharge = GetUpdatedFocusCharge();
        Persuasion = GetUpdatedPersuasion();
        PersuasionResist = GetUpdatedPersuasionResist();
        ActionPoints = MaxActionPoints = GetUpdatedActionPoints();
        MoveSpeed = GetUpdatedMoveSpeed();
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
        return UpdateStat(Endurance, 0.4f, 0.025f);
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
        return UpdateStat(Intellect, 2f, 0.025f);
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
        return UpdateStat(Might, 0.75f, 0.02f);
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
                return HitBonusPrecision + HitBonusWeapon;
            }
            else
            {
                return HitBonusPrecision;
            }
        }
        else if (WeaponTypeEquipped == WeaponTypeEquippedMode.Strength)
        {
            return HitBonusWeapon + HitBonusStrength;
        }
        else
        {
            return HitBonusWeapon + HitBonusPrecision;
        }
    }

    public int GetCorrectWeaponDamageBonus()
    {
        return WeaponTypeEquipped == WeaponTypeEquippedMode.Strength ? PhysicalDamageStrength + WeaponDamageBonus : PhysicalDamagePrecision + WeaponDamageBonus;
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

    // Higher 'multiplier' results in a higher output value.
    // Higher 'constant' increases the effect of diminishing returns (i.e. smaller result)
    private int UpdateStat(int att, float multiplier, float constant)
    {
        return (int)(att * multiplier / (1 + (att * constant)));
    }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PatronGod { get; set; }
    public List<int> Perks { get; set; }
    public int Level { get; set; }

    // Attributes
    public int Might { get; set; }
    public int Resilience { get; set; }
    public int Precision { get; set; }
    public int Speed { get; set; }
    public int Intellect { get; set; }
    public int Charisma { get; set; }
    public int Luck { get; set; }

    // DERIVED STATS: // Derived stats: ingame, these will be adjusted by attributes and other variables.   
    public int MaxHealth { get; set; } = 10;
    public int MaxEndurance { get; set; } = 10; // every action costs endurance (endurance regen will ensure can always do something)
    public int HealthRegen { get; set; } = 2;
    public int EnduranceRegen { get; set; } = 3;
    public int MysticResist { get; set; } = 5;
    public int PhysicalResist { get; set; } = 4;
    public int Dodge { get; set; } = 3;
    public int PhysicalDamageStrength { get; set; } = 3;
    public int PhysicalDamagePrecision { get; set; } = 4;
    public int Mysticism { get; set; } = 4; // increases power of magical effects
    public int Initiative { get; set; } = 10;
    public int Leadership { get; set; } = 10;
    public int CriticalThreshold { get; set; } = 20; // This can reduce depending your luck to 19, 18, etc. to a minimum of x (11?)
    public int ArmourClass { get; set; } = 0; // Should be updated when changing armour
    public int WeaponDamageBonus { get; set; } = 2; // Should be updated when changing weapon
    public List<Tuple<int, int>> WeaponDice { get; set; } = new() {new Tuple<int,int>(1,8), // should be updated when changing weapon
        new Tuple<int,int>(1,6)}; // e.g. 2d4 + 1d6 -> post-jam will need to change be explicit about damage types, and maybe introduce damage type resistances

    // the below are intended to be modified by attributes and perks
    public int HitBonusStrength { get; set; } = 0;
    public int HitBonusPrecision { get; set; } = 0; // used for magic as well

    // the below are intended to be modified by equipped weapons
    public int HitBonusWeapon { get; set; } = 0;

    public enum WeaponTypeEquippedMode { Strength, Precision, Magical }
    // This should be modified by the weapon equipped, e.g. strength or precision (for most precision weapons or magical weapons)
    public WeaponTypeEquippedMode WeaponTypeEquipped = WeaponTypeEquippedMode.Strength;



    // modified by the selected action, 
    // e.g. if attacking with a weapon, associated weapon perks will increase this hit bonus. if casting, associated magical perks will increase this hit bonus.
    // with strength based weapons, should add StrengthHit, same with dex based weapons and precision, and magic based and mysticism
    // public int HitBonus { get; set; } = 3;
    public int MaxReagents { get; set; } = 5; // different spells cost different amount, some cost zero, maybe increased by intellect? + perks like forager. Increases by 10% per round.
    public int MaxFocusCharge { get; set; } = 10; // increases by 20% per round
    public int Persuasion { get; set; } = 3; // at 10% health, enemies may surrender at low persuasion: roll a d20 against their persuasion defence
    public int PersuasionResist { get; set; } = 10;
    public int MaxActionPoints { get; set; } = 5;
    public int Experience { get; set; } = 0;
    public int MoveSpeed { get; set; } = 300; // out of combat move speed, analagous to action points in combat

    // derived from derived stats, and change during e.g. during battle
    public int ActionPoints { get; set; } = 5;
    public bool Alive { get; set; } = true;
    public int Health { get; set; } = 10; // e.g. this will be adjusted by endurance, vigour, etc.
    public int Endurance { get; set; } = 10;
    public int Reagents { get; set; } = 5;
    public int FocusCharge { get; set; } = 10;
    ////


}
