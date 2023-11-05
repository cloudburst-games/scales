using Godot;
using System.Collections.Generic;
using System.Linq;

public class BattleState
{
    public Battler Battler { get; set; }

    public virtual void ComputeTurnOrder()
    {
        Battler.CharactersAwaitingTurn = GetAliveUnits().OrderByDescending(x => x.CharacterStats.Initiative).ToList();

        // make a list of all characters by turn order that will be accessible in other states (so in Battler)
    }

    public List<CharacterUnit> GetAliveUnits()
    {
        List<CharacterUnit> result = new();
        foreach (CharacterUnit cUnit in Battler.AllCharacters)
        {
            // skip the ones that die (remember we free all battleunits at conclusion anyway)
            if (cUnit.CharacterStats.Alive)
            {
                result.Add(cUnit);
                continue;
            }
        }
        return result;
    }

    public virtual void ProcessUpdate(double delta)
    {

    }

    public virtual void InputUpdate(InputEvent ev)
    {

    }

    public virtual void OnActionBtnPressed(Battler.ActionMode btn)
    {

    }
}
