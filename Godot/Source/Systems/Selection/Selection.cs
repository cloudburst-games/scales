using Godot;
using System;
using System.Collections.Generic;

public partial class Selection : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

    public enum SelectionMode { Adventure, Battle }

    private SelectionState _selectionState;

    [Signal]
    public delegate void MoveTargetSelectedEventHandler(CharacterUnit characterUnit, Vector2 targetPos);

    // public void InsertPlayerCharacter(CharacterUnit playerCharacter)
    // {
    //     if (!_selectionState.PlayerCharacters.Contains(playerCharacter))
    //     {
    //         _selectionState.PlayerCharacters.Add(playerCharacter);
    //     }
    // }

    public void SetPlayerCharacters(List<CharacterUnit> playerCharacters)
    {
        _selectionState.PlayerCharacters = playerCharacters;
    }

    public void OnPlayerCharacterClicked(CharacterUnit playerCharacter, bool shift)
    {
        if (_selectionState == null)
        {
            return;
        }
        _selectionState.OnPlayerCharacterClicked(playerCharacter, shift);
    }

    public void SetState(SelectionMode selectionMode)
    {
        switch (selectionMode)
        {
            case SelectionMode.Adventure:
                _selectionState = new AdventureSelectionState(this);
                break;
            case SelectionMode.Battle:
                _selectionState = new BattleSelectionState(this);
                break;
        }
    }

    public SelectionMode GetState()
    {
        return _selectionState is AdventureSelectionState ? SelectionMode.Adventure : SelectionMode.Battle;
    }

    public override void _Draw()
    {
        base._Draw();
        if (_selectionState == null)
        {
            return;
        }
        _selectionState.DrawUpdateOnce();
    }

    public override void _Process(double delta)
    {
        if (_selectionState == null)
        {
            return;
        }
        _selectionState.ProcessUpdate(delta);
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        base._UnhandledInput(ev);
        if (_selectionState == null)
        {
            return;
        }
        _selectionState.UnhandledInputUpdate(ev);
    }

    public override void _Input(InputEvent ev)
    {
        base._Input(ev);
        if (_selectionState == null)
        {
            return;
        }
        _selectionState.InputUpdate(ev);
    }
}
