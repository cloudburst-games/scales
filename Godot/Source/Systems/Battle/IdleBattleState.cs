// For AI, need a separate AIIdleBattleState, and consider also funnelling player INPUT logic into PlayerIdleBattleState (put these into control)

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class IdleBattleState : BattleState
{

    // action deduced from 

    public IdleBattleState(Battler battler)
    {
        this.Battler = battler;

        InitCurrentTurn();
        Vector2 mousePos = Battler.GetGlobalMousePosition();
        SetContextualAction(mousePos);

    }

    private void InitCurrentTurn()
    {
        Battler.CharactersAwaitingTurn[0].CharacterStartBattleTurn();

        Battler.SetGridUserHexes(GetValidMoveHexes(), GetValidHalfMoveHexes(), Battler.CurrentDisplayMode);
        if (HumanTurn())
        {
            Battler.SetOutlineColours();
        }
        SetSpriteOutlines(Battler.GetGlobalMousePosition());

        Battler.EmitSignal(Battler.SignalName.TurnStarted, Battler.CharactersAwaitingTurn[0].CharacterData.KnownSpells);

        if (Battler.CharactersAwaitingTurn[0].CharacterData.Berserk)
        {
            GD.Print("TODO - berserk effect");
            //
            // do berserk AI effect here -> 1. get the nearest target. 2. if the target is within melee range, attack. 3. otherwise, shoot if possible. 4. otherwise, move towards it.
        }
    }

    public override void OnMovedButStillHaveAP()
    {
        Battler.SetGridUserHexes(GetValidMoveHexes(), GetValidHalfMoveHexes(), Battler.CurrentDisplayMode);
    }

    private bool HumanTurn()
    {
        return Battler.CharactersAwaitingTurn[0].StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player;
    }

    private bool IsValidWorldMove(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        return IsValidGridMove(Battler.BattleGrid.WorldToGrid(startWorldPos), Battler.BattleGrid.WorldToGrid(endWorldPos));
    }

    private List<Vector2> GetValidMoveHexes()
    {
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition);

        List<Vector2> result = new();
        foreach (KeyValuePair<Vector2, Hexagon> kv in Battler.BattleGrid.Cells)
        {
            if (kv.Value.Obstacle)
            {
                continue;
            }
            if (IsValidGridMove(characterGridPos, kv.Key))
            {
                result.Add(kv.Key);
            }
        }
        return result;
    }
    private List<Vector2> GetValidHalfMoveHexes()
    {
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition);

        List<Vector2> result = new();
        foreach (KeyValuePair<Vector2, Hexagon> kv in Battler.BattleGrid.Cells)
        {
            if (kv.Value.Obstacle)
            {
                continue;
            }
            if (GridMoveCost(characterGridPos, kv.Key) > 0 && GridMoveCost(characterGridPos, kv.Key) <=
                Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] - MeleeRangedCastCost())
            {
                result.Add(kv.Key);
            }
        }
        return result;
    }
    private bool IsValidGridMove(Vector2 startGridPos, Vector2 endGridPos)
    {
        // GD.Print(GridMoveCost(startGridPos, endGridPos) == 0 ? "cant move to same square" : Battler.CharactersAwaitingTurn[0].CharacterData.ActionPoints + " available; cost is " + GridMoveCost(startGridPos, endGridPos));
        return GridMoveCost(startGridPos, endGridPos) > 0 &&
            GridMoveCost(startGridPos, endGridPos) <=
            Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints];
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
        return Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.MaxActionPoints] / 2; //return (int)Math.Floor(((float)Battler.CharactersAwaitingTurn[0].CharacterData.MaxActionPoints) * 0.5f);
    }

    private bool CanAfford(int cost)
    {
        return Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] >= cost;
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


    public override void ProcessUpdate(double delta)
    {
        base.ProcessUpdate(delta);
        // do something else when human turn
        // if (!HumanTurn())
        // {
        //     Battler.CharactersAwaitingTurn[0].BattleSkipOrder(); // todo!
        //     Battler.SetState(Battler.BattleMode.Processing); // todo! AI TURN
        //     return;
        // }

        // should only happen when grid changes
        // Battler.BattleGrid.UpdateNavigationAndDisplay();
        //
    }

    public override void InputUpdate(InputEvent ev)
    {
        base.InputUpdate(ev);
        // if (ev is InputEventMouseButton btnr)
        // {
        //     if (btnr.Pressed && btnr.ButtonIndex == MouseButton.Right)
        //     {
        //         Battler.CharactersAwaitingTurn[0].BattleSkipOrder(); // todo!
        //         // contextual right click either done here, or more likely at the UI element level
        //     }
        // }
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
        Vector2 centredWorldPos = Battler.BattleGrid.GridToWorld(mouseGridPos);
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition);
        Vector2 closestPos = new();// surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];

        if (Battler.CurrentAction == Battler.ActionMode.Move)
        {
            int moveCost = GridMoveCost(characterGridPos, mouseGridPos);
            List<Vector2> worldPath = Battler.BattleGrid.GridToWorldPath(
                Battler.BattleGrid.HexNavigation.CalculateGridPath(characterGridPos, mouseGridPos)
            );
            Battler.CharactersAwaitingTurn[0].BattleMoveOrder(moveCost, worldPath);

            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
                moveCost, controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]), true);
        }
        else if (Battler.CurrentAction == Battler.ActionMode.Melee)
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
                    moveCost, controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]), true);
            }
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} strikes {1} in melee combat.", controlledCharacter.CharacterData.Name,
                targetCharacter.CharacterData.Name), true);
        }
        else if (Battler.CurrentAction == Battler.ActionMode.Shoot)
        {
            SpellEffectManager.Spell spell = Battler.AllSpells[SpellEffectManager.SpellMode.Arrow];
            spell.Origin = controlledCharacter.GlobalPosition;
            spell.Destination = targetCharacter.GlobalPosition;
            spell.OriginCharacter = controlledCharacter;
            spell.TargetCharacter = targetCharacter;
            spell.AssociatedEffects[0].DamageDice = controlledCharacter.CharacterData.WeaponDice;
            controlledCharacter.BattleShootOrder(spell);
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} shoots {1}.", controlledCharacter.CharacterData.Name,
                targetCharacter.CharacterData.Name), true);
        }
        else if (Battler.CurrentAction == Battler.ActionMode.Cast)
        {
            // NEEd to reset everything or it bugs out in future castings. next time use 
            SpellEffectManager.Spell spell = Battler.AllSpells[Battler.CharactersAwaitingTurn[0].UISelectedSpell];
            spell.Origin = controlledCharacter.GlobalPosition;
            spell.Destination = centredWorldPos;
            spell.OriginCharacter = controlledCharacter;
            spell.TargetCharacter = targetCharacter;
            spell.HexDistance = Battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, centredWorldPos);
            spell.Outcome = null;
            spell.AreaAffectedCharacters = new();
            controlledCharacter.BattleCastOrder(spell);
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} casts {1}.", controlledCharacter.CharacterData.Name,
                spell.Name), true);
        }
        else if (Battler.CurrentAction == Battler.ActionMode.Hint)
        {
            // do hint
            return;
        }
        else if (Battler.CurrentAction == Battler.ActionMode.Invalid)
        {
            OnInvalidTarget();
            return;
        }
        else if (Battler.CurrentAction == Battler.ActionMode.None)
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
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        if (targetCharacter == null)
        {
            return false;
        }
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (Battler.PlayerSelectedAction == Battler.ActionMode.Melee || Battler.PlayerSelectedAction == Battler.ActionMode.Move)
        {
            bool validTarget = controlledCharacter.ValidEnemyTargets.Contains(targetCharacter.StatusToPlayer);
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

    private bool IsValidRanged(Vector2 mouseGridPos, int range)
    {
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (targetCharacter == null)
        {
            return false;
        }
        if (controlledCharacter.ValidEnemyTargets.Contains(targetCharacter.StatusToPlayer))
        {
            if (CanAfford(MeleeRangedCastCost()) && Battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, targetCharacter.GlobalPosition) <= range)
            {

                return true;
            }
        }
        return false;
    }

    // private SpellEffectManager.Spell _currentSelectedSpell = new() // set this to whatever is being selected in spellbook (starts off as null and goes to spellbook to pick if clicked)
    // {
    //     Target = SpellEffectManager.Spell.TargetMode.Enemy,
    //     Name = "Solar Flare",
    // };

    private bool IsValidSpell(Vector2 mouseGridPos, SpellEffectManager.SpellMode spell)
    {
        if (spell == SpellEffectManager.SpellMode.None)
        {
            return false;
        }
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        SpellEffectManager.Spell currentSelectedSpell = Battler.AllSpells[spell];
        bool validEnemySpellTarget = false;
        bool validAllySpellTarget = false;
        if (targetCharacter != null)
        {
            validEnemySpellTarget = currentSelectedSpell.Target == SpellEffectManager.Spell.TargetMode.Enemy &&
                controlledCharacter.ValidEnemyTargets.Contains(targetCharacter.StatusToPlayer);
            validAllySpellTarget = currentSelectedSpell.Target == SpellEffectManager.Spell.TargetMode.Ally &&
            controlledCharacter.ValidAllyTargets.Contains(targetCharacter.StatusToPlayer);
        }
        bool validGroundSpellTarget = currentSelectedSpell.Target == SpellEffectManager.Spell.TargetMode.Ground &&
            IsMouseOverCharacterOrEmpty(mouseGridPos);
        Vector2 destination = Battler.GetGlobalMousePosition();
        // GD.Print(currentSelectedSpell.Target);
        if (validEnemySpellTarget || validAllySpellTarget || validGroundSpellTarget)
        {
            if (Battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, destination) <= Battler.AllSpells[spell].Range)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsMouseOverCharacterOrEmpty(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        if (targetCharacter != null || !Battler.BattleGrid.GetHexAtGridPosition(mouseGridPos).Obstacle)
        {
            return true;
        }
        return false;
    }

    // Player only
    private bool IsMouseOverAlly(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
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

    private void SetSpriteOutlines(Vector2 mousePos)
    {
        Vector2 mouseGridPos = Battler.BattleGrid.WorldToGrid(mousePos);
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        foreach (CharacterUnit characterUnit in GetAliveUnits())
        {
            if (characterUnit != Battler.CharactersAwaitingTurn[0] && characterUnit != targetCharacter)
            {
                characterUnit.ShowSpriteOutline(false);
            }
        }
        if (targetCharacter != null)
        {
            if (targetCharacter != Battler.CharactersAwaitingTurn[0])
            {
                targetCharacter.ShowSpriteOutline(true);
            }
        }
    }

    private void SetContextualAction(Vector2 mousePos)
    {
        Vector2 mouseGridPos = Battler.BattleGrid.WorldToGrid(mousePos);
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition);
        SetSpriteOutlines(Battler.GetGlobalMousePosition());


        Battler.CurrentAction = Battler.ActionMode.Invalid;
        // CAST
        if (IsValidSpell(mouseGridPos, Battler.CharactersAwaitingTurn[0].UISelectedSpell) && Battler.PlayerSelectedAction == Battler.ActionMode.Cast)
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Spell);
            Battler.CurrentAction = Battler.ActionMode.Cast;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, String.Format("Cast {0}", Battler.AllSpells[Battler.CharactersAwaitingTurn[0].UISelectedSpell].Name), false);
        }
        // RANGED
        else if (IsValidRanged(mouseGridPos, Battler.AllSpells[SpellEffectManager.SpellMode.Arrow].Range) && Battler.PlayerSelectedAction == Battler.ActionMode.Shoot)
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Ranged);
            Battler.CurrentAction = Battler.ActionMode.Shoot;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, String.Format("Shoot {0}", targetCharacter.CharacterData.Name), false);
        }
        // MELEE
        else if (IsValidMelee(mouseGridPos, characterGridPos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Melee);
            Battler.CurrentAction = Battler.ActionMode.Melee;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, String.Format("Strike {0}", targetCharacter.CharacterData.Name), false);
        }
        // MOVE
        else if (IsValidGridMove(characterGridPos, mouseGridPos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Move);
            Battler.CurrentAction = Battler.ActionMode.Move;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "Move to location", false);
        }
        // HINT
        else if (IsMouseOverAlly(mouseGridPos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Hint);
            Battler.CurrentAction = Battler.ActionMode.Hint;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "Click for more information", false);
        }
        else if (IsOverUI(mousePos))
        {
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Select);
            Battler.CurrentAction = Battler.ActionMode.None;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "", false);
        }
        else // outside the grid bounds
        {
            // Set valid error message to log TODODODO!!!!
            Battler.CursorControl.SetCursor(CursorControl.CursorMode.Invalid);
            Battler.EmitSignal(Battler.SignalName.LogBattleText, "", false);
        }


    }

    public override void OnActionBtnPressed(Battler.ActionMode action)
    {
        Battler.PlayerSelectedAction = action;
        Battler.CharactersAwaitingTurn[0].UISelectedAction = action;
    }

    public override void OnBtnChooseSpellPressed()
    {
        base.OnBtnChooseSpellPressed();
        Battler.EmitSignal(Battler.SignalName.HUDActionRequested, (int)BattleHUD.StateMode.SpellBookOpened);
    }

    public override void OnBtnEndTurnPressed()
    {
        if (HumanTurn())
        {
            Battler.CharactersAwaitingTurn[0].BattleSkipOrder();
        }
        // TEMPORARY UNTIL AI SORTED:
        else
        {

            Battler.CharactersAwaitingTurn[0].BattleSkipOrder();
        }
    }
}
