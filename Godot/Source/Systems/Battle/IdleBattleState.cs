using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class IdleBattleState : BattleState
{
    // private CharacterUnit _currentUnit;


    // the action that is deduced from information available
    private Battler.ActionMode _currentAction = Battler.ActionMode.Move;

    public IdleBattleState(Battler battler)
    {
        this.Battler = battler;

        InitCurrentTurn();
        Vector2 mousePos = Battler.GetGlobalMousePosition();
        SetContextualAction(mousePos);

        // Battler.SetDebugText("entering idle state");
        // _currentUnit = Battler.CharactersAwaitingTurn[0];

        //await input from player (or action decision from enemy)
        // if player - make it obvious whose turn it is and awaiting input. dynamically update UI elements such as pathing

        // if enemy, show ENEMY TURN or similar, show some enemy thinking animation, and make it obvious which enemy unit turn it is

        // once input (from either enemy or player) -> transition to processing state
    }

    private void InitCurrentTurn()
    {
        Battler.CharactersAwaitingTurn[0].CharacterStartBattleTurn();
        // Battler.CharactersAwaitingTurn[0].SetActionState(CharacterUnit.Battler.ActionMode.IdleBattle);
        // Battler.CharactersAwaitingTurn[0].Modulate = new Color(0,1,0);
    }

    private bool HumanTurn()
    {
        return Battler.CharactersAwaitingTurn[0].StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player;
    }

    private bool IsValidWorldMove(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        return IsValidGridMove(Battler.BattleGrid.WorldToGrid(startWorldPos), Battler.BattleGrid.WorldToGrid(endWorldPos));
    }

    private bool IsValidGridMove(Vector2 startGridPos, Vector2 endGridPos)
    {
        // GD.Print(GridMoveCost(startGridPos, endGridPos) == 0 ? "cant move to same square" : Battler.CharactersAwaitingTurn[0].CharacterData.ActionPoints + " available; cost is " + GridMoveCost(startGridPos, endGridPos));
        return GridMoveCost(startGridPos, endGridPos) > 0 &&
            GridMoveCost(startGridPos, endGridPos) <=
            Battler.CharactersAwaitingTurn[0].CharacterData.ActionPoints;
    }

    private int WorldMoveCost(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        return GridMoveCost(Battler.BattleGrid.WorldToGrid(startWorldPos), Battler.BattleGrid.WorldToGrid(endWorldPos));
        // Battler.CharactersAwaitingTurn.Remove(Battler.CharactersAwaitingTurn.ToList()[0]);
    }
    private int GridMoveCost(Vector2 startGridPos, Vector2 endGridPos)
    {
        // subtract starting hex....
        return Battler.BattleGrid.HexNavigation.CalculateGridPath(startGridPos, endGridPos).Length - 1;
        // Battler.CharactersAwaitingTurn.Remove(Battler.CharactersAwaitingTurn.ToList()[0]);
    }

    private int MeleeRangedCastCost()
    {
        return Battler.CharactersAwaitingTurn[0].CharacterData.MaxActionPoints / 2; //return (int)Math.Floor(((float)Battler.CharactersAwaitingTurn[0].CharacterData.MaxActionPoints) * 0.5f);
    }

    private bool CanAfford(int cost)
    {
        return Battler.CharactersAwaitingTurn[0].CharacterData.ActionPoints >= cost;
    }

    private bool IsNeighbour(Vector2 originPos, Vector2 targetPos)
    {

        Hexagon hex = Battler.BattleGrid.GetHexAtGridPosition(targetPos);
        List<Vector2> surroundingGridPositions = Battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);
        if (surroundingGridPositions.Contains(originPos))
        {
            return true;
        }
        return false;
    }

    private CharacterUnit CharacterAtGridPos(Vector2 gridPos)
    {
        foreach (CharacterUnit characterUnit in Battler.AllCharacters)
        {
            if (Battler.BattleGrid.WorldToGrid(characterUnit.GlobalPosition) == gridPos)
            {
                return characterUnit;
            }
        }
        return null;
    }

    public override void ProcessUpdate(double delta)
    {
        base.ProcessUpdate(delta);
        // do something else when human turn
        if (!HumanTurn())
        {
            Battler.CharactersAwaitingTurn[0].BattleSkipOrder(); // todo!
            Battler.SetState(Battler.BattleMode.Processing); // todo! AI TURN
            return;
        }

        // should only happen when grid changes
        // Battler.BattleGrid.UpdateNavigationAndDisplay();
        //
    }

    public override void InputUpdate(InputEvent ev)
    {
        base.InputUpdate(ev);
        ////
        // DEBUGGING //
        string text = "Round " + Battler.Round;
        text += "\nMouse pos: " + Battler.BattleGrid.WorldToGrid(Battler.GetGlobalMousePosition());
        text += "=> " + (IsValidWorldMove(Battler.CharactersAwaitingTurn[0].GlobalPosition, Battler.GetGlobalMousePosition())
            ? "valid move" : "invalid move");
        text += String.Format("\n[Turn of character unit at hex {0}",
            Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition));

        if (HumanTurn())
        {

            text += "\nHuman";
            // update display every frame
        }
        else
        {
            text += "\nAI";
            // show AI thinking animation
        }
        Battler.SetDebugText(text);
        ////
        ////
        if (!HumanTurn())
        {
            return;
        }
        if (ev.IsEcho())
        {
            return;
        }



        Vector2 mousePos = Battler.GetGlobalMousePosition();
        SetContextualAction(mousePos);

        // TODO -split this into some logical programming pattern like state pattern in a separate class

        // deduce actions AFTER left click
        if (ev is InputEventMouseButton btn)
        {
            if (btn.Pressed && btn.ButtonIndex == MouseButton.Left)
            {
                DoAction();
            }
            else if (btn.Pressed && btn.ButtonIndex == MouseButton.Right)
            {
                // contextual right click either done here, or more likely at the UI element level
            }
        }
    }

    private void DoAction()
    {
        Vector2 mousePos = Battler.GetGlobalMousePosition();
        Vector2 mouseGridPos = Battler.BattleGrid.WorldToGrid(mousePos);
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition);
        Vector2 closestPos = new();// surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
        CharacterUnit targetCharacter = CharacterAtGridPos(mouseGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];

        if (_currentAction == Battler.ActionMode.Move)
        {
            int moveCost = GridMoveCost(characterGridPos, mouseGridPos);
            List<Vector2> worldPath = Battler.BattleGrid.GridToWorldPath(
                Battler.BattleGrid.HexNavigation.CalculateGridPath(characterGridPos, mouseGridPos)
            );
            Battler.CharactersAwaitingTurn[0].BattleMoveOrder(moveCost, worldPath);

            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
                moveCost, controlledCharacter.CharacterData.ActionPoints), true);
        }
        else if (_currentAction == Battler.ActionMode.Melee)
        {
            if (!IsNeighbour(characterGridPos, mouseGridPos))
            {

                Hexagon hex = Battler.BattleGrid.GetHexAtGridPosition(mouseGridPos);
                List<Vector2> surroundingGridPositions = Battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);
                surroundingGridPositions.RemoveAll(pos => !IsValidGridMove(characterGridPos, pos));
                // surroundingGridPositions guaranteed to be non-zero because IsValidMelee checked prior to this method
                closestPos = surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
            }
            if (IsNeighbour(characterGridPos, mouseGridPos))
            {
                controlledCharacter.BattleMeleeOrder(targetCharacter);
            }
            else
            {
                // if not adjacent, then first move the character to the target, THEN melee the target
                int moveCost = GridMoveCost(characterGridPos, closestPos);
                List<Vector2> worldPath = Battler.BattleGrid.GridToWorldPath(
                    Battler.BattleGrid.HexNavigation.CalculateGridPath(characterGridPos, closestPos)
                );
                controlledCharacter.BattleMoveOrder(moveCost, worldPath, targetCharacter);
                Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
                    moveCost, controlledCharacter.CharacterData.ActionPoints), true);
            }
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} strikes {1} in melee combat.", controlledCharacter.CharacterData.Name,
                targetCharacter.CharacterData.Name), true);
        }
        else if (_currentAction == Battler.ActionMode.Shoot)
        {
            controlledCharacter.BattleShootOrder(targetCharacter);
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} shoots {1}.", controlledCharacter.CharacterData.Name,
                targetCharacter.CharacterData.Name), true);
        }
        else if (_currentAction == Battler.ActionMode.Cast)
        {
            controlledCharacter.BattleCastOrder(mouseGridPos, targetCharacter);
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} casts {1}.", controlledCharacter.CharacterData.Name,
                controlledCharacter.SelectedSpell.Name), true);
        }
        else if (_currentAction == Battler.ActionMode.Hint)
        {
            // do hint
            return;
        }
        else if (_currentAction == Battler.ActionMode.Invalid)
        {
            OnInvalidTarget();
            return;
        }
        else if (_currentAction == Battler.ActionMode.None)
        {
            return;
        }
        Battler.SetState(Battler.BattleMode.Processing);
    }

    private void OnInvalidTarget()
    {
        Battler.EmitSignal(Battler.SignalName.LogBattleText, "Invalid target", false);
        // GD.Print("invalid target - play BZZZT sound here");
    }

    private bool IsValidMelee(Vector2 mouseGridPos, Vector2 characterGridPos)
    {
        Vector2 closestPos = new();// surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
        CharacterUnit targetCharacter = CharacterAtGridPos(mouseGridPos);
        if (targetCharacter == null)
        {
            return false;
        }
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (Battler.PlayerSelectedAction == Battler.ActionMode.Melee || Battler.PlayerSelectedAction == Battler.ActionMode.Move)
        {
            bool validTarget = targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Neutral || targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Hostile;
            if (!IsNeighbour(characterGridPos, mouseGridPos))
            {

                Hexagon hex = Battler.BattleGrid.GetHexAtGridPosition(mouseGridPos);
                List<Vector2> surroundingGridPositions = Battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);
                surroundingGridPositions.RemoveAll(pos => !IsValidGridMove(characterGridPos, pos));
                if (surroundingGridPositions.Count == 0 || !validTarget)
                {
                    return false;
                }
                closestPos = surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
            }
            if (validTarget && CanAfford(GridMoveCost(characterGridPos, closestPos) + MeleeRangedCastCost()))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsValidRanged(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = CharacterAtGridPos(mouseGridPos);
        if (targetCharacter == null)
        {
            return false;
        }
        if (targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Neutral || targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Hostile)
        {
            if (CanAfford(MeleeRangedCastCost()))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsValidSpell(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = CharacterAtGridPos(mouseGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        bool validEnemySpellTarget = false;
        bool validAllySpellTarget = false;
        if (targetCharacter != null)
        {
            validEnemySpellTarget = controlledCharacter.SelectedSpell.Target == SpellEffectData.TargetMode.Enemy &&
                (targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Hostile || targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Neutral);
            validAllySpellTarget = controlledCharacter.SelectedSpell.Target == SpellEffectData.TargetMode.Ally && (targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Allied ||
                targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Neutral || targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player);
        }
        bool validGroundSpellTarget = controlledCharacter.SelectedSpell.Target == SpellEffectData.TargetMode.Ground &&
            IsMouseOverCharacterOrEmpty(mouseGridPos);

        if (validEnemySpellTarget || validAllySpellTarget || validGroundSpellTarget)
        {
            return true;
        }
        return false;
    }

    private bool IsMouseOverCharacterOrEmpty(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = CharacterAtGridPos(mouseGridPos);
        if (targetCharacter != null || !Battler.BattleGrid.GetHexAtGridPosition(mouseGridPos).Obstacle)
        {
            return true;
        }
        return false;
    }

    private bool IsMouseOverAlly(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = CharacterAtGridPos(mouseGridPos);
        if (targetCharacter == null)
        {
            return false;
        }
        if (targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Allied || targetCharacter.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
        {
            return true;
        }
        return false;
    }

    private bool IsOverUI(Vector2 mousePos)
    {
        return Battler.UIBounds.HasPoint(mousePos);
    }

    private void SetContextualAction(Vector2 mousePos)
    {
        Vector2 mouseGridPos = Battler.BattleGrid.WorldToGrid(mousePos);
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition);
        CharacterUnit targetCharacter = CharacterAtGridPos(mouseGridPos);

        _currentAction = Battler.ActionMode.Invalid;
        // CAST
        if (IsValidSpell(mouseGridPos) && Battler.PlayerSelectedAction == Battler.ActionMode.Cast)
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Spell);
            _currentAction = Battler.ActionMode.Cast;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, String.Format("Cast {0}", Battler.CharactersAwaitingTurn[0].SelectedSpell.Name), false);
        }
        // RANGED
        else if (IsValidRanged(mouseGridPos) && Battler.PlayerSelectedAction == Battler.ActionMode.Shoot)
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Ranged);
            _currentAction = Battler.ActionMode.Shoot;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, String.Format("Shoot {0}", targetCharacter.CharacterData.Name), false);
        }
        // MELEE
        else if (IsValidMelee(mouseGridPos, characterGridPos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Melee);
            _currentAction = Battler.ActionMode.Melee;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, String.Format("Strike {0}", targetCharacter.CharacterData.Name), false);
        }
        // MOVE
        else if (IsValidGridMove(characterGridPos, mouseGridPos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Move);
            _currentAction = Battler.ActionMode.Move;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "Move to location", false);
        }
        // HINT
        else if (IsMouseOverAlly(mouseGridPos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Hint);
            _currentAction = Battler.ActionMode.Hint;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "Click for more information", false);
        }
        else if (IsOverUI(mousePos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Select);
            _currentAction = Battler.ActionMode.None;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "", false);
        }
        else // outside the grid bounds
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Invalid);
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "", false);
        }


    }

    public override void OnActionBtnPressed(Battler.ActionMode action)
    {
        Battler.PlayerSelectedAction = action;
    }
}
