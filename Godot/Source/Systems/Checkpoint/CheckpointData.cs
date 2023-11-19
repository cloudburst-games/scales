using System.Collections.Generic;

public class CheckpointData : IJSONSaveable
{
    public List<CharacterCheckpointData> PlayerCharacters { get; set; }
    public int CurrentLevel { get; set; }
    public int Difficulty { get; set; }
    public float Favour { get; set; }
}

public class CharacterCheckpointData : IJSONSaveable
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string PatronGod { get; set; }
    public string BodyPath { get; set; }
    public string PortraitPath { get; set; }
    public string CharacterBtnNormalPath { get; set; }
    public string CharacterBtnPressedPath { get; set; }
    public int _meleeWeaponEquipped { get; set; }
    public int _rangedWeaponEquipped { get; set; } // oops
    public List<int> Perks { get; set; }
    public int Level { get; set; }
    public int Might { get; set; }
    public int Resilience { get; set; }
    public int Precision { get; set; }
    public int Speed { get; set; }
    public int Intellect { get; set; }
    public int Charisma { get; set; }
    public int Luck { get; set; }
    public List<string> AudioWalkPath { get; set; }
    public List<string> AudioHurtPath { get; set; }
    public List<string> AudioDiePath { get; set; }
    public List<string> AudioMeleePath { get; set; }
    public Dictionary<int, List<List<string>>> Barks { get; set; }
}