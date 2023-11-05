using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class HexObstacleMarkerTool : Node2D
{
    [Signal]
    public delegate void FinishedMarkingObstaclesEventHandler();

    private HexGrid _hexGrid;
    public HexObstacleMarkerTool(HexGrid hexGrid)
    {
        _hexGrid = hexGrid;
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
    private Area2D MakeDummyArea(Hexagon hex, Vector2 pos)
    {
        Area2D dummyArea = new Area2D();
        CollisionShape2D shape = new CollisionShape2D();
        dummyArea.AddChild(shape);
        shape.Shape = new CircleShape2D();
        ((CircleShape2D)shape.Shape).Radius = (hex.A > hex.O ? hex.O : hex.A) / 8;
        dummyArea.Position = pos;
        AddChild(dummyArea);
        return dummyArea;
    }

}