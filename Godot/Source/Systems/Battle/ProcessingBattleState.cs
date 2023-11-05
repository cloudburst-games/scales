using Godot;
using System;

public class ProcessingBattleState : BattleState
{
    public ProcessingBattleState(Battler battler)
    {
        this.Battler = battler;

        Battler.CursorControl.SetCursor(CursorControl.CursorMode.Wait);
    }

    public override void ProcessUpdate(double delta)
    {
        base.ProcessUpdate(delta);
        Battler.SetDebugText("processing...");
        // Continue processing until a character is idle
        foreach (CharacterUnit cUnit in Battler.AllCharacters)
        {
            if (cUnit.GetActionState() == CharacterUnit.ActionMode.IdleBattle)
            {
                Battler.SetState(Battler.BattleMode.Idle);
            }
        }
    }
}
