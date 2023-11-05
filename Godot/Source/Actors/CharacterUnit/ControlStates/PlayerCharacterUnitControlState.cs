// ?? NEEDED. Input seems to be handled by Selection and Battler state pattern scripts
using Godot;
using System;

public partial class PlayerCharacterUnitControlState : CharacterUnitControlState
{
    public PlayerCharacterUnitControlState(CharacterUnit characterUnit)
    {
        this.CharacterUnit = characterUnit;
    }

    public override void Update(double delta)
    {
        // if (this.CharacterUnit.Selected && Input.IsMouseButtonPressed(MouseButton.Right))
        // {
        //     CharacterUnit.NavAgent.TargetPosition = CharacterUnit.GetGlobalMousePosition();
        // }
    }
}
