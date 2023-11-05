// DyingCharacterUnitActionState. TODO. Consider making part of the Battle action states, as reasonably
// would expect to transition from HitBattle or similar to this.
using Godot;
using System;

public partial class DyingCharacterUnitActionState : CharacterUnitActionState
{

    public DyingCharacterUnitActionState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
    }
}
