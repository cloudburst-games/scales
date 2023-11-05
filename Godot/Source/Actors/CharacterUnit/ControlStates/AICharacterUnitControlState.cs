// ?? NEEDED. Input seems to be handled by Selection and Battler state pattern scripts
using Godot;
using System;

public partial class AICharacterUnitControlState : CharacterUnitControlState
{
    public AICharacterUnitControlState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
    }
}
