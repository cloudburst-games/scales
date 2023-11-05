using Godot;
using System;
using System.Collections.Generic;

public partial class SelectionState : RefCounted
{
    public Selection Selection {get; set;}

    public List<CharacterUnit> PlayerCharacters {get; set;} = new();


    public virtual void ProcessUpdate(double delta)
    {

    }

    public virtual void UnhandledInputUpdate(InputEvent ev)
    {

    }

    public virtual void InputUpdate(InputEvent ev)
    {

    }

    public virtual void DrawUpdateOnce()
    {

    }
    
    public virtual void OnPlayerCharacterClicked(CharacterUnit playerCharacter, bool shift)
    {
        
    }
}
