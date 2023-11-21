using System;
using Godot;

public partial class ControlIdleBattleState : RefCounted
{
    public IdleBattleState IdleBattleState { get; set; }

    public virtual void InputUpdate(InputEvent ev)
    {

    }

    public virtual void RedisplayHexGrid() { }

    public virtual void OnInitCurrentTurn()
    {

    }

    public virtual void ProcessUpdate(double delta)
    {
    }

    public virtual void SetPlayerAction(Battler.ActionMode action)
    {
    }
}