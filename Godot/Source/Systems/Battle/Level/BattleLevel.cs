using Godot;
using System;
using System.Collections.Generic;

public partial class BattleLevel : Node2D
{
    [Export]
    public HexGrid HexGrid;
    [Export]
    private TileMap _dummyTilemap;
    [Export]
    public HexModifier HexModifier { get; set; }
    [Export]
    private Sprite2D _background;

    [Export]
    public Node2D CharacterUnitsContainer;

    public HexTilemapIsometricInterface HexTileInterface { get; set; } // used only to get the hex grid start pos because i couldnt figure out how else to do it!
    // private HexObstacleMarkerTool _hexObstacleMarkerTool;

    [Export]
    public int LevelID { get; set; } = 0;
    [Export]
    public string IntroMessage { get; set; } = "Let battle be joined!";

    [Export]
    public Godot.Collections.Array<StoryCharacter.StoryCharacterMode> StartingEnemies = new();
    [Export]
    public Godot.Collections.Array<StoryCharacter.StoryCharacterMode> StartingAllies = new();

    [Export]
    private Node2D _playerPositionCnt;
    [Export]
    private Node2D _enemyPositionCnt;
    [Export]
    private Node2D _allyPositionCnt;

    private List<Vector2> _availablePlayerStartingPositions = new();
    private List<Vector2> _availableEnemyStartingPositions = new();
    private List<Vector2> _availableAllyStartingPositions = new();

    private Dictionary<CharacterUnit.StatusToPlayerMode, List<Vector2>> _playerStatusPositions;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        InitHexGrid();

        foreach (Marker2D marker in _playerPositionCnt.GetChildren())
        {
            _availablePlayerStartingPositions.Add(marker.Position);
        }
        foreach (Marker2D marker in _enemyPositionCnt.GetChildren())
        {
            _availableEnemyStartingPositions.Add(marker.Position);
        }
        foreach (Marker2D marker in _allyPositionCnt.GetChildren())
        {
            _availableAllyStartingPositions.Add(marker.Position);
        }
        _playerStatusPositions = new() {
            {CharacterUnit.StatusToPlayerMode.Allied, _availableAllyStartingPositions},
            {CharacterUnit.StatusToPlayerMode.Hostile, _availableEnemyStartingPositions},
            {CharacterUnit.StatusToPlayerMode.Player, _availablePlayerStartingPositions},
        };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    private void InitHexGrid()
    {
        // _hexObstacleMarkerTool = new(_hexGrid);
        // _hexGrid.AddChild(_hexObstacleMarkerTool);
        // _hexObstacleMarkerTool.FinishedMarkingObstacles += _hexGrid.UpdateNavigationAndDisplay;
        HexTileInterface = new(HexGrid, _dummyTilemap);
        HexGrid.AddChild(HexTileInterface);
        HexTileInterface.FinishedMarkingObstacles += HexGrid.UpdateNavigationAndDisplay;
        // GD.Print(_hexTileInterface.GetHexGridBoundsFromTilemap()[0]);
        // GD.Print(_hexTileInterface.GetHexGridBoundsFromTilemap()[1]);

        // _hexGrid.Start(new Vector2(-1436.841f, 1595.2328f), new Vector2(1074.8021f, 4061.621f), HexGrid.ConstructionMode.WorldSize);
        HexGrid.Start(HexTileInterface.GetHexGridBoundsFromTilemap()[0], HexTileInterface.GetHexGridBoundsFromTilemap()[1], HexGrid.ConstructionMode.WorldSize);

        // _hexObstacleMarkerTool.MarkAllHexObstacles(LevelID);
        HexTileInterface.MarkAllHexObstacles(LevelID);
        // _hexGrid.UpdateNavigationAndDisplay(); // shouldnt need to do it twice???
        HexModifier.Init(HexGrid);
    }
    private void SetCharacterPositionAndDirection(CharacterUnit characterUnit, List<Vector2> positionPool)
    {
        characterUnit.Position = positionPool[0];
        positionPool.RemoveAt(0);

        Vector2 mapCenter = _background.GlobalPosition;// + _background.Texture.GetSize() * _background.Scale * 0.5f;

        Vector2 directionToCenter = (mapCenter - characterUnit.Position).Normalized();

        List<Vector2> inwardFacingDirections = new()
        {
            new(0.47f, -0.88f),
            new(0.99f, -0.13f),
            new (0.89f, 0.45f),
            new (-0.47f, 0.88f),
            new (-0.99f, 0.13f),
            new (-0.89f, -0.45f)
        };

        Vector2 closestInwardFacingDirection = FindClosestVector(directionToCenter, inwardFacingDirections);

        characterUnit.StartingBattleAnimDirection = closestInwardFacingDirection;
    }

    private Vector2 FindClosestVector(Vector2 target, List<Vector2> vectors)
    {
        float closestDistance = float.MaxValue;
        Vector2 closestVector = Vector2.Zero;

        foreach (Vector2 vector in vectors)
        {
            float distance = target.DistanceSquaredTo(vector);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestVector = vector;
            }
        }

        return closestVector;
    }

    public void PlaceCharacterUnit(CharacterUnit characterUnit)
    {
        SetCharacterPositionAndDirection(characterUnit, _playerStatusPositions[characterUnit.StatusToPlayer]);// characterUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player ? _availablePlayerStartingPositions : characterUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Allied ? _available
    }
}

