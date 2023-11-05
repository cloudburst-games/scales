using Godot;
using System.Linq;
using System.Collections.Generic;

public class StartingBattleState : BattleState
{
    private bool _UIStuffDone = false;
    private bool _turnOrderComputed = false;
    public StartingBattleState(Battler battler)
    {
        this.Battler = battler;

        RationaliseCharacterPositions();
    }

    private void RationaliseCharacterPositions()
    {
        if (!CharactersSharingHexes())
        {
            AnimateCharacterMovementToEndPositions();
            return;
        }
        List<Hexagon> occupiedHexes = new();
        List<CharacterUnit> charactersToMove = new();
        foreach (CharacterUnit characterUnit in Battler.AllCharacters)
        {
            Hexagon characterHex = Battler.BattleGrid.GetHexAtWorldPosition(characterUnit.BattleTargetPosition);
            if (occupiedHexes.Contains(characterHex))
            {
                charactersToMove.Add(characterUnit);
            }
            occupiedHexes.Add(characterHex);
        }

        foreach (CharacterUnit characterUnit in charactersToMove)
        {
            Hexagon characterHex = Battler.BattleGrid.GetHexAtWorldPosition(characterUnit.BattleTargetPosition);

            Hexagon nearestFreeHex = Battler.BattleGrid.GetFreeNeighbouringHexByArea(
                characterHex,
                occupiedHexes,
                new()
            );
            characterUnit.BattleTargetPosition = Battler.BattleGrid.GetHexWorldPosition(nearestFreeHex);
        }

        RationaliseCharacterPositions();        
    }

    private bool CharactersSharingHexes()
    {
        foreach (CharacterUnit characterUnit in Battler.AllCharacters)
        {
            Hexagon characterHex = Battler.BattleGrid.GetHexAtWorldPosition(
                Battler.BattleGrid.GetCorrectedWorldPosition(characterUnit.BattleTargetPosition));
            foreach (CharacterUnit comparisonUnit in Battler.AllCharacters)
            {
                if (comparisonUnit == characterUnit)
                {
                    continue;
                }
                Hexagon comparisonHex = Battler.BattleGrid.GetHexAtWorldPosition(
                    Battler.BattleGrid.GetCorrectedWorldPosition(comparisonUnit.BattleTargetPosition));
                if (characterHex == comparisonHex)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void AnimateCharacterMovementToEndPositions()
    {
        foreach (CharacterUnit characterUnit in Battler.AllCharacters)
        {
            characterUnit.SetActionState(CharacterUnit.ActionMode.EnteringBattle);
        }

        ComputeTurnOrder();
        DoUIStuff();
        EndStartingStateWhenReady();
    }

    private async void DoUIStuff()
    {
        Battler.PlayAnim("Start");
        await Battler.ToSignal(Battler, Battler.SignalName.AnimationFinished);
        _UIStuffDone = true;
    }

    public override void ComputeTurnOrder()
    {
        base.ComputeTurnOrder(); // ordered in the parent class
        _turnOrderComputed = true;
    }

    private async void EndStartingStateWhenReady()
    {
        // if still in the entering battle phase (being animated to the start position)
        while (Battler.AllCharacters.Any(x => x.GetActionState() == CharacterUnit.ActionMode.EnteringBattle) ||
        !_UIStuffDone || !_turnOrderComputed)
        {
            await Battler.ToSignal(Battler.GetTree(), "process_frame");
        }
        Battler.SetState(Battler.BattleMode.Idle);
    }

    public override void ProcessUpdate(double delta)
    {
        base.ProcessUpdate(delta);
        // GD.Print(Battler.AllCharacters.Any(x => x.GetActionState() == CharacterUnit.ActionMode.EnteringBattle));
    }
}
