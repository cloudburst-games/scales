using Godot;
using System.Collections.Generic;

public class HexGridData : IJSONSaveable
{
    public List<Vector2> ObstacleGridPositions {get; set;} = new();
}
