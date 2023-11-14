using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PlayerIdleBattleState : ControlIdleBattleState
{
    public PlayerIdleBattleState(IdleBattleState idleBattleState)
    {
        IdleBattleState = idleBattleState;
    }

    public Battler.ActionMode CurrentAction { get; set; } = Battler.ActionMode.Move;

    private List<Vector2> _moveHexes;
    // private List<Vector2> _allHexes;
    private List<Vector2> _halfMoveHexes;
    public override void OnInitCurrentTurn()
    {
        base.OnInitCurrentTurn();

        IdleBattleState.Battler.SetOutlineColours();
        SetContextualAction(IdleBattleState.Battler.BattleGrid.WorldToGrid(IdleBattleState.Battler.GetGlobalMousePosition()));
        _moveHexes = IdleBattleState.GetValidMoveHexes();
        // _allHexes = IdleBattleState.Battler.GetAllNonObstacleGridPositions();
        _halfMoveHexes = IdleBattleState.GetValidHalfMoveHexes();
        IdleBattleState.Battler.SetGridUserHexes(_moveHexes, _halfMoveHexes, IdleBattleState.Battler.CurrentDisplayMode);

    }

    // private void EnsureHexDisplay()
    // {
    //     if (_moveHexes == null || _allHexes == null)
    //     {
    //         return;
    //     }
    //     if (_allHexes.Count == 0)
    //     {
    //         return;
    //     }
    //     if (IdleBattleState.Battler.AreHexesHidden(IdleBattleState.Battler.CurrentDisplayMode, _moveHexes, _allHexes))
    //     {
    //         IdleBattleState.Battler.SetGridUserHexes(_moveHexes, _halfMoveHexes, IdleBattleState.Battler.CurrentDisplayMode);
    //     }
    // }

    // public override void ProcessUpdate(double delta)
    // {
    //     base.ProcessUpdate(delta);
    //     Battler battler = IdleBattleState.Battler;
    //     CharacterUnit activeCharacter = battler.CharactersAwaitingTurn[0];
    //     StoryCharacterData activeData = activeCharacter.CharacterData;

    //     // if (activeData.Berserk)
    //     // {
    //     //     IdleBattleState.BerserkEffect();
    //     //     return;
    //     // }
    // }

    public override void InputUpdate(InputEvent ev)
    {
        Battler battler = IdleBattleState.Battler;

        Vector2 mousePos = battler.GetGlobalMousePosition();
        Vector2 mouseGridPos = battler.BattleGrid.WorldToGrid(mousePos);

        Vector2 characterWorldPos = battler.CharactersAwaitingTurn[0].GlobalPosition;
        Vector2 characterGridPos = battler.BattleGrid.WorldToGrid(characterWorldPos);
        if (battler.BattleGrid.GetHexAtGridPosition(characterGridPos).Obstacle)
        {
            return;
        }

        // The below line is a hack because when someone is berserk before player turn for some reason it turns off the hexes. doesnt seem t imapct performance...
        IdleBattleState.Battler.SetGridUserHexes(_moveHexes, _halfMoveHexes, IdleBattleState.Battler.CurrentDisplayMode);

        SetContextualAction(mouseGridPos);

        if (ev is InputEventMouseButton btn)
        {
            if (btn.Pressed && btn.ButtonIndex == MouseButton.Left)
            {
                if (!IsOverUI(mousePos))
                {
                    IdleBattleState.DoAction(CurrentAction, mouseGridPos, battler.CharactersAwaitingTurn[0].UISelectedSpell);
                }
            }
            else if (btn.Pressed && btn.ButtonIndex == MouseButton.Right)
            {
                CharacterUnit targetCharacter = battler.CharacterAtGridPos(mouseGridPos);
                if (targetCharacter != null)
                {
                    battler.EmitSignal(Battler.SignalName.HintClickedCharacter, true, targetCharacter.CharacterData);
                }
            }
        }
    }


    public override void SetPlayerAction(Battler.ActionMode action)
    {
        CurrentAction = action;
    }

    // private void DoAction()
    // {
    //     Battler battler = IdleBattleState.Battler;
    //     Vector2 mousePos = battler.GetGlobalMousePosition();
    //     Vector2 mouseGridPos = battler.BattleGrid.WorldToGrid(mousePos);
    //     Vector2 centredWorldPos = battler.BattleGrid.GridToWorld(mouseGridPos);
    //     Vector2 characterGridPos = battler.BattleGrid.WorldToGrid(battler.CharactersAwaitingTurn[0].GlobalPosition);
    //     Vector2 closestPos = new();// surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
    //     CharacterUnit targetCharacter = battler.CharacterAtGridPos(mouseGridPos);
    //     CharacterUnit controlledCharacter = battler.CharactersAwaitingTurn[0];

    //     if (CurrentAction == Battler.ActionMode.Move)
    //     {
    //         int moveCost = IdleBattleState.GridMoveCost(characterGridPos, mouseGridPos);
    //         List<Vector2> worldPath = battler.BattleGrid.GridToWorldPath(
    //             battler.BattleGrid.HexNavigation.CalculateGridPath(characterGridPos, mouseGridPos)
    //         );
    //         battler.CharactersAwaitingTurn[0].BattleMoveOrder(moveCost, worldPath);

    //         battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
    //             moveCost, controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]), true);
    //     }
    //     else if (CurrentAction == Battler.ActionMode.Melee)
    //     {
    //         if (!IdleBattleState.IsNeighbour(characterGridPos, mouseGridPos))
    //         {

    //             Hexagon hex = battler.BattleGrid.GetHexAtGridPosition(mouseGridPos);
    //             List<Vector2> surroundingGridPositions = battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);
    //             surroundingGridPositions.RemoveAll(pos => !IdleBattleState.IsValidGridMove(characterGridPos, pos));
    //             // surroundingGridPositions guaranteed to be non-zero because IsValidMelee checked prior to this method
    //             closestPos = surroundingGridPositions.OrderBy(x => IdleBattleState.GridMoveCost(characterGridPos, x)).ToList()[0];
    //         }
    //         if (IdleBattleState.IsNeighbour(characterGridPos, mouseGridPos))
    //         {
    //             controlledCharacter.BattleMeleeOrder(targetCharacter);
    //         }
    //         else
    //         {
    //             // if not adjacent, then first move the character to the target, THEN melee the target
    //             int moveCost = IdleBattleState.GridMoveCost(characterGridPos, closestPos);
    //             List<Vector2> worldPath = battler.BattleGrid.GridToWorldPath(
    //                 battler.BattleGrid.HexNavigation.CalculateGridPath(characterGridPos, closestPos)
    //             );
    //             controlledCharacter.BattleMoveOrder(moveCost, worldPath, targetCharacter);
    //             battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
    //                 moveCost, controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]), true);
    //         }
    //         battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} strikes {1} in melee combat.", controlledCharacter.CharacterData.Name,
    //             targetCharacter.CharacterData.Name), true);
    //     }
    //     else if (CurrentAction == Battler.ActionMode.Shoot)
    //     {
    //         SpellEffectManager.Spell spell = battler.AllSpells[SpellEffectManager.SpellMode.Arrow];
    //         spell.Origin = controlledCharacter.GlobalPosition;
    //         spell.Destination = targetCharacter.GlobalPosition;
    //         spell.OriginCharacter = controlledCharacter;
    //         spell.TargetCharacter = targetCharacter;
    //         spell.AssociatedEffects[0].DamageDice = controlledCharacter.CharacterData.WeaponDiceRanged;
    //         controlledCharacter.BattleShootOrder(spell);
    //         battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} shoots {1}.", controlledCharacter.CharacterData.Name,
    //             targetCharacter.CharacterData.Name), true);
    //     }
    //     else if (CurrentAction == Battler.ActionMode.Cast)
    //     {
    //         // NEEd to reset everything or it bugs out in future castings. next time use 
    //         SpellEffectManager.Spell spell = battler.AllSpells[battler.CharactersAwaitingTurn[0].UISelectedSpell];
    //         spell.Origin = controlledCharacter.GlobalPosition;
    //         spell.Destination = centredWorldPos;
    //         spell.OriginCharacter = controlledCharacter;
    //         spell.TargetCharacter = targetCharacter;
    //         spell.HexDistance = battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, centredWorldPos);
    //         spell.Outcome = null;
    //         spell.AreaAffectedCharacters = new();
    //         controlledCharacter.BattleCastOrder(spell);
    //         battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} casts {1}.", controlledCharacter.CharacterData.Name,
    //             spell.Name), true);
    //     }
    //     else if (CurrentAction == Battler.ActionMode.Hint)
    //     {
    //         if (targetCharacter != null)
    //         {
    //             battler.EmitSignal(Battler.SignalName.HintClickedCharacter, false, targetCharacter.CharacterData);
    //         }
    //         return;
    //     }
    //     else if (CurrentAction == Battler.ActionMode.Invalid)
    //     {
    //         OnInvalidTarget();
    //         return;
    //     }
    //     else if (CurrentAction == Battler.ActionMode.None)
    //     {
    //         return;
    //     }
    //     battler.SetState(Battler.BattleMode.Processing);
    // }

    private void OnInvalidTarget()
    {
        IdleBattleState.Battler.EmitSignal(Battler.SignalName.LogBattleText, "Invalid target", false);
        // GD.Print("invalid target - play BZZZT sound here");
    }


    private BattleLogParser.InvalidReasonMode GetInvalidReasonMode(Vector2 characterGridPos, Vector2 mouseGridPos)
    {
        Battler battler = IdleBattleState.Battler;
        // if (action == Battler.ActionMode.Move)
        // {
        if (battler.PlayerSelectedAction == Battler.ActionMode.Move || battler.PlayerSelectedAction == Battler.ActionMode.Melee)
        {
            int moveCost = IdleBattleState.GridMoveCost(characterGridPos, mouseGridPos);
            if (battler.PlayerSelectedAction == Battler.ActionMode.Melee)
            {
                if (!IdleBattleState.CanAfford(IdleBattleState.MeleeRangedCastAPCost()))
                {
                    return BattleLogParser.InvalidReasonMode.NotEnoughAP;
                }
            }
            if (moveCost == -1)
            {
                return BattleLogParser.InvalidReasonMode.NotEnoughAP;
            }
            else if (!IdleBattleState.CanAfford(moveCost))
            {
                return BattleLogParser.InvalidReasonMode.NotEnoughAP;
            }
        }
        else if (battler.PlayerSelectedAction == Battler.ActionMode.Cast)
        {
            SpellEffectManager.SpellMode spell = battler.CharactersAwaitingTurn[0].UISelectedSpell;
            if (!IdleBattleState.IsValidSpellTarget(mouseGridPos, spell))
            {
                return BattleLogParser.InvalidReasonMode.InvalidTarget;
            }
            else if (!IdleBattleState.TargetWithinRange(DistanceFromCurrentCharToMouseWorld(), battler.AllSpells[spell].Range))
            {
                return BattleLogParser.InvalidReasonMode.OutOfRange;
            }
            else if (battler.BattleGrid.IsLOSBlocked(battler.CharactersAwaitingTurn[0].GlobalPosition, battler.GetGlobalMousePosition()))
            {
                return BattleLogParser.InvalidReasonMode.LOSBlocked;
            }
            else if (!IdleBattleState.CanAfford(IdleBattleState.MeleeRangedCastAPCost()))
            {
                return BattleLogParser.InvalidReasonMode.NotEnoughAP;
            }
            else if (battler.AllSpells[spell].Patron == SpellEffectManager.Spell.PatronMode.Shamash && !IdleBattleState.CanAffordChargeCost(battler.AllSpells[spell]))
            {
                return BattleLogParser.InvalidReasonMode.NotEnoughCharge;
            }
            else if (battler.AllSpells[spell].Patron == SpellEffectManager.Spell.PatronMode.Ishtar && !IdleBattleState.CanAffordReagentCost(battler.AllSpells[spell]))
            {
                return BattleLogParser.InvalidReasonMode.NotEnoughReagents;
            }

        }
        else if (battler.PlayerSelectedAction == Battler.ActionMode.Shoot)
        {
            if (!IdleBattleState.IsValidRangedTarget(mouseGridPos))
            {
                return BattleLogParser.InvalidReasonMode.InvalidTarget;
            }
            else if (!IdleBattleState.CanAfford(IdleBattleState.MeleeRangedCastAPCost()))
            {
                return BattleLogParser.InvalidReasonMode.NotEnoughAP;
            }
            else if (!IdleBattleState.WithinShootingRange(battler.CharacterAtGridPos(mouseGridPos), battler.AllSpells[SpellEffectManager.SpellMode.Arrow].Range))
            {
                return BattleLogParser.InvalidReasonMode.OutOfRange;
            }
            else if (battler.BattleGrid.IsLOSBlocked(battler.CharactersAwaitingTurn[0].GlobalPosition, battler.GetGlobalMousePosition()))
            {
                return BattleLogParser.InvalidReasonMode.LOSBlocked;
            }
        }
        return BattleLogParser.InvalidReasonMode.InvalidTarget;
        // }
    }

    // private bool IsValidRanged(Vector2 mouseGridPos, int range)
    // {
    //     CharacterUnit targetCharacter = battler.CharacterAtGridPos(mouseGridPos);
    //     if (IsValidRangedTarget(mouseGridPos, range))
    //     {
    //         if (CanAfford(MeleeRangedCastCost()))
    //         {
    //             if (WithinShootingRange(targetCharacter, range))
    //             {
    //                 return true;
    //             }
    //         }
    //     }
    //     return false;
    // }
    private int DistanceFromCurrentCharToMouseWorld()
    {
        Battler battler = IdleBattleState.Battler;
        return battler.BattleGrid.GetHexDistanceByWorld(battler.CharactersAwaitingTurn[0].GlobalPosition, battler.GetGlobalMousePosition());
    }

    private void SetContextualAction(Vector2 mouseGridPos)
    {
        Battler battler = IdleBattleState.Battler;
        CharacterUnit targetCharacter = battler.CharacterAtGridPos(mouseGridPos);
        Vector2 characterGridPos = battler.BattleGrid.WorldToGrid(battler.CharactersAwaitingTurn[0].GlobalPosition);
        battler.AllSpells[battler.CharactersAwaitingTurn[0].UISelectedSpell].TargetCharacter = targetCharacter;
        IdleBattleState.SetSpriteOutlines(battler.GetGlobalMousePosition());

        CurrentAction = Battler.ActionMode.Invalid;
        // CAST
        if (IdleBattleState.IsValidSpell(mouseGridPos, battler.CharactersAwaitingTurn[0].UISelectedSpell) && battler.PlayerSelectedAction == Battler.ActionMode.Cast)
        {
            battler.CursorControl.SetCursor(CursorControl.CursorMode.Spell);
            CurrentAction = Battler.ActionMode.Cast;
            // battler.EmitSignal(battler.SignalName.LogBattleText, String.Format("Cast {0}", battler.AllSpells[battler.CharactersAwaitingTurn[0].UISelectedSpell].Name), false);
        }
        // RANGED
        else if (IdleBattleState.IsValidRanged(mouseGridPos, battler.AllSpells[SpellEffectManager.SpellMode.Arrow].Range) && battler.PlayerSelectedAction == Battler.ActionMode.Shoot)
        {
            battler.CursorControl.SetCursor(CursorControl.CursorMode.Ranged);
            CurrentAction = Battler.ActionMode.Shoot;
            // battler.EmitSignal(battler.SignalName.LogBattleText, String.Format("Shoot {0}", targetCharacter.CharacterData.Name), false);
        }
        // MELEE
        else if (IdleBattleState.IsValidMelee(mouseGridPos, characterGridPos))
        {
            battler.CursorControl.SetCursor(CursorControl.CursorMode.Melee);
            CurrentAction = Battler.ActionMode.Melee;
            // battler.EmitSignal(battler.SignalName.LogBattleText, String.Format("Strike {0}", targetCharacter.CharacterData.Name), false);
        }
        // MOVE
        else if (IdleBattleState.IsValidGridMove(characterGridPos, mouseGridPos))
        {
            battler.CursorControl.SetCursor(CursorControl.CursorMode.Move);
            CurrentAction = Battler.ActionMode.Move;
            // battler.EmitSignal(battler.SignalName.LogBattleText, "Move to location", false);
        }
        // HINT
        else if (IsMouseOverAlly(mouseGridPos) &&
            !(battler.PlayerSelectedAction == Battler.ActionMode.Cast &&
            battler.AllSpells[battler.CharactersAwaitingTurn[0].UISelectedSpell].Target == SpellEffectManager.Spell.TargetMode.Ally))
        {
            battler.CursorControl.SetCursor(CursorControl.CursorMode.Hint);
            CurrentAction = Battler.ActionMode.Hint;
            // battler.EmitSignal(battler.SignalName.LogBattleText, "Click for more information", false);
        }
        else if (IsOverUI(battler.GetGlobalMousePosition()))
        {
            battler.CursorControl.SetCursor(CursorControl.CursorMode.Select);
            CurrentAction = Battler.ActionMode.None;
            return; // should get UIHINT in the log instead
            // battler.EmitSignal(battler.SignalName.LogBattleText, "", false);
        }
        else // outside the grid bounds
        {
            // Set valid error message to log TODODODO!!!!
            CurrentAction = Battler.ActionMode.Invalid;
            battler.CursorControl.SetCursor(CursorControl.CursorMode.Invalid);
            // battler.EmitSignal(battler.SignalName.LogBattleText, "", false);
        }
        battler.EmitSignal(Battler.SignalName.LogBattleText,
            BattleLogParser.ParseAction(CurrentAction, battler.AllSpells[battler.CharactersAwaitingTurn[0].UISelectedSpell],
                IdleBattleState.GetSpellNumberAffected(battler.AllSpells[battler.CharactersAwaitingTurn[0].UISelectedSpell], mouseGridPos),
                battler.CharactersAwaitingTurn[0].CharacterData.Name, targetCharacter == null ? "" : targetCharacter.CharacterData.Name,
                GetInvalidReasonMode(characterGridPos, mouseGridPos)),
                false);


    }// Player only
    private bool IsMouseOverAlly(Vector2 mouseGridPos)
    {
        Battler battler = IdleBattleState.Battler;
        CharacterUnit targetCharacter = battler.CharacterAtGridPos(mouseGridPos);
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
        Battler battler = IdleBattleState.Battler;
        return battler.UIBounds.HasPoint(mousePos);
    }

}