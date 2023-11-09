using Godot;
using System;
using System.Collections.Generic;

public partial class BattleRoller
{

    public enum RollResult { Undetermined, CriticalHit, CriticalMiss, Hit, Miss }
    private static int Roll(Random rand, int num, int sides)
    {
        if (num <= 0 || sides <= 0)
        {
            return 0;
        }
        int res = 0;
        for (int i = 0; i < num; i++)
        {
            res += rand.Next(1, sides + 1);
        }
        return res;
    }
    private static Tuple<int, RollResult> RollD20(Random rand, int criticalThreshold)
    {
        int num = Roll(rand, 1, 20);
        RollResult res = RollResult.Undetermined;
        if (num == 1)
        {
            res = RollResult.CriticalMiss;
        }
        else if (num >= criticalThreshold)
        {
            res = RollResult.CriticalHit;
        }
        return Tuple.Create(num, res);
    }
    private static bool IsHit(int attackerResult, int defenderResult)
    {
        return attackerResult >= defenderResult;
    }

    private static int DamageResult(int attackerResult, int defenderResist)
    {
        return attackerResult - defenderResist < 0 ? 0 : attackerResult - defenderResist;
    }

    public class PersuadeOutcomeInformation
    {
        public int AttackerRoll { get; set; }
        public int DefenderRoll { get; set; }
        public int AttackerPersuade { get; set; }
        public int DefenderPersuadeResist { get; set; }
        public bool PersuadeSuccess { get; set; }
    }

    public static PersuadeOutcomeInformation RollPersuade(Random rand, int attackerPersuade, int defenderPersuadeResist)
    {
        int attackerRoll = Roll(rand, 1, 20);
        int defenderRoll = Roll(rand, 1, 20);

        return new PersuadeOutcomeInformation()
        {
            AttackerRoll = attackerRoll,
            DefenderRoll = defenderRoll,
            AttackerPersuade = attackerPersuade,
            DefenderPersuadeResist = defenderPersuadeResist,
            PersuadeSuccess = attackerRoll + attackerPersuade >= defenderRoll + defenderPersuadeResist
        };

    }

    public static void Tests()
    {
        // Random rand = new();

        // RollerInput targetedPhysical = new(attackerHitModifier: 5, defenderDodgeModifier: 2, attackerDamageModifier: 3,
        //     defenderDamageResist: 5, damageDice: new() { new Tuple<int, int>(2, 4), new Tuple<int, int>(1, 6) },
        //     criticalThreshold: 19);

        // RollerOutcomeInformation res1 = CalculateAttack(rand, targetedPhysical);
        // GD.Print("phsical attack damage: ", res1.FinalDamage);

        // RollerInput targetedMagical = new(attackerHitModifier: 3, defenderDodgeModifier: 2, attackerDamageModifier: 7,
        //     defenderDamageResist: 6, damageDice: new() { new Tuple<int, int>(2, 4) },
        //     criticalThreshold: 20);

        // RollerOutcomeInformation res2 = CalculateAttack(rand, targetedMagical);
        // GD.Print("magical attack damage: ", res2.FinalDamage);

        // RollerInput targetedUndodgableMagicalMissile = new(attackerDamageModifier: 3, defenderDamageResist: 1,
        //     damageDice: new() { new Tuple<int, int>(1, 4), new Tuple<int, int>(1, 4), new Tuple<int, int>(1, 4) });

        // RollerOutcomeInformation res3 = CalculateAttack(rand, targetedUndodgableMagicalMissile);
        // GD.Print("magic missile undodgable damage: ", res3.FinalDamage);



        // RollerInput areaTargetedMagicalAttackFireball = new(hexDistance: 11, attackerHitModifier: 3,
        //     attackerDamageModifier: 7, defenderDamageResist: 6, targetGridPoint: new Vector2(1, 1),
        //     damageDice: new() { new Tuple<int, int>(3, 3) }, criticalThreshold: 20,
        //     surroundingGridPoints: new() { new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, 0), new Vector2(1, 2), new Vector2(2, 1), new Vector2(2, 2) });

        // RollerOutcomeInformation res4 = ParseAreaAttackOutcome(rand, CalculateAttack(rand, areaTargetedMagicalAttackFireball));

        // GD.Print("fireball damage: ", res4.FinalDamage);
        // GD.Print("fireball does full damage to target: ", res4.RollResult == RollResult.Hit || res4.RollResult == RollResult.CriticalHit);
        // GD.Print("fireball hits a different tile: ", res4.RollerInput.AttackType == AttackType.AreaMissed);
        // GD.Print("fireball hits this tile: ", res4.RollerInput.TargetGridPoint);
        // GD.Print("fireball CRITICALLY missed, doing no damage and missing all tiles: ", res4.RollResult == RollResult.CriticalMiss); // area attacks should check critical miss explicitly
    }

    private static RollerOutcomeInformation ParseAreaAttackOutcome(Random rand, RollerOutcomeInformation areaAttackOutcome)
    {
        if (areaAttackOutcome.RollResult == RollResult.Miss)
        {
            areaAttackOutcome.RollerInput.AttackType = AttackType.AreaMissed;
            areaAttackOutcome.RollerInput.TargetGridPoint = areaAttackOutcome.RollerInput.SurroundingGridPoints[rand.Next(0, areaAttackOutcome.RollerInput.SurroundingGridPoints.Count - 1)];
        }
        return areaAttackOutcome;
    }

    public enum AttackType { Normal, Area, Undodgeable, AreaMissed, AreaConfirmed, AreaConfirmedHalfDamage }

    private static bool CalculateHitOutcome(RollResult rollResult, int attackerResult, int defenderResult, AttackType attackType, int hexDistance)
    {
        if (attackType == AttackType.Undodgeable || rollResult == RollResult.CriticalHit)
        {
            return true;
        }
        if (rollResult == RollResult.CriticalMiss)
        {
            return false;
        }
        if (attackType == AttackType.Area)
        {
            return IsHit(attackerResult, hexDistance);
        }
        else
        {
            return IsHit(attackerResult, defenderResult);
        }

    }

    public static RollerOutcomeInformation CalculateAreaHitOutcome(Random rand, RollerInput rollerInput)
    {
        Godot.Collections.Array<Vector2> area = new();

        Tuple<int, RollResult> attackerRoll = RollD20(rand, rollerInput.CriticalThreshold);
        RollerOutcomeInformation outcome = new()
        {
            AttackerD20Roll = attackerRoll.Item1,
            RollerInput = rollerInput,
            RollResult = attackerRoll.Item2
        };
        outcome.IsHit = CalculateHitOutcome(outcome.RollResult, outcome.AttackerD20Roll + rollerInput.AttackerHitModifier,
            outcome.DefenderD20Roll + rollerInput.DefenderDodgeModifier, rollerInput.AttackType, rollerInput.HexDistance);

        if (outcome.RollResult == RollResult.Undetermined) // i.e. not critical hit/miss
        {
            outcome.RollResult = outcome.IsHit ? RollResult.Hit : RollResult.Miss;
        }
        // GD.Print("to hit: ", outcome.AttackerD20Roll + rollerInput.AttackerHitModifier);
        // GD.Print(" vs DC ", rollerInput.HexDistance);
        // GD.Print(outcome.RollResult);

        return outcome;

        // if CriticalMiss - do not play finish, just queue free out of screen. 
        // If Miss, recalculate area. With the results, need to then modify outcome with the target and the full damage if centre of hex, or half damage if not
        // and then send into calculate single end area attack
    }

    public static RollerOutcomeInformation CalculateAttack(Random rand, RollerInput rollerInput, RollerOutcomeInformation preexistingOutcome = null)
    {
        RollerOutcomeInformation outcome = new();
        if (preexistingOutcome != null)
        {
            outcome = preexistingOutcome;
            // GD.Print(3, rollerInput.AttackType);
        }
        else
        {
            Tuple<int, RollResult> attackerRoll = RollD20(rand, rollerInput.CriticalThreshold);
            outcome.AttackerD20Roll = attackerRoll.Item1;
            outcome.RollerInput = rollerInput;
            outcome.RollResult = attackerRoll.Item2;

            outcome.DefenderD20Roll = RollD20(rand, rollerInput.CriticalThreshold).Item1;

            outcome.IsHit = CalculateHitOutcome(outcome.RollResult, outcome.AttackerD20Roll + rollerInput.AttackerHitModifier,
                outcome.DefenderD20Roll + rollerInput.DefenderDodgeModifier, rollerInput.AttackType, rollerInput.HexDistance);


            if (outcome.RollResult == RollResult.Undetermined) // i.e. not critical hit/miss
            {
                outcome.RollResult = outcome.IsHit ? RollResult.Hit : RollResult.Miss;
            }
        }
        // GD.Print(4, rollerInput.AttackType);

        if (outcome.IsHit || rollerInput.AttackType == AttackType.Area || rollerInput.AttackType == AttackType.AreaConfirmed || rollerInput.AttackType == AttackType.AreaConfirmedHalfDamage)
        {
            int attackerDamageRoll = 0;
            foreach (Tuple<int, int> damageDie in rollerInput.DamageDice)
            {
                int dieResult = Roll(rand, damageDie.Item1, damageDie.Item2);
                outcome.AttackerDamageRolls.Add(dieResult);
                attackerDamageRoll += dieResult;
            }
            if (outcome.RollResult == RollResult.CriticalHit && rollerInput.AttackType != AttackType.Undodgeable) // "undodgable" attacks cannot crit
            {
                attackerDamageRoll *= 2;
            }
            // GD.Print("damage!", attackerDamageRoll);
            if (rollerInput.AttackType == AttackType.AreaConfirmedHalfDamage)
            {
                attackerDamageRoll /= 2;
                // GD.Print("half damage!", attackerDamageRoll);
            }

            outcome.FinalDamage = DamageResult(attackerDamageRoll + rollerInput.AttackerDamageModifier, rollerInput.DefenderDamageResist);
            // GD.Print(string.Format("oa damage! {0} = {1} + {2} modded by {3}", outcome.FinalDamage, attackerDamageRoll, rollerInput.AttackerDamageModifier, rollerInput.DefenderDamageResist));
        }

        return outcome;
    }

    public partial class RollerOutcomeInformation : RefCounted
    {
        public RollerInput RollerInput { get; set; }
        public RollResult RollResult { get; set; }
        public int AttackerD20Roll { get; set; }
        public int DefenderD20Roll { get; set; }
        public bool IsHit { get; set; }
        public List<int> AttackerDamageRolls = new();
        public int FinalDamage { get; set; }
        public static void DebugPrint(RollerOutcomeInformation outcome)
        {
            RollerInput.DebugPrint(outcome.RollerInput);
            GD.Print("Roll result: ", outcome.RollResult);
            GD.Print("AttackerD20Roll: ", outcome.AttackerD20Roll);
            GD.Print("DefenderD20Roll: ", outcome.DefenderD20Roll);
            GD.Print("IsHit: ", outcome.IsHit);
            foreach (int damageRoll in outcome.AttackerDamageRolls)
            {
                GD.Print("Attacker damage roll: ", damageRoll);
            }
            GD.Print("FinalDamage: ", outcome.FinalDamage);
        }
    }
    public partial class RollerInput : RefCounted
    {
        public int AttackerHitModifier { get; set; }
        public int DefenderDodgeModifier { get; set; }
        public int AttackerDamageModifier { get; set; }
        public int DefenderDamageResist { get; set; }
        public int HexDistance { get; set; }
        // The area is the count of the surroudning grid points, allowing this to be recalculated if needed
        public List<Vector2> SurroundingGridPoints { get; set; } = new();
        public Vector2 TargetGridPoint { get; set; }
        public List<Tuple<int, int>> DamageDice { get; set; } = new();
        public BattleRoller.AttackType AttackType { get; set; }
        public int CriticalThreshold { get; set; }

        public RollerInput()
        {

        }

        // public RollerInput(int attackerHitModifier, int defenderDodgeModifier, int attackerDamageModifier, int defenderDamageResist, List<Tuple<int, int>> damageDice, int criticalThreshold, AttackType attackType = AttackType.Normal)
        // {
        //     SetTargeted(attackerHitModifier, defenderDodgeModifier, attackerDamageModifier, defenderDamageResist, damageDice, criticalThreshold);
        // }

        // public RollerInput(int attackerDamageModifier, List<Tuple<int, int>> damageDice, int defenderDamageResist)
        // {
        //     SetUndodgable(attackerDamageModifier, damageDice, defenderDamageResist);
        // }

        // public RollerInput(int attackerHitModifier, int attackerDamageModifier, int defenderDamageResist, List<Tuple<int, int>> damageDice, int hexDistance, Vector2 targetGridPoint, List<Vector2> surroundingGridPoints, int criticalThreshold)
        // {
        //     SetArea(attackerHitModifier, attackerDamageModifier, defenderDamageResist, damageDice, hexDistance, targetGridPoint, surroundingGridPoints, criticalThreshold);
        // }

        // private void SetTargeted(int attackerHitModifier, int defenderDodgeModifier, int attackerDamageModifier, int defenderDamageResist, List<Tuple<int, int>> damageDice, int criticalThreshold, AttackType attackType = AttackType.Normal)
        // {
        //     AttackerHitModifier = attackerHitModifier;
        //     DefenderDodgeModifier = defenderDodgeModifier;
        //     AttackerDamageModifier = attackerDamageModifier;
        //     DefenderDamageResist = defenderDamageResist;
        //     DamageDice = damageDice;
        //     AttackType = attackType;
        //     CriticalThreshold = criticalThreshold;
        // }
        // private void SetUndodgable(int attackerDamageModifier, List<Tuple<int, int>> damageDice, int defenderDamageResist)
        // {
        //     AttackerDamageModifier = attackerDamageModifier;
        //     DamageDice = damageDice;
        //     DefenderDamageResist = defenderDamageResist;
        //     AttackType = AttackType.Undodgeable;
        // }
        // private void SetArea(int attackerHitModifier, int attackerDamageModifier, int defenderDamageResist, List<Tuple<int, int>> damageDice, int hexDistance, Vector2 targetGridPoint, List<Vector2> surroundingGridPoints, int criticalThreshold)
        // {
        //     AttackerHitModifier = attackerHitModifier;
        //     AttackerDamageModifier = attackerDamageModifier;
        //     DefenderDamageResist = defenderDamageResist;
        //     DamageDice = damageDice;
        //     AttackType = AttackType.Area;
        //     HexDistance = hexDistance;
        //     TargetGridPoint = targetGridPoint;
        //     SurroundingGridPoints = surroundingGridPoints;
        //     CriticalThreshold = criticalThreshold;
        // }

        public static void DebugPrint(RollerInput rollerInput)
        {

            GD.Print("AttackerHitModifier: ", rollerInput.AttackerHitModifier);
            GD.Print("DefenderDodgeModifier: ", rollerInput.DefenderDodgeModifier);
            GD.Print("AttackerDamageModifier: ", rollerInput.AttackerDamageModifier);
            GD.Print("DefenderDamageResist: ", rollerInput.DefenderDamageResist);
            GD.Print("HexDistance: ", rollerInput.HexDistance);
            GD.Print("Surrounding grid points: ");
            foreach (Vector2 vec in rollerInput.SurroundingGridPoints)
            {
                GD.Print(vec);
            }

            GD.Print("Target grid point: ", rollerInput.TargetGridPoint);
            GD.Print("Damage dice: ");
            foreach (Tuple<int, int> die in rollerInput.DamageDice)
            {
                GD.Print(die.Item1 + "d" + die.Item2);
            }

            GD.Print("Attack type: ", rollerInput.AttackType);
            GD.Print("CriticalThreshold: ", rollerInput.CriticalThreshold);
        }
    }

}
