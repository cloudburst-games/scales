// ?? NEEDED. Input seems to be handled by Selection and Battler state pattern scripts
using Godot;
using System;

public partial class CharacterUnitControlState : RefCounted
{
    public CharacterUnit CharacterUnit {get; set;}

    public virtual void Update(double delta)
    {
        
    }
}
