// i am making AI for a turn based battle game. this script is called when it is the AI's turn (entry point is ProcessUpdate)
// lets make a hierarchical AI, i.e. it ranks actions in order of desirability and if they are available, does them
// 1. cast an aoe spell if it affects more than 1 target
// 2. cast any other random spell if valid target
// 3. melee if in range and melee attack is stronger than ranged attack (get stats by doing GetActiveCharacterData.Stats eg; for melee attack power GetActiveCharacterData.Stats.MeleeAP)
// 4. melee if adjacent to an enemy
// 5. ranged if in range
// 6. melee if in range and melee attack is weaker than ranged attack (as not in range for ranged)
// 7. move towards closest enemy otherwise

using System;
using System.Collections.Generic;
using Godot;
using System.Linq;

public partial class AIIdleBattleState : ControlIdleBattleState
{

    public partial class AIAction : RefCounted
    {
        public Battler.ActionMode Action;
        public Vector2 TargetGridPos;
        public SpellEffectManager.SpellMode Spell;
        public AIAction(Battler.ActionMode action, Vector2 targetGridPos, SpellEffectManager.SpellMode spell)
        {
            Action = action;
            TargetGridPos = targetGridPos;
            Spell = spell;
        }

        public void DebugPrint()
        {
            GD.Print("Action: ", Action.ToString());
            GD.Print("Target grid position: ", TargetGridPos);
            GD.Print("Spell: ", Spell.ToString());
        }
    }

    public override void OnInitCurrentTurn()
    {
        base.OnInitCurrentTurn();
        // IdleBattleState.Battler.SetGridUserHexes(IdleBattleState.GetValidMoveHexes(), IdleBattleState.GetValidHalfMoveHexes(), HexGridUserDisplay.DisplayMode.HideAllHexes);
        IdleBattleState.Battler.ToggleGrid(false);

    }

    // private Random _rand = new();
    public AIIdleBattleState(IdleBattleState idleBattleState)
    {
        this.IdleBattleState = idleBattleState;
    }

    public override void ProcessUpdate(double delta)
    {
        base.ProcessUpdate(delta);
        Battler battler = IdleBattleState.Battler;
        Vector2 characterWorldPos = battler.CharactersAwaitingTurn[0].GlobalPosition;
        Vector2 characterGridPos = battler.BattleGrid.WorldToGrid(characterWorldPos);
        CharacterUnit activeCharacter = battler.CharactersAwaitingTurn[0];
        // StoryCharacterData activeData = GetActiveCharacterData();
        if (battler.BattleGrid.GetHexAtGridPosition(characterGridPos).Obstacle)
        {
            return;
        }
        GD.Print(activeCharacter.GetActionState());
        // if (activeData.Berserk)
        // {
        //     IdleBattleState.BerserkEffect();
        //     return;
        // }
        // GD.Print(GetActiveCharacterData().Name);
        AIAction nextAction = ChooseAction();
        if (nextAction.Action == Battler.ActionMode.None)
        {
            IdleBattleState.EndTurnEarly();
        }
        // nextAction.DebugPrint();
        this.IdleBattleState.DoAction(nextAction.Action, nextAction.TargetGridPos, nextAction.Spell);
    }

    private AIAction ChooseAction()
    {
        Battler battler = IdleBattleState.Battler;
        Vector2 characterWorldPos = battler.CharactersAwaitingTurn[0].GlobalPosition;
        Random rand = battler.CharactersAwaitingTurn[0].Rand;
        Vector2 characterGridPos = battler.BattleGrid.WorldToGrid(characterWorldPos);
        CharacterUnit activeCharacter = battler.CharactersAwaitingTurn[0];
        StoryCharacterData activeData = GetActiveCharacterData();

        List<SpellEffectManager.Spell> knownSpells = activeData.KnownSpells
            .Select(spellMode => battler.AllSpells.ContainsKey(spellMode) ? battler.AllSpells[spellMode] : null)
            .Where(spell => spell != null)
            .OrderBy(_ => rand.Next())
            .ToList();

        List<Vector2> enemyPositions = battler.AllCharacters
            // the berserk list we can change but for now it includes everyone
            .Where(x => activeData.Berserk ? activeCharacter.BerserkValidEnemyTargets.Contains(x.StatusToPlayer) : activeCharacter.ValidEnemyTargets.Contains(x.StatusToPlayer))
            .Where(x => x != activeCharacter)
            .Where(x => x.CharacterData.Alive)
            .Select(x => battler.BattleGrid.WorldToGrid(x.GlobalPosition))
            .ToList();
        List<Vector2> alliedPositions = battler.AllCharacters
            .Where(x => activeData.Berserk ? activeCharacter.BerserkValidAllyTargets.Contains(x.StatusToPlayer) : activeCharacter.ValidAllyTargets.Contains(x.StatusToPlayer))
            .Where(x => x.CharacterData.Alive)
            .Select(x => battler.BattleGrid.WorldToGrid(x.GlobalPosition))
            .ToList();


        List<Vector2> allGridPositions = GetAllGridPositions();

        // if berserk and nobody left to fight, just end turn
        if (activeData.Berserk)
        {
            if (enemyPositions.Count == 0)
            {
                return new(Battler.ActionMode.None, new Vector2(), SpellEffectManager.SpellMode.None);
            }
        }

        // get all the character grid positions
        // if berserk, dont use magic
        if (!activeData.Berserk)
        {
            foreach (Vector2 gridPos in enemyPositions)
            {
                // MAGIC
                // 1. If there is a good spell target for our AOE SPELL, do it
                SpellEffectManager.SpellMode spellSelected = IsGoodAOESpellTarget(knownSpells, gridPos);
                if (spellSelected != SpellEffectManager.SpellMode.None)
                {
                    return new(Battler.ActionMode.Cast, gridPos, spellSelected);
                }
                // 2. If there is a good spell target for our hostile spell, do it
                spellSelected = IsGoodSpellTarget(knownSpells, gridPos, SpellEffectManager.Spell.TargetMode.Enemy);
                if (spellSelected != SpellEffectManager.SpellMode.None)
                {
                    return new(Battler.ActionMode.Cast, gridPos, spellSelected);
                }
            }
            foreach (Vector2 gridPos in alliedPositions)
            {
                // 3. If there is a good spell target for our friendly spell, do it
                SpellEffectManager.SpellMode spellSelected = IsGoodSpellTarget(knownSpells, gridPos, SpellEffectManager.Spell.TargetMode.Ally);
                if (spellSelected != SpellEffectManager.SpellMode.None)
                {
                    return new(Battler.ActionMode.Cast, gridPos, spellSelected);
                }
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        // TODO SORTING OUT BERSERK TARGETS

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        foreach (Vector2 gridPos in enemyPositions)
        {
            // If we are next to the enemy, strike in melee, as ranged has a penalty to hit chance (4) and cant move back due to reduced AP
            if (IsAdjacentMelee(gridPos, characterGridPos))
            {
                return new(Battler.ActionMode.Melee, gridPos, SpellEffectManager.SpellMode.None);
            }
            // if we are not adjacent to melee, but our melee is stronger than our ranged, move in to attack
            else if (IsMeleeStrongerThanRanged(gridPos, activeData, characterGridPos))
            {
                return new(Battler.ActionMode.Melee, gridPos, SpellEffectManager.SpellMode.None);
            }
            // otherwise ranged attack if possible
            else if (IsValidRanged(gridPos) && (StoryCharacterData.RangedWeaponMode)activeData.RangedWeaponEquipped != StoryCharacterData.RangedWeaponMode.None)
            {
                return new(Battler.ActionMode.Shoot, gridPos, SpellEffectManager.SpellMode.None);
            }
            // if not possible, try for a melee attack
            else if (IdleBattleState.IsValidMelee(gridPos, characterGridPos))
            {
                return new(Battler.ActionMode.Melee, gridPos, SpellEffectManager.SpellMode.None);
            }
        }
        Vector2 bestHalfMovePos = IsValidMove(characterGridPos, allGridPositions,
            GetActiveCharacterData().Stats[StoryCharacterData.StatMode.ActionPoints] - IdleBattleState.MeleeRangedCastAPCost() < 0 ? 0 :
             GetActiveCharacterData().Stats[StoryCharacterData.StatMode.ActionPoints] - IdleBattleState.MeleeRangedCastAPCost(), enemyPositions);

        if (bestHalfMovePos != new Vector2(-9999, -9999))
        {
            // GD.Print("best half move pos is ok");
            return new(Battler.ActionMode.Move, bestHalfMovePos, SpellEffectManager.SpellMode.None);
        }

        Vector2 bestRemainingMovePos = IsValidMove(characterGridPos, allGridPositions,
            GetActiveCharacterData().Stats[StoryCharacterData.StatMode.ActionPoints], enemyPositions);

        // if (bestRemainingMovePos != new Vector2(-9999, -9999))
        //     GD.Print("best remaining move pos is ok");

        return new(bestRemainingMovePos != new Vector2(-9999, -9999) ? Battler.ActionMode.Move : Battler.ActionMode.None, bestRemainingMovePos, SpellEffectManager.SpellMode.None);

    }

    public Vector2 IsValidMove(Vector2 originGridPos, List<Vector2> allGridPos, int ap, List<Vector2> enemyPositions)
    {
        Vector2 result = new(-9999, -9999);
        if (ap == 0)
        {
            // GD.Print("returning invalid move due to zero AP");
            return result;
        }
        List<Vector2> validGridMoves = new();
        // GD.Print("originGridPos: ", originGridPos);
        // allGridPos.ForEach(x => GD.Print("grid Pos: ", x));
        // GD.Print("ap: ", ap);
        foreach (Vector2 vec in allGridPos)
        {
            // GD.Print(vec);
            if (IdleBattleState.IsValidGridMoveSpecificAP(originGridPos, vec, ap))
            {
                // GD.Print(vec);
                validGridMoves.Add(vec);
            }
        }
        if (validGridMoves.Count > 0)
        {
            return ClosestToEnemy(validGridMoves, enemyPositions);
        }

        // GD.Print("returning invalid move as no valid grid moves for current AP");
        // GD.Print("AP: ", ap);
        // GD.Print("origin grid Pos: ", originGridPos);
        // GD.Print("gridposition count: ", allGridPos.Count);
        // GD.Print("valid grid move count: ", validGridMoves.Count);

        return result;
    }

    public bool IsAdjacentMelee(Vector2 gridPos, Vector2 characterGridPos)
    {
        if (IdleBattleState.IsNeighbour(characterGridPos, gridPos) && IdleBattleState.IsValidMelee(gridPos, characterGridPos))
        {
            return true;
        }

        return false;
    }

    private Vector2 ClosestToEnemy(List<Vector2> validMoves, List<Vector2> enemyPositions)
    {
        Battler battler = IdleBattleState.Battler;
        CharacterUnit activeCharacter = battler.CharactersAwaitingTurn[0];
        Vector2 activeCharacterGridPosition = battler.BattleGrid.WorldToGrid(activeCharacter.GlobalPosition);

        // 1. Get all empty valid move hexes: validMoves
        if (validMoves.Count == 0)
        {
            // GD.Print("error here no valid moves");

            // No valid moves to consider
            return new Vector2(-9999, -9999);
        }

        // 2. Get the enemy positions

        // 3. Get which enemy hex is closest to the activeCharacterGridPosition
        Vector2 closestEnemyPosition = enemyPositions.OrderBy(x => x.DistanceSquaredTo(activeCharacterGridPosition)).FirstOrDefault();

        // GD.Print("closest enemy: ", closestEnemyPosition);

        // 4. Get which of the valid move hexes are closest to this enemy hex
        Vector2 closestValidMovePosition = validMoves.OrderBy(x => x.DistanceSquaredTo(closestEnemyPosition)).FirstOrDefault();


        // if (closestValidMovePosition == null)
        // {
        //     GD.Print("error here closest move is null");
        // }

        // 5. Return it, or return new Vector2(-9999,-9999) if the list of valid moves or the list of enemy positions is empty
        return closestValidMovePosition != null ? closestValidMovePosition : new Vector2(-9999, -9999);
    }
    private bool IsMeleeStrongerThanRanged(Vector2 gridPos, StoryCharacterData data, Vector2 characterGridPos)
    {
        return (data.GetAverageWeaponDiceDamage(data.WeaponDiceMelee[0]) > data.GetAverageWeaponDiceDamage(data.WeaponDiceRanged[0])) &&
            IdleBattleState.IsValidMelee(gridPos, characterGridPos);
    }


    private bool IsValidRanged(Vector2 gridPos)
    {
        Battler battler = IdleBattleState.Battler;
        return IdleBattleState.IsValidRanged(gridPos, battler.AllSpells[SpellEffectManager.SpellMode.Sling].Range);
    }


    private SpellEffectManager.SpellMode IsGoodSpellTarget(List<SpellEffectManager.Spell> knownSpells, Vector2 gridPos, SpellEffectManager.Spell.TargetMode targetType)
    {
        CharacterUnit target = IdleBattleState.Battler.CharacterAtGridPos(gridPos);

        if (target != null)
        {
            foreach (SpellEffectManager.SpellMode spellMode in knownSpells
                .Where(spell => spell.Target == targetType)
                .Select(spell => spell.SpellMode)
                .Where(spellMode => !target.CharacterData.CurrentEffects.Any(x => x.FromSpell == spellMode)))
            {
                if (IdleBattleState.IsValidSpell(gridPos, spellMode))
                {
                    return spellMode;
                }
            }
        }

        return SpellEffectManager.SpellMode.None;
    }


    private SpellEffectManager.SpellMode IsGoodAOESpellTarget(List<SpellEffectManager.Spell> knownSpells, Vector2 gridPos)
    {
        CharacterUnit activeCharacter = IdleBattleState.Battler.CharactersAwaitingTurn[0];
        foreach (SpellEffectManager.Spell spell in knownSpells.Where(x => x.Target == SpellEffectManager.Spell.TargetMode.Ground))
        {
            spell.TargetCharacter = IdleBattleState.Battler.CharacterAtGridPos(gridPos);
            if (CanAOESpellAffectMoreThanOneTarget(spell, gridPos))
            {
                // GD.Print("it affect more than one target");
                if (IdleBattleState.IsValidSpell(gridPos, spell.SpellMode))
                {
                    if (IdleBattleState.Battler.GetAffectedAreaSpellCharacters(spell, gridPos).All(x => activeCharacter.ValidEnemyTargets.Contains(x.StatusToPlayer)))
                    {
                        return spell.SpellMode;
                    }
                }

            }
        }
        return SpellEffectManager.SpellMode.None;
    }

    private bool CanAOESpellAffectMoreThanOneTarget(SpellEffectManager.Spell spell, Vector2 gridPos)
    {
        // GD.Print(" more than one target?? ", IdleBattleState.Battler.GetAffectedAreaSpellCharacters(spell, gridPos).Count);
        return IdleBattleState.Battler.GetAffectedAreaSpellCharacters(spell, gridPos).Count > 1;
    }


    private StoryCharacterData GetActiveCharacterData()
    {
        return IdleBattleState.Battler.CharactersAwaitingTurn[0].CharacterData;
    }

    private List<Vector2> GetAllGridPositions()
    {
        Battler battler = IdleBattleState.Battler;
        return battler.GetAllNonObstacleGridPositions()
            .Where(x => x != battler.BattleGrid.WorldToGrid(IdleBattleState.Battler.CharactersAwaitingTurn[0].GlobalPosition)).ToList();
    }
}