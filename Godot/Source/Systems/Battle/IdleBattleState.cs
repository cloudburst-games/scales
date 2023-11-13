// For AI, need a separate AIIdleBattleState, and consider also funnelling player INPUT logic into PlayerIdleBattleState (put these into control)

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class IdleBattleState : BattleState
{

    // action deduced from 

    public IdleBattleState(Battler Battler)
    {
        this.Battler = Battler;


        Battler.CursorControl.SetCursor(CursorControl.CursorMode.Wait);
        InitCurrentTurn();

    }

    public enum IdleBattleControlMode { Player, AI }

    private ControlIdleBattleState _controlIdleBattleState;

    public void SetControlState(IdleBattleControlMode state)
    {
        switch (state)
        {
            case IdleBattleControlMode.Player:
                _controlIdleBattleState = new PlayerIdleBattleState(this);
                break;
            case IdleBattleControlMode.AI:
                _controlIdleBattleState = new AIIdleBattleState(this);
                break;
        }
    }

    private void InitCurrentTurn()
    {
        SetControlState(HumanTurn() ? IdleBattleControlMode.Player : IdleBattleControlMode.AI);
        Battler.CharactersAwaitingTurn[0].CharacterStartBattleTurn();

        Battler.SetGridUserHexes(GetValidMoveHexes(), GetValidHalfMoveHexes(), Battler.CurrentDisplayMode);
        SetSpriteOutlines(Battler.GetGlobalMousePosition());

        Battler.EmitSignal(Battler.SignalName.TurnStarted, Battler.CharactersAwaitingTurn[0].CharacterData.KnownSpells);

        if (Battler.CharactersAwaitingTurn[0].CharacterData.Berserk)
        {
            GD.Print("TODO - berserk effect");
            //
            // do berserk AI effect here -> 1. get the nearest target. 2. if the target is within melee range, attack. 3. otherwise, shoot if possible. 4. otherwise, move towards it.
        }

        _controlIdleBattleState.OnInitCurrentTurn();
    }

    public void SetSpriteOutlines(Vector2 mousePos)
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
    public override void RecalculateUserHexes()
    {
        Battler.SetGridUserHexes(GetValidMoveHexes(), GetValidHalfMoveHexes(), Battler.CurrentDisplayMode);
    }

    public override void OnMovedButStillHaveAP()
    {
        Battler.SetGridUserHexes(GetValidMoveHexes(), GetValidHalfMoveHexes(), Battler.CurrentDisplayMode);
    }

    private bool HumanTurn()
    {
        return Battler.CharactersAwaitingTurn[0].StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player;
    }

    // private bool IsValidWorldMove(Vector2 startWorldPos, Vector2 endWorldPos)
    // {
    //     return IsValidGridMove(Battler.BattleGrid.WorldToGrid(startWorldPos), Battler.BattleGrid.WorldToGrid(endWorldPos));
    // }

    // Get all of the move hexes valid for the active character
    private List<Vector2> GetValidMoveHexes()
    {
        // System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
        // stopwatch.Stop();
        // GD.Print($"Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
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
            int moveCost = GridMoveCost(characterGridPos, kv.Key);
            // moveCost = (moveCost == -1) ? 0 : moveCost;
            if (moveCost > 0 && moveCost <=
                Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] - MeleeRangedCastAPCost())
            {
                result.Add(kv.Key);
            }
        }
        return result;
    }
    public bool IsValidGridMove(Vector2 startGridPos, Vector2 endGridPos)
    {
        // GD.Print(GridMoveCost(startGridPos, endGridPos) == 0 ? "cant move to same square" : Battler.CharactersAwaitingTurn[0].CharacterData.ActionPoints + " available; cost is " + GridMoveCost(startGridPos, endGridPos));
        return GridMoveCost(startGridPos, endGridPos) > 0 &&
            GridMoveCost(startGridPos, endGridPos) <=
            Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints];
    }
    public bool IsValidGridMoveSpecificAP(Vector2 startGridPos, Vector2 endGridPos, int ap)
    {
        // GD.Print(GridMoveCost(startGridPos, endGridPos) == 0 ? "cant move to same square" : Battler.CharactersAwaitingTurn[0].CharacterData.ActionPoints + " available; cost is " + GridMoveCost(startGridPos, endGridPos));
        return GridMoveCost(startGridPos, endGridPos) > 0 &&
            GridMoveCost(startGridPos, endGridPos) <= ap;
    }

    private int WorldMoveCost(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        return GridMoveCost(Battler.BattleGrid.WorldToGrid(startWorldPos), Battler.BattleGrid.WorldToGrid(endWorldPos));
        // Battler.CharactersAwaitingTurn.Remove(Battler.CharactersAwaitingTurn.ToList()[0]);
    }
    public int GridMoveCost(Vector2 startGridPos, Vector2 endGridPos)
    {
        // subtract starting hex....
        return Battler.BattleGrid.HexNavigation.CalculateGridPath(startGridPos, endGridPos).Length - 1;
        // Battler.CharactersAwaitingTurn.Remove(Battler.CharactersAwaitingTurn.ToList()[0]);
    }

    public int MeleeRangedCastAPCost()
    {
        return Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.MaxActionPoints] / 2; //return (int)Math.Floor(((float)Battler.CharactersAwaitingTurn[0].CharacterData.MaxActionPoints) * 0.5f);
    }

    public bool CanAfford(int cost)
    {
        return Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] >= cost;
    }

    public bool IsNeighbour(Vector2 originGridPos, Vector2 targetGridPos)
    {

        Hexagon hex = Battler.BattleGrid.GetHexAtGridPosition(targetGridPos);
        List<Vector2> surroundingGridPositions = Battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);
        if (surroundingGridPositions.Contains(originGridPos))
        {
            return true;
        }
        return false;
    }


    public override void ProcessUpdate(double delta)
    {
        base.ProcessUpdate(delta);
        if (_controlIdleBattleState == null)
        {
            return;
        }
        _controlIdleBattleState.ProcessUpdate(delta);
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
        if (_controlIdleBattleState == null)
        {
            return;
        }

        _controlIdleBattleState.InputUpdate(ev);


    }


    public bool IsValidMelee(Vector2 mouseGridPos, Vector2 characterGridPos)
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
            int moveCost = GridMoveCost(characterGridPos, closestPos);
            moveCost = (moveCost == -1) ? 0 : moveCost;
            if (validTarget && CanAfford(moveCost + MeleeRangedCastAPCost()))
            {
                return true;
            }
        }
        return false;
    }

    // if (GridMoveCost(characterGridPos, kv.Key) > 0 && GridMoveCost(characterGridPos, kv.Key) <=
    //     Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] - MeleeRangedCastCost())
    public bool IsValidRanged(Vector2 mouseGridPos, int range)
    {
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (IsValidRangedTarget(mouseGridPos))
        {
            if (CanAfford(MeleeRangedCastAPCost()))
            {
                if (WithinShootingRange(targetCharacter, range))
                {
                    if (!Battler.BattleGrid.IsLOSBlocked(controlledCharacter.GlobalPosition, Battler.GetGlobalMousePosition()))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool IsValidRangedTarget(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (targetCharacter == null)
        {
            return false;
        }
        if (controlledCharacter.ValidEnemyTargets.Contains(targetCharacter.StatusToPlayer))
        {
            return true;
        }

        return false;
    }

    public bool WithinShootingRange(CharacterUnit targetCharacter, int range)
    {
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (targetCharacter == null)
        {
            return false;
        }
        return Battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, targetCharacter.GlobalPosition) <= range;

    }

    public bool IsValidSpell(Vector2 mouseGridPos, SpellEffectManager.SpellMode spell)
    {
        if (spell == SpellEffectManager.SpellMode.None)
        {
            return false;
        }
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (IsValidSpellTarget(mouseGridPos, spell))
        {
            if (TargetWithinRange(Battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, Battler.GetGlobalMousePosition()), Battler.AllSpells[spell].Range))
            {
                if (!Battler.BattleGrid.IsLOSBlocked(controlledCharacter.GlobalPosition, Battler.GetGlobalMousePosition()))
                {
                    SpellEffectManager.Spell.PatronMode patron = Battler.AllSpells[spell].Patron;
                    if ((patron == SpellEffectManager.Spell.PatronMode.Shamash && CanAffordChargeCost(Battler.AllSpells[spell])) || (patron == SpellEffectManager.Spell.PatronMode.Ishtar && CanAffordReagentCost(Battler.AllSpells[spell])))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool CanAffordChargeCost(SpellEffectManager.Spell spell)
    {
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        return controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.FocusCharge] >= spell.ChargeCost;
    }

    public bool CanAffordReagentCost(SpellEffectManager.Spell spell)
    {
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        return controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.Reagents] >= spell.ReagentCost;
    }

    public bool IsValidSpellTarget(Vector2 mouseGridPos, SpellEffectManager.SpellMode spell)
    {
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
        // GD.Print(currentSelectedSpell.Target);
        return validEnemySpellTarget || validAllySpellTarget || validGroundSpellTarget;
    }

    public bool TargetWithinRange(int gridDistance, int range)
    {
        return gridDistance <= range;
    }

    private bool IsMouseOverCharacterOrEmpty(Vector2 mouseGridPos)
    {
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(mouseGridPos);
        if (Battler.BattleGrid.GetHexAtGridPosition(mouseGridPos) == null) // BUG???
        {
            return false;
        }
        if (targetCharacter != null || !Battler.BattleGrid.GetHexAtGridPosition(mouseGridPos).Obstacle)
        {
            return true;
        }
        return false;
    }




    public int GetSpellNumberAffected(SpellEffectManager.Spell spell, Vector2 gridPos)
    {
        if (spell.Target == SpellEffectManager.Spell.TargetMode.Ground)
        {
            return Battler.GetAffectedAreaSpellCharacters(spell, gridPos).Count;
        }
        return 0;
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

    public void EndTurnEarly()
    {
        Battler.CharactersAwaitingTurn[0].BattleSkipOrder();
        Battler.EmitSignal(Battler.SignalName.LogBattleText,
            string.Format("{0} ends their turn early.", Battler.CharactersAwaitingTurn[0].CharacterData.Name),
            true);
    }

    public override void OnBtnEndTurnPressed()
    {
        if (HumanTurn())
        {
            EndTurnEarly();
        }
    }

    public override void SetPlayerAction(Battler.ActionMode action)
    {
        _controlIdleBattleState.SetPlayerAction(action);
    }

    // for player this is battler.CurrentAction (this should be ideally placed in player state), mouseGridPos, and Battler.CharactersAwaitingTurn[0].UISelectedSpell
    public void DoAction(Battler.ActionMode action, Vector2 targetGridPos, SpellEffectManager.SpellMode spellMode)
    {
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(Battler.CharactersAwaitingTurn[0].GlobalPosition);
        Vector2 closestPos = new();// surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(targetGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];

        if (action == Battler.ActionMode.Move)
        {
            int moveCost = GridMoveCost(characterGridPos, targetGridPos);
            List<Vector2> worldPath = Battler.BattleGrid.GridToWorldPath(
                Battler.BattleGrid.HexNavigation.CalculateGridPath(characterGridPos, targetGridPos)
            );
            Battler.CharactersAwaitingTurn[0].BattleMoveOrder(moveCost, worldPath);

            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
                moveCost, controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]), true);
        }
        else if (action == Battler.ActionMode.Melee)
        {
            if (!IsNeighbour(characterGridPos, targetGridPos))
            {

                Hexagon hex = Battler.BattleGrid.GetHexAtGridPosition(targetGridPos);
                List<Vector2> surroundingGridPositions = Battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);
                surroundingGridPositions.RemoveAll(pos => !IsValidGridMove(characterGridPos, pos));
                // surroundingGridPositions guaranteed to be non-zero because IsValidMelee checked prior to this method
                closestPos = surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
            }
            if (IsNeighbour(characterGridPos, targetGridPos))
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
        else if (action == Battler.ActionMode.Shoot)
        {
            SpellEffectManager.Spell spell = Battler.AllSpells[SpellEffectManager.SpellMode.Arrow];
            spell.Origin = controlledCharacter.GlobalPosition;
            spell.Destination = targetCharacter.GlobalPosition;
            spell.OriginCharacter = controlledCharacter;
            spell.TargetCharacter = targetCharacter;
            spell.AssociatedEffects[0].DamageDice = controlledCharacter.CharacterData.WeaponDiceRanged;
            controlledCharacter.BattleShootOrder(spell);
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} shoots {1}.", controlledCharacter.CharacterData.Name,
                targetCharacter.CharacterData.Name), true);
        }
        else if (action == Battler.ActionMode.Cast)
        {
            Vector2 centredWorldPos = Battler.BattleGrid.GridToWorld(targetGridPos);
            // NEEd to reset everything or it bugs out in future castings. next time use 
            SpellEffectManager.Spell spell = Battler.AllSpells[spellMode]; // Battler.CharactersAwaitingTurn[0].UISelectedSpell
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
        else if (action == Battler.ActionMode.Hint)
        {
            if (targetCharacter != null)
            {
                Battler.EmitSignal(Battler.SignalName.HintClickedCharacter, false, targetCharacter.CharacterData);
            }
            return;
        }
        else if (action == Battler.ActionMode.Invalid)
        {
            // Battler.EmitSignal(Battler.SignalName.LogBattleText, "Invalid target", false);
            return;
        }
        else if (action == Battler.ActionMode.None)
        {
            if (!HumanTurn())
            {
                EndTurnEarly();
            }
            return;
        }
        Battler.SetState(Battler.BattleMode.Processing);
    }
}
