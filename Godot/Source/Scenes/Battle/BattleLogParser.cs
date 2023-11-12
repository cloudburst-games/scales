using System;

public class BattleLogParser
{

    public enum InvalidReasonMode
    {
        InvalidTarget, NotEnoughAP,
        InvalidLocation,
        OutOfRange,
        NotEnoughCharge,
        NotEnoughReagents,
        NotEnoughEndurance
    }

    public static string ParseUIHint(PnlAction.UIHint hint)
    {
        string output = "";

        switch (hint)
        {
            case PnlAction.UIHint.Cast:
                output = "Cast spell.";
                break;
            case PnlAction.UIHint.ChooseAction:
                output = "Select a default action.";
                break;
            case PnlAction.UIHint.EndTurn:
                output = "End this character's turn.";
                break;
            case PnlAction.UIHint.Melee:
                output = "Melee attack.";
                break;
            case PnlAction.UIHint.Shoot:
                output = "Ranged attack.";
                break;
            case PnlAction.UIHint.Move:
                output = "Move.";
                break;
            case PnlAction.UIHint.Options:
                output = "Options.";
                break;
            case PnlAction.UIHint.ChooseSpell:
                output = "Open spellbook.";
                break;
            case PnlAction.UIHint.ExpandLog:
                output = "Expand combat log.";
                break;
            case PnlAction.UIHint.None:
                output = "";
                break;
        }
        return output;
    }

    public static string ParseAction(Battler.ActionMode action, SpellEffectManager.Spell spell, int numAffected, string originName, string targetName, InvalidReasonMode reason)
    {
        string output = "";
        switch (action)
        {
            case Battler.ActionMode.Cast:
                output = spell.Target == SpellEffectManager.Spell.TargetMode.Ground ?
                    string.Format("Cast {0}. Will affect {1} characters if hits the target.", spell.Name, numAffected) :
                    string.Format("Cast {0} on {1}", spell.Name, targetName);
                break;
            case Battler.ActionMode.Hint:
                output = string.Format("View {0} information", targetName);
                break;
            case Battler.ActionMode.Invalid:
                switch (reason)
                {
                    case InvalidReasonMode.InvalidTarget:
                        output = "Invalid target.";
                        break;
                    case InvalidReasonMode.NotEnoughAP:
                        output = "Insufficient action points.";
                        break;
                    case InvalidReasonMode.InvalidLocation:
                        output = "Invalid location.";
                        break;
                    case InvalidReasonMode.OutOfRange:
                        output = "Out of range.";
                        break;
                    case InvalidReasonMode.NotEnoughEndurance:
                        output = "Insufficient endurance.";
                        break;
                    case InvalidReasonMode.NotEnoughReagents:
                        output = "Insufficient reagents.";
                        break;
                    case InvalidReasonMode.NotEnoughCharge:
                        output = "Insufficient charge.";
                        break;
                }
                break;
            case Battler.ActionMode.Melee:
                output = string.Format("Attack {0} in melee combat.", targetName);
                break;
            case Battler.ActionMode.Move:
                output = string.Format("Move {0} here.", originName);
                break;
            case Battler.ActionMode.Shoot:
                output = string.Format("Shoot {0}", targetName);
                break;
        }

        return output;
    }

    internal static string ParseSpellHint(SpellEffectManager.Spell spell)
    {
        string output = "";

        if (spell.SpellMode != SpellEffectManager.SpellMode.None)
        {
            output = spell.Description;
            string cost = spell.ChargeCost > 0 ? "Cost: " + spell.ChargeCost.ToString() + " Charge" : spell.ReagentCost > 0 ? "Cost: " + spell.ReagentCost + " Reagents" : "";
            output += (spell.ChargeCost > 0 || spell.ReagentCost > 0) ? "\n" + cost : "";
        }

        return output;

    }
}