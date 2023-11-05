using Godot;
using System;

public class EndingBattleState : BattleState
{
    public EndingBattleState(Battler battler)
    {
        this.Battler = battler;

        // TODO
        // play ending animation
        // show victory screen or defeat screen
        // when this is closed, animate back to adventure UI        
        // tell characters that battle is ending
        Battler.SetDebugText("");

        if (!Battler.AreAnyAlive(CharacterUnit.StatusToPlayerMode.Player))
        {
            Battler.EmitSignal(Battler.SignalName.BattleEnded, false); // player lost
        }
        else
        {
            Battler.EmitSignal(Battler.SignalName.BattleEnded, true); // player won
        }
        // make characters idle last, so that they can be responsive when appropriate to player input
        foreach (CharacterUnit cUnit in Battler.AllCharacters)
        {
            cUnit.OnBattleEnd();
        }
    }
}
