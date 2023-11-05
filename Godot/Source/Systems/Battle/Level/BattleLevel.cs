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

    private HexTilemapIsometricInterface _hexTileInterface; // used only to get the hex grid start pos because i couldnt figure out how else to do it!
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
        _hexTileInterface = new(HexGrid, _dummyTilemap);
        HexGrid.AddChild(_hexTileInterface);
        _hexTileInterface.FinishedMarkingObstacles += HexGrid.UpdateNavigationAndDisplay;
        // GD.Print(_hexTileInterface.GetHexGridBoundsFromTilemap()[0]);
        // GD.Print(_hexTileInterface.GetHexGridBoundsFromTilemap()[1]);

        // _hexGrid.Start(new Vector2(-1436.841f, 1595.2328f), new Vector2(1074.8021f, 4061.621f), HexGrid.ConstructionMode.WorldSize);
        HexGrid.Start(_hexTileInterface.GetHexGridBoundsFromTilemap()[0], _hexTileInterface.GetHexGridBoundsFromTilemap()[1], HexGrid.ConstructionMode.WorldSize);

        // _hexObstacleMarkerTool.MarkAllHexObstacles(LevelID);
        _hexTileInterface.MarkAllHexObstacles(LevelID);
        // _hexGrid.UpdateNavigationAndDisplay(); // shouldnt need to do it twice???
        HexModifier.Init(HexGrid);
    }

    private void SetCharacterPosition(CharacterUnit characterUnit, List<Vector2> positionPool)
    {
        characterUnit.Position = positionPool[0];
        positionPool.RemoveAt(0);
        Vector2 size = _background.GlobalPosition + (_background.Texture.GetSize() * _background.Scale);
        // Note: character global position is 0,0 as not yet added to scene tree. conveniently, position is same as global position though, as BattleLevel is at 0,0
        // It may get messed up if this changes though

        // GD.Print("size: ", size);
        // GD.Print("pos: ", characterUnit.Position);
        // Set the orientation
        if (characterUnit.Position.X < size.X * 0.5f)// _background.Texture.GetSize().X * _background.Scale.X / 2f)
        {
            if (characterUnit.Position.Y < size.Y * 0.33f)//  _background.Texture.GetSize().Y * _background.Scale.Y * 0.33f)
            {
                //se
                characterUnit.StartingBattleAnimDirection = new Vector2(1, 1);
            }
            else if (characterUnit.Position.Y < size.Y * 0.66f)//  _background.Texture.GetSize().Y * _background.Scale.Y * 0.66f)
            {
                // e
                characterUnit.StartingBattleAnimDirection = new Vector2(1, 0);
            }
            else
            {
                characterUnit.StartingBattleAnimDirection = new Vector2(1, -1);
            }
        }
        else
        {
            if (characterUnit.Position.Y < size.Y * 0.33f)//_background.Texture.GetSize().Y * _background.Scale.Y * 0.33f)
            {
                //sw
                characterUnit.StartingBattleAnimDirection = new Vector2(-1, 1);
            }
            else if (characterUnit.Position.Y < size.Y * 0.66f)//_background.Texture.GetSize().Y * _background.Scale.Y * 0.66f)
            {
                // w
                characterUnit.StartingBattleAnimDirection = new Vector2(-1, 0);
            }
            else
            {
                characterUnit.StartingBattleAnimDirection = new Vector2(-1, -1);
                // nw
            }
        }
        // GD.Print(characterUnit.StartingBattleAnimDirection);
    }

    public void PlaceCharacterUnit(CharacterUnit characterUnit)
    {
        SetCharacterPosition(characterUnit, _playerStatusPositions[characterUnit.StatusToPlayer]);// characterUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Player ? _availablePlayerStartingPositions : characterUnit.StatusToPlayer == CharacterUnit.StatusToPlayerMode.Allied ? _available
    }
}

// TODO - make a separate grid visualiser with multiple modes:
// show whole grid + obstacles
// show only pathing
