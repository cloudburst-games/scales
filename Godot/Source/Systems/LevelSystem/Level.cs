using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Level : Node2D
{
    public int LevelID { get; set; } = -1;
    private Selection _selection;

    private TileMap _terrain;

    private HexGrid _hexGrid;
    private HexModifier _hexModifier;
    private HexTilemapIsometricInterface _hexTileInterface;
    [Signal]
    public delegate void BattleCommencedEventHandler(Godot.Collections.Array<CharacterUnit> characterUnits, HexGrid hexGrid);

    [Signal]
    public delegate void CharacterClickedEventHandler(CharacterUnit characterUnit, bool shift);

    public override void _Ready()
    {
        RefNodes();
        InitSelectionHelper();
        InitHexGrid();
        UpdateNavRegionShapes();
        InitCharacters();
    }

    private void RefNodes()
    {
        _selection = GetNode<Selection>("Selection");
        _terrain = GetNode<TileMap>("Terrain/Tilemaps/Level1");
        _hexGrid = GetNode<HexGrid>("HexGrid");
        _hexModifier = GetNode<HexModifier>("HexGrid/HexModifier");
        // _battler = GetNode<Battler>("Battler");
    }

    private void InitSelectionHelper()
    {
        _selection.MoveTargetSelected += (CharacterUnit cUnit, Vector2 targetWorldPos) => this.OnMoveTargetSelected(cUnit, targetWorldPos);
        _selection.SetState(Selection.SelectionMode.Adventure);
    }

    private void InitCharacters()
    {
        foreach (CharacterUnit cUnit in GetAllCharacters())
        {
            cUnit.CharacterClicked += (playerCharacter, shift) => _selection.OnPlayerCharacterClicked(playerCharacter, shift);
            // cUnit.EndTargetChanged += playerCharacter => this.OnPlayerEndTargetChanged(playerCharacter);
            cUnit.RemoveObstacle += (playerCharacter, moving) =>
                _hexModifier.OnCharacterRemoveObstacle(playerCharacter.GlobalPosition, moving);
            cUnit.SetActionState(CharacterUnit.ActionMode.Idle);
            // _hexModifier.OnCharacterActionStateChanged(cUnit.GlobalPosition, (int) CharacterUnit.ActionMode.Idle, true);
        }
        _selection.SetPlayerCharacters(GetPlayerCharacters());
    }

    private List<CharacterUnit> GetAllCharacters()
    {
        List<CharacterUnit> result = new();
        foreach (Node n in GetNode("Entities/CharacterUnits").GetChildren())
        {
            if (n is CharacterUnit cUnit)
            {
                result.Add(cUnit);
            }
        }
        return result;
    }

    private List<CharacterUnit> GetPlayerCharacters()
    {
        List<CharacterUnit> result = new();
        foreach (CharacterUnit cUnit in GetAllCharacters())
        {
            if (cUnit.GetControlState() == CharacterUnit.ControlMode.Player)
            {
                result.Add(cUnit);
            }
        }
        return result;
    }

    // this runs after right clicking to select a target hex
    private void OnMoveTargetSelected(CharacterUnit player, Vector2 targetWorldPos)
    {
        // GD.Print("move order given");

        // ignore if clicking where the player is:
        // if (_hexGrid.GetCorrectedWorldPosition(targetWorldPos) == _hexGrid.GetCorrectedWorldPosition(player.GlobalPosition))
        // {
        //     return;
        // }

        // check for occupied hexes for future calculations
        List<Hexagon> occupiedHexes = new();
        foreach (CharacterUnit cUnit in GetAllCharacters())
        {
            occupiedHexes.Add(_hexGrid.GetHexAtWorldPosition(cUnit.GlobalPosition));
        }

        // move player to nearest free hex IF player is on an obstacle hex
        // Hexagon hexAtPlayer = _hexGrid.GetHexAtWorldPosition(player.GlobalPosition);
        // if (hexAtPlayer.Obstacle)
        // {
        //     Hexagon nearestFreeHex = _hexGrid.GetFreeNeighbouringHexByLine(player.GlobalPosition, player.EndTarget, occupiedHexes, false);
        //     player.GlobalPosition = _hexGrid.GetHexWorldPosition(nearestFreeHex);
        // }

        // GD.Print(_hexGrid.GetCorrectedWorldPosition(targetWorldPos));
        // GD.Print(_hexGrid.GetCorrectedWorldPosition(player.GlobalPosition));
        // adjust end target according to formation. 
        // TODO - when formation UI is implemented, take the orientation of the mouse click, 
        // and then find the hex from 1 to x in that orientation
        // and then find the neighbouring unoccupied hex if it is occupied or already a target
        // (will need to store a record of hex targets)
        switch (player.FormationPosition)
        {
            case 1:
                break;
            case 2:
                targetWorldPos += new Vector2(100, 0);
                break;
            case 3:
                targetWorldPos += new Vector2(-100, 0);
                break;
            case 4:
                targetWorldPos += new Vector2(100, 100);
                break;
            case 5:
                targetWorldPos += new Vector2(-100, -100);
                break;
            default:
                break;
        }

        // check if the target is on an obstacle hex. if so, adjust it to the nearest non-obstacle hex
        // first correct to a valid position
        targetWorldPos = _hexGrid.GetCorrectedWorldPosition(targetWorldPos);
        Hexagon hexAtEndTarget = _hexGrid.GetHexAtWorldPosition(targetWorldPos);
        if (hexAtEndTarget.Obstacle || occupiedHexes.Contains(hexAtEndTarget))
        {
            Hexagon nearestFreeHex = _hexGrid.GetFreeNeighbouringHexByLine(player.GlobalPosition, targetWorldPos, occupiedHexes);
            targetWorldPos = _hexGrid.GetHexWorldPosition(nearestFreeHex);
        }

        if (_hexGrid.GetCorrectedWorldPosition(targetWorldPos) == _hexGrid.GetCorrectedWorldPosition(player.GlobalPosition))
        {
            player.NavAgent.TargetPosition = player.GlobalPosition;
            return;
        }

        // GD.Print("nearest: ", _hexGrid.HexNavigation.GetNearestNonObstacleGridNeighbour(_hexGrid.WorldToGrid(player.GlobalPosition),
        //     player.EndTarget));

        // if (_hexGrid.WorldToGrid(player.EndTarget) != _hexGrid.WorldToGrid(player.GlobalPosition) &&
        //     ! _hexGrid.GetHexAtGridPosition(_hexGrid.WorldToGrid(player.EndTarget)).Obstacle)
        // {
        // SOMETIMES THE END COORD IS DIFFERENT- BECAUSE OF FORMATION

        player.NavAgent.TargetPosition = targetWorldPos;

        // player.CurrentPath = _hexGrid.GridToWorldPath(
        // _hexGrid.HexNavigation.CalculateGridPath(
        // _hexGrid.WorldToGrid(player.GlobalPosition), _hexGrid.WorldToGrid(player.EndTarget)));
        // }
        // else
        // {
        //     _hexModifier.OnCharacterActionStateChanged(player.GlobalPosition, (int)CharacterUnit.ActionMode.Idle, true);
        // }

        // foreach (Vector2 vec in player.CurrentPath)
        // {
        //     GD.Print(vec);
        // }
    }

    private void UpdateNavRegionShapes()
    {
        List<CollisionPolygon2D> shapes = BaseProject.Utils.Node.GetNodesRecursive(new List<CollisionPolygon2D>(), GetNode("Entities"));
        GetNode<NavRegion>("Navigation/NavRegion").UpdateCollisionShapes(LevelID, shapes);

    }

    private void InitHexGrid()
    {
        _hexTileInterface = new HexTilemapIsometricInterface(_hexGrid, _terrain);
        _hexGrid.AddChild(_hexTileInterface);
        _hexTileInterface.FinishedMarkingObstacles += _hexGrid.UpdateNavigationAndDisplay;

        _hexGrid.Start(_hexTileInterface.GetHexGridBoundsFromTilemap()[0], _hexTileInterface.GetHexGridBoundsFromTilemap()[1], HexGrid.ConstructionMode.WorldSize);

        _hexTileInterface.MarkAllHexObstacles(LevelID);
        // _hexGrid.UpdateNavigationAndDisplay(); // shouldnt need to do it twice???
        _hexModifier.Init(_hexGrid);
    }

    public void StartBattle()
    {
        _selection.SetState(Selection.SelectionMode.Battle);
        List<CharacterUnit> involvedCharacters = new();
        List<CharacterUnit> playerCharacters = GetPlayerCharacters();
        // If the character is a player, involve in the battle.
        // Otherwise, then if the character is within x hexes of the player, involve in the battle.
        foreach (CharacterUnit characterUnit in GetAllCharacters())
        {
            if (characterUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player)
            {
                involvedCharacters.Add(characterUnit);
            }
            else
            {
                foreach (CharacterUnit playerCharacter in playerCharacters)
                {
                    if (_hexGrid.GetHexDistance(playerCharacter.GlobalPosition, characterUnit.GlobalPosition) <= 10) // ?involved if within 10 hexes of a player
                    {
                        involvedCharacters.Add(characterUnit);
                        break;
                    }
                }
            }
        }
        EmitSignal(SignalName.BattleCommenced, involvedCharacters.ToArray<CharacterUnit>(), _hexGrid);
        // _battler.Init(involvedCharacters, _hexGrid);
        SetDebugText("battle started. no. characters: " + involvedCharacters.Count);
    }


    public void OnBattleEnded()
    {

        SetDebugText("battle ended");
        _selection.SetState(Selection.SelectionMode.Adventure); //??
        _selection.SetPlayerCharacters(GetPlayerCharacters());
    }

    // public void Die()
    // {
    //     _hexTileInterface.Die();
    // }


    // DEBUG methods
    public override void _Input(InputEvent ev)
    {
        if (ev.IsEcho())
        {
            return;
        }
        if (ev.IsActionPressed("ui_select"))
        {
            StartBattle();
        }
        else if (ev.IsActionPressed("ui_cancel"))
        {
            OnBattleEnded();
        }
        else if (ev.IsActionPressed("ui_accept"))
        {
            foreach (CharacterUnit characterUnit in GetPlayerCharacters())
            {
                characterUnit.GlobalPosition = new Vector2(1210, 900);
            }
        }
    }
    private void SetDebugText(string txt)
    {
        GetNode<Label>("DEBUGCANVAS/DEBUGLABEL").Text = txt;
    }
}
