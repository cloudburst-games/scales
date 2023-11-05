// Class responsible for any interfacing between a hex grid and tilemap
// Takes a Tilemap and a HexGrid as arguments - shouild only be used once both tilemap and hexgrid will not be modified
// following initialisation
using Godot;
using System.Linq;
using System.Collections.Generic;
using System;

public partial class HexTilemapIsometricInterface : Node2D
{

    // public delegate void FinishedMarkingObstaclesDelegate();
    // public event FinishedMarkingObstaclesDelegate FinishedMarkingObstacles;

    [Signal]
    public delegate void FinishedMarkingObstaclesEventHandler();

    private HexGrid _hexGrid;
    private TileMap _tileMap;

    public HexTilemapIsometricInterface()
    {
        throw new NotImplementedException();
    }

    public HexTilemapIsometricInterface(HexGrid hexGrid, TileMap tileMap)
    {
        _hexGrid = hexGrid;
        _tileMap = tileMap;

    }

    // Get the grid coordinates of the leftmost and rightmost tilemap tiles
    // Then convert these into the isometric tilemap world positions
    // Depending on the tilemap, adjust the startoffset and endoffset as required (requires experimentation)
    // Then correct this using the hexgrid helper method (which adjusts for 45 degree rotation and y scaling),
    // and return the start and end bound to be used for hex grid generation    
    public Vector2[] GetHexGridBoundsFromTilemap()
    {
        List<Vector2I> usedCells = new();

        foreach (Vector2I v in _tileMap.GetUsedCells(0))
        {
            usedCells.Add(v);
        }
        var orderbyResult = (from vec in usedCells
                             orderby vec.X
                             orderby vec.Y descending
                             select vec).ToList();

        Vector2 startOffset = new Vector2(-_tileMap.TileSet.TileSize.X / 8, -_tileMap.TileSet.TileSize.Y / 4);
        Vector2 endOffset = new Vector2(-_tileMap.TileSet.TileSize.X / 8, 0);

        Vector2 startPos = _tileMap.MapToLocal(orderbyResult[0]) + startOffset;
        startPos = _hexGrid.IsometricCorrectionFromHexWorld(startPos);
        Vector2 endPos = _tileMap.MapToLocal(orderbyResult[orderbyResult.Count - 1]) + endOffset;
        endPos = _hexGrid.IsometricCorrectionFromHexWorld(endPos);

        return new Vector2[2] { startPos, endPos };
    }

    public async void MarkAllHexObstacles(int levelID)
    {
        string obstacleDataPath = "/RuntimeData/ObstacleData" + levelID.ToString() + ".json";
        // _JSONDataHandler.SaveToDisk(PackAllSettings(), "/Settings.json");
        if (System.IO.File.Exists(OS.GetUserDataDir() + obstacleDataPath))
        {
            JSONDataHandler dataHandler = new();
            HexGridData hexGridData = dataHandler.LoadFromJSON<HexGridData>(obstacleDataPath);
            foreach (Vector2 gridPos in hexGridData.ObstacleGridPositions)
            {
                _hexGrid.Cells[gridPos].Obstacle = true;
            }
            EmitSignal(SignalName.FinishedMarkingObstacles);
            return;
        }
        else
        {

            List<Hexagon> allHexes = _hexGrid.Cells.Values.ToList();
            for (int i = 0; i < allHexes.Count; i++)
            {
                MarkObstacleAtHex(allHexes[i], i, allHexes.Count - 1);
                MarkObstacleAtImpassableTile(allHexes[i]);
            }
            await ToSignal(this, SignalName.FinishedMarkingObstacles);

            HexGridData hexGridData = new();
            foreach (KeyValuePair<Vector2, Hexagon> kv in _hexGrid.Cells)
            {
                if (kv.Value.Obstacle)
                {
                    hexGridData.ObstacleGridPositions.Add(kv.Key);
                }
            }
            JSONDataHandler dataHandler = new();
            dataHandler.SaveToDisk(hexGridData, obstacleDataPath);
        }
    }

    public void MarkObstacleAtImpassableTile(Hexagon hex)
    {
        Vector2 worldPos = _hexGrid.GetHexWorldPosition(hex);
        Vector2I tilePos = _tileMap.LocalToMap(worldPos);
        if (_tileMap.GetCellTileData(0, tilePos) == null)
            return;
        string rules = (string)_tileMap.GetCellTileData(0, tilePos).GetCustomData("rules");
        hex.Obstacle = rules.Substring(0, 4) == "0000";
        // GD.Print(_hexGrid.GetHexWorldPosition(hex) + ": " + hex.Obstacle);
    }

    // TODO: access physics server to check for collision points at each hex position,
    // rather than using Area2D.
    // Currently this is a really hacky way to check if hex is at a collision point
    // Creates a small area corresponding to size of the hex, at each hex position, then checks
    // if overlap with a physics body, and if so marks the hex as an obstacle.
    // Publish a finished event. Mainly so we can set navigation accordingly.
    public async void MarkObstacleAtHex(Hexagon hex, int currentCount = -1, int totalCount = -1)
    {
        Vector2 hexWorldPos = _hexGrid.GetHexWorldPosition(hex);


        // Physics Server ATTEMPT - i don't understand how it works..
        // PhysicsDirectSpaceState2DExtension test = new();
        // PhysicsPointQueryParameters2D testp = new()
        // {
        //     CollideWithBodies = true,
        //     Position = hexWorldPos
        // };
        // var ipTest = test.IntersectPoint(testp); //(testp.Position, (long) testp.CanvasInstanceId, testp.CollisionMask,
        //     //testp.CollideWithBodies, testp.CollideWithAreas, 0, 32);
        // GD.Print(ipTest.Count);
        // // if (ipTest > 0)
        // // {
        // //     GD.Print("asd");
        // // }

        Area2D dummyArea = MakeDummyArea(hex, hexWorldPos);

        await _hexGrid.ToSignal(_hexGrid.GetTree(), "process_frame");
        await _hexGrid.ToSignal(_hexGrid.GetTree(), "process_frame");
        // await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        if (dummyArea.GetOverlappingBodies().Count > 0)
        {
            hex.Obstacle = true;
            // GD.Print("OBSTACLE DETECTED AT " + hexWorldPos);
        }
        // hex.Obstacle = dummyArea.GetOverlappingBodies().Count > 0;

        // dummyArea.Free();

        if (currentCount == totalCount)
        {
            await _hexGrid.ToSignal(_hexGrid.GetTree(), "process_frame");
            await _hexGrid.ToSignal(_hexGrid.GetTree(), "process_frame");
            // FinishedMarkingObstacles?.Invoke();
            EmitSignal(SignalName.FinishedMarkingObstacles);
        }
    }


    public async override void _Input(InputEvent ev)
    {
        if (ev is InputEventMouseButton btn)
        {
            if (btn.ButtonIndex == MouseButton.Left)
            {
                Vector2 worldPos = GetGlobalMousePosition();
                Hexagon hex = _hexGrid.GetHexAtWorldPosition(worldPos);
                Area2D dummyArea = MakeDummyArea(hex, worldPos);

                await ToSignal(_hexGrid.GetTree(), "process_frame");
                await ToSignal(_hexGrid.GetTree(), "process_frame");

                if (dummyArea.GetOverlappingBodies().Count > 0)
                {
                    hex.Obstacle = true;
                    GD.Print("OBSTACLE DETECTED AT " + worldPos);
                }
                EmitSignal(SignalName.FinishedMarkingObstacles);
                // var spaceState = GetWorld2D().DirectSpaceState;
                // var query = PhysicsRayQueryParameters2D.Create(GetGlobalMousePosition() - new Vector2(25,25), GetGlobalMousePosition() + new Vector2(25,25));
                // var result = spaceState.IntersectRay(query);
                // GD.Print(result.Count);
            }
        }
    }

    private Area2D MakeDummyArea(Hexagon hex, Vector2 pos)
    {
        Area2D dummyArea = new Area2D();
        CollisionShape2D shape = new CollisionShape2D();
        dummyArea.AddChild(shape);
        shape.Shape = new CircleShape2D();
        ((CircleShape2D)shape.Shape).Radius = (hex.A > hex.O ? hex.O : hex.A) / 8;
        dummyArea.Position = pos;
        _tileMap.AddChild(dummyArea);
        return dummyArea;
    }

    // public void Die()
    // {
    //     FinishedMarkingObstacles = null;
    // }

}
