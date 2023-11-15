using Godot;
using System;

public partial class Scales : RefCounted
{


    // - so you cast solar bolt -> +10% shamesh favour, so u get +precision and +intellect or something, but -10% inanna's favour -> -might and resilience
    // - and vice versa
    // - tracker throughout the battle sync'd to a scale animation (0.0 to 1.0 corresponding to all the way to the left, or right, with 0.5 being steaady)
    // - buffs become 20% if at 0 or 1 (and debuffs)

    // - at end of battle, 0 to 0.3 = offered shamash perks, 0.4-0.6 = offered either, 0.7 to 1.0 = offered ishtari perks
    // - at the end of the battle, collate the total balance, e.g. -.5 to .5, -ve - shamashi, +ve = ishtari - store this to use at the end of the game
    // - after the battle, also need to decide between sparing an enemy, killing them, or attempting to convince them to join you (based on persuasion vs persuasion resist)
    //     - if kill -> ishtar + and shamash -
    //     - if spare -> vice versa
    //     - if join, both - (floor of 0)
    //     - whoever +, will give u a bonus perk

    public enum FavourMode { Shamash, Ishtar, Balanced }

    private float _battleFavour = 0; // from -0.5 to 0.5 -> shamash to ishtar

    private float _overallFavour = 0;

    private float _increment = 0.05f;

    public void FavourShamash(int num = 1)
    {
        BestowFavour(false, num);
    }

    private void BestowFavour(bool ishtar, int num)
    {
        for (int i = 0; i < num; i++)
        {
            _battleFavour += _increment * (ishtar ? 1 : -1);
            _battleFavour = Math.Min(Math.Max(_battleFavour, -0.5f), 0.5f);
        }
    }

    public bool IsExtreme()
    {
        return Math.Abs(_battleFavour) == 0.5f;
    }

    public void FavourIshtar(int num = 1)
    {
        BestowFavour(true, num);
    }

    public void FavourNeutrality()
    {
        if (_battleFavour > 0)
        {
            FavourShamash();
        }
        else if (_battleFavour < 0)
        {
            FavourIshtar();
        }
    }

    public void CollateFavoursEndOfBattle()
    {
        _overallFavour += _battleFavour;
        _battleFavour = 0;
    }

    public FavourMode GetCurrentFavour()
    {
        return FavourCheck(_battleFavour);
    }

    private FavourMode FavourCheck(float whichFavour)
    {
        return whichFavour < -0.1f ? FavourMode.Shamash : whichFavour < 0.2 ? FavourMode.Balanced : FavourMode.Ishtar;
    }

    public FavourMode CheckOverallFavour()
    {
        return FavourCheck(_overallFavour);
    }

    public float GetScaleAnimationTime()
    {
        return _battleFavour + 0.5f;
    }

}