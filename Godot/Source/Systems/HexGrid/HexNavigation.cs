using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class HexNavigation
{
    private AStar2D _aStar = new AStar2D();
    private Dictionary<Vector2, int> _pointIDs = new Dictionary<Vector2, int>();
    private Dictionary<Vector2, Hexagon> _cells = new Dictionary<Vector2, Hexagon>();
    private HexGrid.OrientationMode _orientation;
    private int _pIDCount = 0;
    
    public HexNavigation()
    {
        throw new NotImplementedException();
    }

    public HexNavigation(Dictionary<Vector2, Hexagon> cells, HexGrid.OrientationMode orientation)
    {
        _cells = cells;
        _orientation = orientation;
        // _pointIDs.Clear();
        
        RecalculateAStarMap();

    }

    public void RecalculateAStarMap()//Dictionary<Vector2, Hexagon> cells)
    {
        // _cells.Clear();
        _aStar.Clear();
        _pointIDs.Clear();

        SetPointIDs();
        AddCells();
        ConnectCells();
    }

    private void SetPointIDs()
    {
        _pIDCount = 0;
        foreach (Vector2 point in _cells.Keys)
        {
            _pointIDs.Add(point, _pIDCount);
            _pIDCount += 1;
        }
    }

    private void AddCells()
    {
        foreach (Vector2 point in _cells.Keys)
        {
            if (_cells[point].Obstacle)
            {
                continue;
            }
            _aStar.AddPoint(_pointIDs[point], point);
        }
    }

    private void ConnectCells()
    {
        foreach (Vector2 point in _cells.Keys)
        {
            if (_cells[point].Obstacle)
            {
                continue;
            }
            foreach (Vector2 neighbour in GetNeighbouringGridPositions(_cells[point]))
            {
                if (_cells[neighbour].Obstacle)
                {
                    continue;
                }
                if (_aStar.ArePointsConnected(_pointIDs[point], _pointIDs[neighbour]))
                {
                    continue;
                }
                _aStar.ConnectPoints(_pointIDs[point], _pointIDs[neighbour], true);
            }
        }
    }

    private void ConnectSingleCell(Vector2 point)
    {
        foreach (Vector2 neighbour in GetNeighbouringGridPositions(_cells[point]))
        {
            _aStar.ConnectPoints(_pointIDs[point], _pointIDs[neighbour], true);
        }
    }
    private void DisconnectSingleCell(Vector2 point)
    {
        foreach (Vector2 neighbour in GetNeighbouringGridPositions(_cells[point]))
        {
            _aStar.DisconnectPoints(_pointIDs[point], _pointIDs[neighbour], true);
        }
    }


    public Vector2[] CalculateGridPath(Vector2 gridStart, Vector2 gridEnd)
    {
        if (!_cells.ContainsKey(gridStart) || !_cells.ContainsKey(gridEnd))
        {
            GD.Print("does not contain the key!");
            return new Vector2[0];
        }
        if (_cells[gridStart].Obstacle || _cells[gridEnd].Obstacle)
        {
            // GD.Print("start or end position is an obstacle!");
            return new Vector2[0];
        }
        // if (!_pointIDs.ContainsKey(gridStart) || ! _pointIDs.ContainsKey(gridEnd))
        // {
        //     return new Vector2[0];
        // }
        int startIndex = _pointIDs[gridStart];
        int endIndex = _pointIDs[gridEnd];
        return _aStar.GetPointPath(startIndex, endIndex);
    }

    public List<Hexagon> CalculateHexPath(Vector2 gridStart, Vector2 gridEnd)
    {
        List<Hexagon> result = new List<Hexagon>();
        Vector2[] gridCoordPath = CalculateGridPath(gridStart, gridEnd);
        foreach (Vector2 pos in gridCoordPath)
        {
            if (_cells.ContainsKey(pos))
            {
                result.Add(_cells[pos]);
            }
            else
            {
                GD.Print("HexNavigation.cs CalculatePath: _cells does not contain the specified coordinate");
            }
        }
        return result;
    }

    public List<Hexagon> GetNeighbouringHexes(Hexagon hex)
    {
        List<Hexagon> result = new List<Hexagon>();

        foreach (KeyValuePair<Vector2, Hexagon> kv in _cells)
        {
            if (kv.Value == hex)
            {
                foreach (Vector2 pos in GetNeighbouringGridPositions(kv.Key))
                {
                    if (_cells.ContainsKey(pos))
                    {
                        result.Add(_cells[pos]);
                    }
                }
                break;
            }
        }
        return result;
    }

    public List<Vector2> GetNeighbouringGridPositions(Hexagon hex)
    {
        List<Vector2> result = new List<Vector2>();

        foreach (KeyValuePair<Vector2, Hexagon> kv in _cells)
        {
            if (kv.Value == hex)
            {
                foreach (Vector2 pos in GetNeighbouringGridPositions(kv.Key))
                {
                    if (_cells.ContainsKey(pos))
                    {
                        result.Add(pos);
                    }
                }
                break;
            }
        }
        return result;
    }

    public void ToggleTemporaryObstacle(Vector2 gridPosition, bool disabled)
    {
        if (_pointIDs.ContainsKey(gridPosition))
        {
            // if (disabled)
            // {
            //     DisconnectSingleCell(gridPosition);
            // }
            // else
            // {
            //     ConnectSingleCell(gridPosition);
            // }
            _aStar.SetPointDisabled(_pointIDs[gridPosition], disabled);
            // GD.Print("point disabled at " + gridPosition + ": " + disabled);
        }
        else
        {
            GD.Print("in ToggleTemporaryObstacle: aStar key not found!");
        }
        
    }

    // public Vector2 GetNearestNonObstacleGridNeighbour(Vector2 startPosition, Vector2 endPosition)
    // {
    //     List<Vector2> neighbours = GetNeighbouringGridPositions(startPosition).Where(vec => !_cells[vec].Obstacle).ToList();
    //     // _aStar.SetPointDisabled()
    //     float xdistance = Math.Abs(endPosition.X - neighbours[0].X);
    //     float ydistance = Math.Abs(endPosition.Y - neighbours[0].Y);
    //     float closestX = neighbours[0].X;
    //     foreach (Vector2 neighbour in neighbours)
    //     {
    //         if (Math.Abs(endPosition.X - neighbour.X) < xdistance)
    //         {
                
    //         }
    //     }


    //     // neighbours.OrderBy(vec => vec.DistanceSquaredTo(endPosition));
    //     // return GetNeighbouringGridPositions(gridPosition).OrderBy(
    //     //     vec => vec.X).ThenBy(vec => vec.Y).Where(vec => !_cells[vec].Obstacle).ToList()[0];
    //     return GetNeighbouringGridPositions(startPosition).OrderBy(vec => vec.DistanceSquaredTo(endPosition)).ToList()[0];
    // }

    public List<Vector2> GetNeighbouringGridPositions(Vector2 gridPosition)
    {
        List<Vector2> result;
        
        if (_orientation == HexGrid.OrientationMode.FlatTop)
        {
            result = gridPosition.X % 2 == 0 ? new List<Vector2>() {
                new Vector2(gridPosition.X, gridPosition.Y - 1),
                new Vector2(gridPosition.X + 1, gridPosition.Y - 1),
                new Vector2(gridPosition.X + 1, gridPosition.Y),
                new Vector2(gridPosition.X, gridPosition.Y + 1),
                new Vector2(gridPosition.X - 1, gridPosition.Y),
                new Vector2(gridPosition.X - 1, gridPosition.Y - 1),
            }
            : new List<Vector2>() {
                new Vector2(gridPosition.X, gridPosition.Y - 1),
                new Vector2(gridPosition.X + 1, gridPosition.Y),
                new Vector2(gridPosition.X + 1, gridPosition.Y + 1),
                new Vector2(gridPosition.X, gridPosition.Y + 1),
                new Vector2(gridPosition.X - 1, gridPosition.Y + 1),
                new Vector2(gridPosition.X - 1, gridPosition.Y),
            };
        }
        else
        {
            result = gridPosition.Y % 2 == 0 ? new List<Vector2>() {
                new Vector2(gridPosition.X, gridPosition.Y - 1),
                new Vector2(gridPosition.X + 1, gridPosition.Y),
                new Vector2(gridPosition.X, gridPosition.Y + 1),
                new Vector2(gridPosition.X - 1, gridPosition.Y + 1),
                new Vector2(gridPosition.X - 1, gridPosition.Y),
                new Vector2(gridPosition.X - 1, gridPosition.Y - 1),
            }
            : new List<Vector2>() {
                new Vector2(gridPosition.X, gridPosition.Y - 1),
                new Vector2(gridPosition.X + 1, gridPosition.Y - 1),
                new Vector2(gridPosition.X + 1, gridPosition.Y),
                new Vector2(gridPosition.X + 1, gridPosition.Y + 1),
                new Vector2(gridPosition.X, gridPosition.Y + 1),
                new Vector2(gridPosition.X - 1, gridPosition.Y),
            };
        }

        foreach (Vector2 pos in result.ToList())
        {
            if (!_cells.ContainsKey(pos))
            {
                result.Remove(pos);
            }
        }

        return result;
    }
}
