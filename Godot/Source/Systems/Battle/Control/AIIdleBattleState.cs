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
    }

    // private Random _rand = new();
    public AIIdleBattleState(IdleBattleState idleBattleState)
    {
        this.IdleBattleState = idleBattleState;
    }

    public override void ProcessUpdate(double delta)
    {
        IdleBattleState.EndTurnEarly(); // until happy with player


        // AIAction nextAction = ChooseAction();
        // this.IdleBattleState.DoAction(nextAction.Action, nextAction.TargetGridPos, nextAction.Spell);
    }

    private AIAction ChooseAction()
    {
        // var result = new Tuple<Battler.ActionMode, Vector2>(Battler.ActionMode.None, Vector2.Zero);
        Battler battler = IdleBattleState.Battler;
        Vector2 characterWorldPos = battler.CharactersAwaitingTurn[0].GlobalPosition;
        Random rand = battler.CharactersAwaitingTurn[0].Rand;
        Vector2 characterGridPos = battler.BattleGrid.WorldToGrid(characterWorldPos);
        Vector2 mouseGridPos = battler.BattleGrid.WorldToGrid(battler.GetGlobalMousePosition());
        StoryCharacterData activeData = GetActiveCharacterData();
        List<SpellEffectManager.Spell> knownSpells = activeData.KnownSpells
            .Select(spellMode => battler.AllSpells.ContainsKey(spellMode) ? battler.AllSpells[spellMode] : null)
            .Where(spell => spell != null)
            .OrderBy(_ => rand.Next())
            .ToList();
        foreach (Vector2 gridPos in GetAllGridPositions())
        {
            // 1. AOE SPELLS
            SpellEffectManager.SpellMode spellSelected = IsGoodAOESpellTarget(knownSpells, gridPos);
            if (spellSelected == SpellEffectManager.SpellMode.None)
            {
                // 2. HOSTILE SPELLS
                spellSelected = IsGoodSpellTarget(knownSpells, gridPos, SpellEffectManager.Spell.TargetMode.Enemy);
            }
            if (spellSelected == SpellEffectManager.SpellMode.None)
            {
                // 3. ALLIED SPELLS
                spellSelected = IsGoodSpellTarget(knownSpells, gridPos, SpellEffectManager.Spell.TargetMode.Ally);
            }
            if (spellSelected != SpellEffectManager.SpellMode.None)
            {
                return new(Battler.ActionMode.Cast, gridPos, spellSelected);
            }

            // 4. Adjacent to an enemy || Stronger melee than ranged
            if (IsAdjacentMelee(gridPos, characterGridPos) || IsStrongMelee(gridPos, activeData, characterGridPos))
            {
                return new(Battler.ActionMode.Melee, gridPos, SpellEffectManager.SpellMode.None);
            }

            if (IsValidRanged(gridPos))
            {
                return new(Battler.ActionMode.Shoot, gridPos, SpellEffectManager.SpellMode.None);
            }
        }
        Vector2 bestHalfMovePos = IsValidMove(characterGridPos, GetAllGridPositions(),
            GetActiveCharacterData().Stats[StoryCharacterData.StatMode.ActionPoints] - IdleBattleState.MeleeRangedCastAPCost() < 0 ? 0 :
             GetActiveCharacterData().Stats[StoryCharacterData.StatMode.ActionPoints] - IdleBattleState.MeleeRangedCastAPCost());

        if (bestHalfMovePos != new Vector2(-9999, -9999))
        {
            return new(Battler.ActionMode.Move, bestHalfMovePos, SpellEffectManager.SpellMode.None);
        }

        Vector2 bestRemainingMovePos = IsValidMove(characterGridPos, GetAllGridPositions(),
            GetActiveCharacterData().Stats[StoryCharacterData.StatMode.ActionPoints]);


        return new(bestRemainingMovePos != new Vector2(-9999, -9999) ? Battler.ActionMode.Move : Battler.ActionMode.None, bestRemainingMovePos, SpellEffectManager.SpellMode.None);

    }

    private bool IsStrongMelee(Vector2 gridPos, StoryCharacterData data, Vector2 characterGridPos)
    {
        return (data.GetAverageWeaponDiceDamage(data.WeaponDiceMelee[0]) > data.GetAverageWeaponDiceDamage(data.WeaponDiceRanged[0])) &&
            IdleBattleState.IsValidMelee(gridPos, characterGridPos);
    }

    private bool IsAdjacentMelee(Vector2 gridPos, Vector2 characterGridPos)
    {
        if (IdleBattleState.IsNeighbour(characterGridPos, gridPos) && IdleBattleState.IsValidMelee(gridPos, characterGridPos))
        {
            return true;
        }

        return false;
    }

    private bool IsValidRanged(Vector2 gridPos)
    {
        Battler battler = IdleBattleState.Battler;
        return IdleBattleState.IsValidRanged(gridPos, battler.AllSpells[SpellEffectManager.SpellMode.Arrow].Range);
    }

    private Vector2 IsValidMove(Vector2 originGridPos, List<Vector2> allGridPos, int ap)
    {
        Vector2 result = new(-9999, -9999);
        if (ap == 0)
        {
            return result;
        }
        List<Vector2> validGridMoves = new();
        foreach (Vector2 vec in allGridPos)
        {
            if (IdleBattleState.IsValidGridMoveSpecificAP(originGridPos, vec, ap))
            {
                validGridMoves.Add(vec);
            }
        }
        if (validGridMoves.Count > 0)
        {
            return ClosestToEnemy(validGridMoves);
        }

        return result;
    }

    private Vector2 ClosestToEnemy(List<Vector2> validMoves)
    {
        Battler battler = IdleBattleState.Battler;
        Vector2 closestEnemyPosition = new Vector2(-9999, -9999);

        foreach (Vector2 pos in validMoves)
        {
            List<Vector2> enemyPositions = battler.AllCharacters
                .Where(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player || x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Allied)
                .Select(x => battler.BattleGrid.WorldToGrid(x.GlobalPosition))
                .OrderBy(x => x.DistanceSquaredTo(pos))
                .ToList();

            if (enemyPositions.Count > 0)
            {
                Vector2 closestEnemy = enemyPositions[0];

                if (closestEnemyPosition == new Vector2(-9999, -9999) || closestEnemy.DistanceSquaredTo(pos) < closestEnemyPosition.DistanceSquaredTo(pos))
                {
                    closestEnemyPosition = closestEnemy;
                }
            }
        }

        return closestEnemyPosition;
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
        foreach (SpellEffectManager.Spell spell in knownSpells.Where(x => x.Target == SpellEffectManager.Spell.TargetMode.Ground))
        {
            if (CanAOESpellAffectMoreThanOneTarget(spell, gridPos))
            {
                if (IdleBattleState.IsValidSpell(gridPos, spell.SpellMode))
                {
                    if (IdleBattleState.Battler.GetAffectedAreaSpellCharacters(spell, gridPos).All(x => x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player || x.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Allied))
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