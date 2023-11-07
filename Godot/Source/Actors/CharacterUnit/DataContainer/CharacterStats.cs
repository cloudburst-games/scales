// DEPRECATED - DO NOT USE. i amnot risking deleting this yet

// CharacterStats. Ideally anything changeable for each character should be in here, to allow for easy prototyping,
// saving, and loading. The contents will be saved in JSON when player saves.
using Godot;
using System;

public partial class CharacterStats : Resource, IJSONSaveable
{
    // DERIVED STATS: // Derived stats: ingame, these will be adjusted by attributes and other variables.
    [Export]
    public int CurrentHealth { get; set; } = 10; // e.g. this will be adjusted by endurance, vigour, etc.
    [Export]
    public int MaxHealth { get; set; } = 10;
    [Export]
    public int Initiative { get; set; } = 10;
    [Export]
    public int MaxActionPoints { get; set; } = 5;
    ////

    // if altered, then load stats from save file
    public bool Altered { get; set; } = false;
    public bool Alive { get; set; } = true;
    // Adventure map speed in real-time. This should be calculated / modified by other attributes
    public float MoveSpeed { get; set; } = 200f;
    public int ActionPoints { get; set; }

}
