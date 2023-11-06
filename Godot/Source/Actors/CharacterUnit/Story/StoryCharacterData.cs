using System.Collections.Generic;

public class StoryCharacterData : IJSONSaveable
{
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
    public int PhysicalDamage { get; set; } = 3;
    public int Mysticism { get; set; } = 4; // increases power of magical effects
    public int Initiative { get; set; } = 10;
    public int Leadership { get; set; } = 10;
    public int CriticalThreshold { get; set; } = 20; // This can reduce depending your luck to 19, 18, etc. to a minimum of x (11?)

    // modified by the selected action, 
    // e.g. if attacking with a weapon, associated weapon perks will increase this hit bonus. if casting, associated magical perks will increase this hit bonus.
    public int HitBonus { get; set; } = 3;
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
    public int CurrentHealth { get; set; } = 10; // e.g. this will be adjusted by endurance, vigour, etc.
    public int Endurance { get; set; } = 10;
    public int Reagents { get; set; } = 5;
    public int FocusCharge { get; set; } = 10;
    ////


}
