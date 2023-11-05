using System.Collections.Generic;

public class StoryCharacterData : IJSONSaveable
{
    public string Name;
    public string Description;
    public string PatronGod;
    public int Might;
    public float PhysicalDamage;
    public List<int> Perks;
}
