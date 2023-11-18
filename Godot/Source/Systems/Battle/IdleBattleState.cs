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

    private bool IsAdjacentToEnemy(Vector2 originPos)
    {
        CharacterUnit activeCharacter = Battler.CharactersAwaitingTurn[0];
        // Vector2 characterWorldPos = Battler.CharactersAwaitingTurn[0].GlobalPosition;
        // Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(characterWorldPos);
        List<Vector2> enemyPositions = Battler.AllCharacters
            .Where(x => activeCharacter.CharacterData.Berserk ? activeCharacter.BerserkValidEnemyTargets.Contains(x.StatusToPlayer) : activeCharacter.ValidEnemyTargets.Contains(x.StatusToPlayer))
            .Where(x => x.CharacterData.Alive)
            .Select(x => Battler.BattleGrid.WorldToGrid(x.GlobalPosition))
            .ToList();
        foreach (Vector2 vec in enemyPositions)
        {
            if (IsNeighbour(originPos, vec))
            {
                return true;
            }
        }
        return false;
    }

    private void SetAPAtStartOfTurn()
    {
        CharacterUnit activeCharacter = Battler.CharactersAwaitingTurn[0];
        Vector2 characterWorldPos = Battler.CharactersAwaitingTurn[0].GlobalPosition;
        Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(characterWorldPos);
        if (IsAdjacentToEnemy(characterGridPos))
        {
            activeCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] = MeleeRangedCastAPCost();
        }
        else
        {
            Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] = Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.MaxActionPoints];
        }
    }

    private void InitCurrentTurn()
    {
        // CharacterUnit activeCharacter = Battler.CharactersAwaitingTurn[0];
        // StoryCharacterData activeData = activeCharacter.CharacterData;&& !activeData.Berserk
        if (Battler.CharactersAwaitingTurn[0].CharacterData.Berserk)
        {
            // GD.Print(Battler.CharactersAwaitingTurn[0].CharacterData.Name + " is berserk");
            SetControlState(IdleBattleControlMode.AI);
        }
        else
        {

            DoLeadershipBonus();


            SetControlState(HumanTurn() ? IdleBattleControlMode.Player : IdleBattleControlMode.AI);
        }

        if (!Battler.CharactersAwaitingTurn[0].TurnPending)
        {
            SetAPAtStartOfTurn();
            Battler.CharactersAwaitingTurn[0].TurnPending = true;
        }
        Battler.CharactersAwaitingTurn[0].CharacterStartBattleTurn();


        SetSpriteOutlines(Battler.GetGlobalMousePosition());

        Battler.EmitSignal(Battler.SignalName.TurnStarted, Battler.CharactersAwaitingTurn[0].CharacterData.KnownSpells);

        _controlIdleBattleState.OnInitCurrentTurn();
    }

    private void DoLeadershipBonus()
    {
        CharacterUnit currentChar = Battler.CharactersAwaitingTurn[0];
        StoryCharacterData data = currentChar.CharacterData;
        int leadership = data.Stats[StoryCharacterData.StatMode.Leadership];

        StoryCharacterData.AttributeMode attributeAffected = (StoryCharacterData.AttributeMode)currentChar.Rand.Next(0, Enum.GetValues(typeof(StoryCharacterData.AttributeMode)).Length);
        int magnitude = leadership / 5;
        string name = string.Format("Leadership ({1}): {0}", attributeAffected.ToString(), data.Name);

        // Debug print statements
        // GD.Print("All characters:");
        // foreach (var character in Battler.AllCharacters)
        // {
        //     GD.Print($"Character: {character.CharacterData.Name}, Alive: {character.CharacterData.Alive}, StatusToPlayer: {character.StatusToPlayer}");
        // }

        Battler.AllCharacters
            .Where(x => x.CharacterData.Alive)
            .Where(x => x != currentChar)
            .Where(x => currentChar.ValidAllyTargets.Contains(x.StatusToPlayer))
            .ToList()
            .ForEach(x =>
            {
                CharacterRoundEffect leadershipEffect = new
                (
                    name: name,
                    attributeAffected: attributeAffected,
                    statAffected: StoryCharacterData.StatMode.Endurance,
                    effectType: CharacterRoundEffect.EffectTypeMode.Attribute,
                    rounds: 3,
                    permanent: false,
                    cumulative: false,
                    magnitude: magnitude,
                    animName: "", // todo INSPIRATION animation
                    fromSpell: SpellEffectManager.SpellMode.None,
                    from: data.Name
                );
                // Debug print statement for characters that pass the filtering
                // GD.Print($"Applying effect to: {x.CharacterData.Name}, StatusToPlayer: {x.StatusToPlayer}");
                x.CharacterData.DoEffectInitial(leadershipEffect);
            });
    }

    // public bool HexDisplaySet { get; set; } = false;

    // public void BerserkEffect()
    // {
    //     Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} is berserk!", Battler.CharactersAwaitingTurn[0].CharacterData.Name), true);
    //     Vector2 characterWorldPos = Battler.CharactersAwaitingTurn[0].GlobalPosition;
    //     Vector2 characterGridPos = Battler.BattleGrid.WorldToGrid(characterWorldPos);

    //     CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];

    //     List<Vector2> allCharacterPositions = Battler.AllCharacters
    //         .Where(x => x.CharacterData.Alive)
    //         .Where(x => x != controlledCharacter)
    //         .Select(x => Battler.BattleGrid.WorldToGrid(x.GlobalPosition))
    //         .ToList();

    //     // 1. if adjacent to a character, melee attack them
    //     foreach (Vector2 gridPos in allCharacterPositions)
    //     {
    //         if (IsNeighbour(characterGridPos, gridPos))
    //         {
    //             // if we cant afford to attack them but we are neighbouring them, do nothing
    //             if (controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] < MeleeRangedCastAPCost())
    //             {
    //                 EndTurnEarly();
    //                 return;
    //             }

    //             DoAction(Battler.ActionMode.Melee, gridPos, SpellEffectManager.SpellMode.None);
    //             return;
    //         }
    //     }
    //     // 2. otherwise move to closest enemy

    //     List<Vector2> validMoves = GetValidMoveHexes();
    //     if (validMoves.Count == 0)
    //     {
    //         EndTurnEarly();
    //         return;
    //     }
    //     Vector2 closest = ClosestToEnemy(validMoves, allCharacterPositions);
    //     DoAction(Battler.ActionMode.Move, closest, SpellEffectManager.SpellMode.None);

    // }


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
        // if (Battler.CharactersAwaitingTurn[0].CharacterData.Berserk)
        // {
        //     BerserkEffect();
        // }
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
    public List<Vector2> GetValidMoveHexes()
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
    public List<Vector2> GetValidHalfMoveHexes()
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
        // GD.Print(startGridPos, " ", endGridPos);//GridMoveCost(startGridPos, endGridPos) == 0 ? "cant move to same square" : Battler.CharactersAwaitingTurn[0].CharacterData.ActionPoints + " available; cost is " + GridMoveCost(startGridPos, endGridPos));
        return GridMoveCost(startGridPos, endGridPos) > 0 &&
            GridMoveCost(startGridPos, endGridPos) <=
            Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints];
    }
    public bool IsValidGridMoveSpecificAP(Vector2 startGridPos, Vector2 endGridPos, int ap)
    {
        // if (!(GridMoveCost(startGridPos, endGridPos) > 0 &&
        //     GridMoveCost(startGridPos, endGridPos) <= ap))
        // {
        //     GD.Print("invalid:");
        //     GD.Print("startPos: ", startGridPos);
        //     GD.Print("endPos: ", endGridPos);
        //     GD.Print("total cost: ", GridMoveCost(startGridPos, endGridPos));
        // }
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

        // GD.Print("current mouse grid position 🐱😿: ", Battler.BattleGrid.WorldToGrid(Battler.GetGlobalMousePosition()));

        _controlIdleBattleState.InputUpdate(ev);


    }


    public bool IsValidMelee(Vector2 targetGridPos, Vector2 characterGridPos)
    {
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        bool isBerserk = controlledCharacter.CharacterData.Berserk;

        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(targetGridPos);

        if (targetCharacter == null)
        {
            return false;
        }

        bool validTarget;

        if (isBerserk)
        {
            // Berserk-specific logic
            validTarget = controlledCharacter.BerserkValidEnemyTargets.Contains(targetCharacter.StatusToPlayer);
            // GD.Print("Berserk");
            // GD.Print("valid targets are:");
            // controlledCharacter.BerserkValidEnemyTargets.ForEach(x => GD.Print(x));
            // GD.Print("The target character is of target type: ", targetCharacter.StatusToPlayer);
            // GD.Print("so outcome is: ", validTarget);
        }
        else
        {
            // Non-Berserk logic
            validTarget = controlledCharacter.ValidEnemyTargets.Contains(targetCharacter.StatusToPlayer);
            // GD.Print("NOT berserk");
            // GD.Print("valid targets are:");
            // controlledCharacter.ValidEnemyTargets.ForEach(x => GD.Print(x));
            // GD.Print("The target character is of target type: ", targetCharacter.StatusToPlayer);
            // GD.Print("so outcome is: ", validTarget);
        }
        if (!IsNeighbour(characterGridPos, targetGridPos))
        {
            Hexagon hex = Battler.BattleGrid.GetHexAtGridPosition(targetGridPos);
            List<Vector2> surroundingGridPositions = Battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);

            // if (isBerserk)
            // {
            //     GD.Print(targetCharacter.CharacterData.Name, "surroundingGridPositions: ", surroundingGridPositions.Count);
            // }

            surroundingGridPositions.RemoveAll(pos => !IsValidGridMove(characterGridPos, pos));

            if (surroundingGridPositions.Count == 0 || !validTarget)
            {

                // GD.Print(surroundingGridPositions.Count, " ", validTarget);
                return false;
            }

            Vector2 closestPos = surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).First();

            int moveCost = Math.Max(0, GridMoveCost(characterGridPos, closestPos));
            // GD.Print(moveCost, " ", MeleeRangedCastAPCost());
            int totalCost = moveCost + MeleeRangedCastAPCost();

            if (validTarget && CanAfford(totalCost))
            {
                return true;
            }
            // GD.Print("moveCost: ", moveCost);
            // GD.Print("MeleRangeCost: ", MeleeRangedCastAPCost());
            // GD.Print("totalCost: ", totalCost);
            // GD.Print("Crrent AP: ", Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]);
        }
        // else is neighbour
        else
        {
            if (validTarget && CanAfford(MeleeRangedCastAPCost()))
            {
                return true;
            }
        }

        return false;
    }


    // public bool IsValidMelee(Vector2 targetGridPos, Vector2 characterGridPos)
    // {
    //     // string debugOutput = "";
    //     CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
    //     bool printDebug = controlledCharacter.CharacterData.Berserk;

    //     Vector2 closestPos = new();
    //     CharacterUnit targetCharacter = Battler.CharacterAtGridPos(targetGridPos);

    //     if (targetCharacter == null)
    //     {
    //         // if (printDebug) { PrintDebugTest(debugOutput); }
    //         return false;
    //     }

    //     // debugOutput += "targetCharacter : " + targetCharacter.CharacterData.Name.ToString() + ", ";

    //     // debugOutput += "controlledCharacter : " + controlledCharacter.CharacterData.Name.ToString() + ", ";
    //     // debugOutput += "controlledCharacter.Berserk: " + controlledCharacter.CharacterData.Berserk + ", ";
    //     // debugOutput += "targetCharacter.StatusToPlayer: " + targetCharacter.StatusToPlayer + ", ";

    //     bool validTarget = controlledCharacter.CharacterData.Berserk ?
    //                        controlledCharacter.BerserkValidEnemyTargets.Contains(targetCharacter.StatusToPlayer) :
    //                        controlledCharacter.ValidEnemyTargets.Contains(targetCharacter.StatusToPlayer);

    //     // debugOutput += "validTarget: " + validTarget + ", ";

    //     if (!IsNeighbour(characterGridPos, targetGridPos))
    //     {
    //         Hexagon hex = Battler.BattleGrid.GetHexAtGridPosition(targetGridPos);
    //         List<Vector2> surroundingGridPositions = Battler.BattleGrid.HexNavigation.GetNeighbouringGridPositions(hex);
    //         surroundingGridPositions.RemoveAll(pos => !IsValidGridMove(characterGridPos, pos));

    //         // debugOutput += "surroundingGridPositions: " + string.Join(", ", surroundingGridPositions) + ", ";

    //         if (surroundingGridPositions.Count == 0 || !validTarget)
    //         {
    //             // if (printDebug) { PrintDebugTest(debugOutput); }
    //             return false;
    //         }

    //         closestPos = surroundingGridPositions.OrderBy(x => GridMoveCost(characterGridPos, x)).ToList()[0];
    //         // debugOutput += "closestPos: " + closestPos.ToString() + ", ";
    //     }

    //     int moveCost = GridMoveCost(characterGridPos, closestPos);
    //     moveCost = (moveCost == -1) ? 0 : moveCost;
    //     // debugOutput += "moveCost: " + moveCost + ", ";
    //     // debugOutput += "MeleeRangedCastAPCost(): " + MeleeRangedCastAPCost() + ", ";

    //     if (validTarget && CanAfford(moveCost + MeleeRangedCastAPCost()))
    //     {
    //         // if (printDebug) { PrintDebugTest(debugOutput); }
    //         return true;
    //     }

    //     // if (printDebug) { PrintDebugTest(debugOutput); }
    //     return false;
    // }


    // private void PrintDebugTest(string s)
    // {
    //     GD.Print(s);
    // }

    // if (GridMoveCost(characterGridPos, kv.Key) > 0 && GridMoveCost(characterGridPos, kv.Key) <=
    //     Battler.CharactersAwaitingTurn[0].CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] - MeleeRangedCastCost())
    public bool IsValidRanged(Vector2 targetGridPos, int range)
    {
        CharacterUnit targetCharacter = Battler.CharacterAtGridPos(targetGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        Vector2 targetWorldPos = Battler.BattleGrid.GridToWorld(targetGridPos);
        if (IsValidRangedTarget(targetGridPos))
        {
            if (CanAfford(MeleeRangedCastAPCost()))
            {
                if (WithinShootingRange(targetCharacter, range))
                {
                    if (!Battler.BattleGrid.IsLOSBlocked(controlledCharacter.GlobalPosition, targetWorldPos))
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
        if (controlledCharacter.CharacterData.Berserk ? controlledCharacter.BerserkValidEnemyTargets.Contains(targetCharacter.StatusToPlayer) : controlledCharacter.ValidEnemyTargets.Contains(targetCharacter.StatusToPlayer))
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

    public bool IsValidSpell(Vector2 targetGridPos, SpellEffectManager.SpellMode spell)
    {
        if (spell == SpellEffectManager.SpellMode.None)
        {
            return false;
        }
        Vector2 targetWorldPos = Battler.BattleGrid.GridToWorld(targetGridPos);
        CharacterUnit controlledCharacter = Battler.CharactersAwaitingTurn[0];
        if (IsValidSpellTarget(targetGridPos, spell))
        {
            if (TargetWithinRange(Battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, targetWorldPos), Battler.AllSpells[spell].Range))
            {
                if (!Battler.BattleGrid.IsLOSBlocked(controlledCharacter.GlobalPosition, targetWorldPos))
                {
                    SpellEffectManager.Spell.PatronMode patron = Battler.AllSpells[spell].Patron;
                    if ((patron == SpellEffectManager.Spell.PatronMode.Shamash && CanAffordChargeCost(Battler.AllSpells[spell])) || (patron == SpellEffectManager.Spell.PatronMode.Ishtar && CanAffordReagentCost(Battler.AllSpells[spell])))
                    {
                        if (CanAfford(MeleeRangedCastAPCost()))
                        {
                            return true;
                        }
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
        Battler.EmitSignal(Battler.SignalName.LogBattleText,
            string.Format("{0} ends their turn early.", Battler.CharactersAwaitingTurn[0].CharacterData.Name),
            true);
        Battler.CharactersAwaitingTurn[0].BattleSkipOrder();
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
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
                moveCost, controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]), true);
            Battler.CharactersAwaitingTurn[0].BattleMoveOrder(moveCost, worldPath);

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
                Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} strikes {1} in melee combat.", controlledCharacter.CharacterData.Name,
                    targetCharacter.CharacterData.Name), true);
                controlledCharacter.BattleMeleeOrder(targetCharacter);
            }
            else
            {
                // if not adjacent, then first move the character to the target, THEN melee the target
                int moveCost = GridMoveCost(characterGridPos, closestPos);
                List<Vector2> worldPath = Battler.BattleGrid.GridToWorldPath(
                    Battler.BattleGrid.HexNavigation.CalculateGridPath(characterGridPos, closestPos)
                );
                Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} moved by {1} hexes. {2} action points remain.", controlledCharacter.CharacterData.Name,
                    moveCost, controlledCharacter.CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints]), true);
                Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} strikes {1} in melee combat.", controlledCharacter.CharacterData.Name,
                    targetCharacter.CharacterData.Name), true);
                controlledCharacter.BattleMoveOrder(moveCost, worldPath, targetCharacter);
            }
        }
        else if (action == Battler.ActionMode.Shoot)
        {
            var rangedWeaponEquipped = (StoryCharacterData.RangedWeaponMode)controlledCharacter.CharacterData.RangedWeaponEquipped;
            if (rangedWeaponEquipped == StoryCharacterData.RangedWeaponMode.None)
            {
                GD.Print("Error - character does not have a ranged weapon (IdleBattleState.cs DoAction)");
                return;
            }

            SpellEffectManager.Spell spell = Battler.AllSpells[SpellEffectManager.RangedWeaponSpells[rangedWeaponEquipped]];
            spell.AssociatedEffects[0].HitPenalty = IsAdjacentToEnemy(characterGridPos);
            spell.Origin = controlledCharacter.GetProjectileAttackOrigin();
            // GD.Print(controlledCharacter.GetProjectileAttackOrigin());
            spell.Destination = targetCharacter.GlobalPosition;
            spell.OriginCharacter = controlledCharacter;
            spell.TargetCharacter = targetCharacter;
            spell.AssociatedEffects[0].DamageDice = controlledCharacter.CharacterData.WeaponDiceRanged;
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} shoots {1}.", controlledCharacter.CharacterData.Name,
                targetCharacter.CharacterData.Name), true);
            controlledCharacter.BattleShootOrder(spell);
        }
        else if (action == Battler.ActionMode.Cast)
        {
            Vector2 centredWorldPos = Battler.BattleGrid.GridToWorld(targetGridPos);
            // NEEd to reset everything or it bugs out in future castings. next time use 
            SpellEffectManager.Spell spell = Battler.AllSpells[spellMode]; // Battler.CharactersAwaitingTurn[0].UISelectedSpell
            // GD.Print(controlledCharacter.GetProjectileAttackOrigin());
            spell.Origin = controlledCharacter.GetProjectileAttackOrigin();
            spell.Destination = centredWorldPos;
            spell.OriginCharacter = controlledCharacter;
            spell.TargetCharacter = targetCharacter;
            spell.HexDistance = Battler.BattleGrid.GetHexDistanceByWorld(controlledCharacter.GlobalPosition, centredWorldPos);
            spell.Outcome = null;
            spell.AreaAffectedCharacters = new();
            Battler.EmitSignal(Battler.SignalName.LogBattleText, string.Format("{0} casts {1}.", controlledCharacter.CharacterData.Name,
                spell.Name), true);
            controlledCharacter.BattleCastOrder(spell);
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
                // GD.Print("ENDING TURN EARLY!");
                EndTurnEarly();
            }
            return;
        }
        Battler.SetState(Battler.BattleMode.Processing);
    }
}
