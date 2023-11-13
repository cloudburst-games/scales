// HexGrid script. Generate a hex grid from desired grid size (rows/columns), top left start position of the grid, length
// of hexagon side, and orientation (flat vs pointy top), and set display to isometric mode or not (halves vertical display length)

// ***TODO:**
// High priority:
// - Get around the physics hack in HexTilemapIsometricInterface
// - Make a "stacked" version - draw hexes in a stacked layout and correspond to a stacked tilemap

using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public partial class HexGrid : Node
{
    // Draws the grid
    private HexGridVisualiser _hexGridVisualiser;
    // Parent of HexGridVisualiser, modifies overall grid and can add further elements as needed.
    // E.g. can adjust scale to make isometric
    private Node2D _hexDisplay;
    public HexNavigation HexNavigation { get; private set; }

    public enum OrientationMode { PointyTop, FlatTop }
    public enum ConstructionMode { GridSize, WorldSize }
    public enum LayoutMode { Diamond, Stacked }
    public Dictionary<Vector2, Hexagon> Cells { get; private set; } = new Dictionary<Vector2, Hexagon>();

    [Export]
    private OrientationMode _orientation = OrientationMode.FlatTop;
    [Export]
    private ConstructionMode _construction = ConstructionMode.WorldSize;
    [Export]
    private LayoutMode _layout = LayoutMode.Stacked;

    [Export]
    private Vector2 _gridSize = new Vector2(8, 5);
    [Export]
    private Vector2 _startPosition = new Vector2(50, 50);
    [Export]
    private Vector2 _endPosition = new Vector2(150, 150);
    [Export]
    private float _sideLength = 50f;
    [Export]
    private bool _isometric = false;
    // [Export]
    // private bool _isometricStacked = false;
    // This just generates the grid on _Ready, rather than waiting for Start() to be called.
    [Export]
    private bool _makeGridFromInspector = false;

    private Random _rand = new();

    public override void _Ready()
    {
        _hexGridVisualiser = GetNode<HexGridVisualiser>("HexDisplay/HexGridVisualiser");
        _hexDisplay = GetNode<Node2D>("HexDisplay");
        if (_isometric)
        {
            _hexDisplay.Scale = new Vector2(1, 0.5f);

            if (_layout == LayoutMode.Diamond)
            {
                _hexGridVisualiser.RotationDegrees = -45;
            }
            else
            {

                Vector2 dXdY = new(_endPosition.X - _startPosition.X, _endPosition.Y - _startPosition.Y);
                _endPosition = _startPosition + new Vector2(dXdY.X, dXdY.Y * 2);
            }

            // Keep the top left of the displayed grid within the screen boundaries if isometric
            // Disable this we are displaying the grid some other way
            // Note the grid will not be visible if very large as the top left space is empty
            // _hexDisplay.Position = new Vector2(100, _gridSize.X * 33);
        }

        // With "stacked" mode, would this simply be not rotating the grid? -:
        // else if (_isometricStacked)
        // {
        //     _hexGridVisualiser.RotationDegrees = 0;
        //     _hexDisplay.Scale = new Vector2(1, 0.5f);
        // }
        else
        {
            _hexGridVisualiser.RotationDegrees = 0;
            _hexDisplay.Scale = new Vector2(1, 1f);
        }

        if (_makeGridFromInspector)
        {
            Start(_startPosition, _endPosition, _construction);
        }
    }

    // Generates the grid
    public void Start(Vector2 startPosition, Vector2 endPos, ConstructionMode constructionMode)
    {
        _startPosition = startPosition;

        // // Clear (if already set before) - comment out if not needed
        Cells.Clear();
        _hexGridVisualiser.HexesToDraw.Clear();

        // The grid can be generated either by total size (number of rows and columns determined by total area)
        // or by specifying number of rows and columns.
        if (constructionMode == ConstructionMode.WorldSize)
        {
            Vector2 totalArea = endPos - startPosition;
            // GD.Print(totalArea);
            Vector2 hexRect = Hexagon.CalculateRect(_orientation, _sideLength) - (_orientation == OrientationMode.FlatTop
                ? new Vector2(Hexagon.CalculateO(_sideLength), 0)
                : new Vector2(0, Hexagon.CalculateO(_sideLength)));
            _gridSize = new Vector2(
                totalArea.X / hexRect.X,
                totalArea.Y / hexRect.Y
            );
        }
        // Loop through number of columns (x) then rows (y) and generate a hexagon for each grid position
        for (int x = 0; x < _gridSize[0]; x++)
        {
            for (int y = 0; y < _gridSize[1]; y++)
            {
                if (_layout == LayoutMode.Stacked)
                {
                    if (_orientation == OrientationMode.PointyTop)
                    {
                        Hexagon h = new Hexagon(
                            _orientation,
                            _startPosition + new Vector2(
                                // for pointy top hexes, we deduced the incremental spacing to make the side points touch,
                                // with an offset for odd rows (shifting to the right)
                                // the y spacing is consistent for pointy top hexes
                                x * (2 * Hexagon.CalculateA(_sideLength)) + (y % 2 == 0 ? 0 : Hexagon.CalculateA(_sideLength)),
                                y * (Hexagon.CalculateO(_sideLength) + _sideLength)),
                            _sideLength);
                        Cells[new Vector2(x, y)] = h;
                        // GD.Print(x + "," + y);
                        _hexGridVisualiser.HexesToDraw.Add(Cells[new Vector2(x, y)]);
                    }
                    else
                    {
                        Hexagon h = new Hexagon(
                            _orientation,
                            _startPosition + new Vector2(
                                // for flat top hexes, the x spacing is consistent, but the y adds an offset for odd columns
                                x * (Hexagon.CalculateO(_sideLength) + _sideLength),
                                y * (2 * Hexagon.CalculateA(_sideLength)) + (x % 2 == 0 ? 0 : Hexagon.CalculateA(_sideLength))),
                            _sideLength);
                        Cells[new Vector2(x, y)] = h;
                        _hexGridVisualiser.HexesToDraw.Add(Cells[new Vector2(x, y)]);
                    }
                }
                else
                {
                    if (_orientation == OrientationMode.PointyTop)
                    {
                        Hexagon h = new Hexagon(
                            _orientation,
                            _startPosition + new Vector2(
                                // for pointy top hexes, we deduced the incremental spacing to make the side points touch,
                                // with an offset for odd rows (shifting to the right)
                                // the y spacing is consistent for pointy top hexes
                                x * (2 * Hexagon.CalculateA(_sideLength)) + (y % 2 == 0 ? 0 : Hexagon.CalculateA(_sideLength)),
                                y * (Hexagon.CalculateO(_sideLength) + _sideLength)),
                            _sideLength);
                        Cells[new Vector2(x, y)] = h;
                        _hexGridVisualiser.HexesToDraw.Add(Cells[new Vector2(x, y)]);
                    }
                    else
                    {
                        Hexagon h = new Hexagon(
                            _orientation,
                            _startPosition + new Vector2(
                                // for flat top hexes, the x spacing is consistent, but the y adds an offset for odd columns
                                x * (Hexagon.CalculateO(_sideLength) + _sideLength),
                                y * (2 * Hexagon.CalculateA(_sideLength)) + (x % 2 == 0 ? 0 : Hexagon.CalculateA(_sideLength))),
                            _sideLength);
                        Cells[new Vector2(x, y)] = h;
                        _hexGridVisualiser.HexesToDraw.Add(Cells[new Vector2(x, y)]);
                    }
                }

            }
        }
        HexNavigation = new HexNavigation(Cells, _orientation);
        _hexGridVisualiser.Start(_orientation);
    }

    public Vector2 GridToWorld(Vector2 gridPos)
    {
        Vector2 result = GetCentredHexWorldPosition(Cells[gridPos].GetPointPositions()[0]);
        // the world position of the first point is stored in the relevant hex

        // adjust for grid position, scale, and rotation
        result = result.Rotated(_hexGridVisualiser.Rotation) * new Vector2(_hexDisplay.Scale.X, _hexDisplay.Scale.Y) +
            _hexDisplay.Position;
        return result;
    }

    public List<Vector2> GridToWorldPath(Vector2[] gridPath)
    {
        List<Vector2> result = new();
        foreach (Vector2 vec in gridPath)
        {
            result.Add(GridToWorld(vec));
        }
        return result;
    }

    public List<Vector2> HexToWorldPath(List<Hexagon> hexPath)
    {
        List<Vector2> result = new();
        foreach (Hexagon hex in hexPath)
        {
            result.Add(GetHexWorldPosition(hex));
        }
        return result;
    }

    public Vector2 IsometricCorrectionToHexWorld(Vector2 worldPos)
    {
        return worldPos.Rotated(_hexGridVisualiser.Rotation) * new Vector2(_hexDisplay.Scale.X, _hexDisplay.Scale.Y) +
            _hexDisplay.Position;
    }
    public Vector2 IsometricCorrectionFromHexWorld(Vector2 worldPos)
    {
        return ((worldPos - _hexDisplay.Position) * new Vector2(_hexDisplay.Scale.X, 1 / _hexDisplay.Scale.Y))
            .Rotated(-_hexGridVisualiser.Rotation);
    }

    public Vector2 GetCorrectedWorldPosition(Vector2 worldPos)
    {
        return GridToWorld(WorldToGrid(worldPos));
    }

    // we then calculate the centre position of the hex based on the orientation
    // can be deduced with trig (see Hexagon.cs)
    private Vector2 GetCentredHexWorldPosition(Vector2 hexPointPosition)
    {
        if (_orientation == OrientationMode.FlatTop)
        {
            return new Vector2(hexPointPosition.X + _sideLength / 2, hexPointPosition.Y + Hexagon.CalculateA(_sideLength));
        }
        else
        {
            return new Vector2(hexPointPosition.X, hexPointPosition.Y + (Hexagon.CalculateO(_sideLength) + _sideLength / 2f));
        }
    }

    // this just reverses what the GetCentredHexWorldPosition method does
    private Vector2 GetUncentredHexWorldPosition(Vector2 hexWorldPosition)
    {
        if (_orientation == OrientationMode.FlatTop)
        {
            return new Vector2(hexWorldPosition.X - _sideLength / 2, hexWorldPosition.Y - Hexagon.CalculateA(_sideLength));
        }
        else
        {
            return new Vector2(hexWorldPosition.X, hexWorldPosition.Y - (Hexagon.CalculateO(_sideLength) + _sideLength / 2f));
        }
    }

    // there is probably a better way to do this
    // SLOW!
    public Vector2 WorldToGrid(Vector2 worldPos)
    {
        // correct for grid position, scale, and rotation
        worldPos = ((worldPos - _hexDisplay.Position) * new Vector2(_hexDisplay.Scale.X, 1 / _hexDisplay.Scale.Y))
            .Rotated(-_hexGridVisualiser.Rotation);

        worldPos = GetUncentredHexWorldPosition(worldPos);

        Vector2 gridPos = new Vector2(0, 0);
        float currentMinDistanceSquared = Cells[new Vector2(0, 0)].GetPointPositions()[0].DistanceSquaredTo(worldPos);
        foreach (KeyValuePair<Vector2, Hexagon> kv in Cells)
        {
            float distanceSquared = kv.Value.GetPointPositions()[0].DistanceSquaredTo(worldPos);
            if (distanceSquared < currentMinDistanceSquared)
            {
                currentMinDistanceSquared = distanceSquared;
                gridPos = kv.Key;
            }
        }

        return gridPos;
    }

    // Doesn't work...
    // public Vector2 WorldToGrid(Vector2 worldPos)
    // {
    //     // correct for grid position, scale, and rotation
    //     worldPos = ((worldPos - _hexDisplay.Position) * new Vector2(_hexDisplay.Scale.X, 1 / _hexDisplay.Scale.Y))
    //         .Rotated(-_hexGridVisualiser.Rotation);
    //     worldPos = GetUncentredHexWorldPosition(worldPos);

    //     // convert world position to grid coordinates based on hexagonal grid layout
    //     float s = Cells.ElementAt(0).Value.S;
    //     float q = (worldPos.X * (2.0f / 3.0f)) / s;
    //     float r = ((-worldPos.X / 3.0f) + (Mathf.Sqrt(3.0f) / 3.0f * worldPos.Y)) / s;

    //     // round to the nearest hexagon
    //     float x = q;
    //     float y = -q - r;
    //     float z = r;
    //     float rx = Mathf.Round(x);
    //     float ry = Mathf.Round(y);
    //     float rz = Mathf.Round(z);

    //     // adjust coordinates to make sure they sum to 0 (since hexagons are arranged in a grid)
    //     float xDiff = Mathf.Abs(rx - x);
    //     float yDiff = Mathf.Abs(ry - y);
    //     float zDiff = Mathf.Abs(rz - z);

    //     if (xDiff > yDiff && xDiff > zDiff)
    //     {
    //         rx = -ry - rz;
    //     }
    //     else if (yDiff > zDiff)
    //     {
    //         ry = -rx - rz;
    //     }
    //     else
    //     {
    //         rz = -rx - ry;
    //     }

    //     return new Vector2(rx, rz);
    // }

    public Hexagon GetHexAtGridPosition(Vector2 gridPos)
    {
        foreach (Vector2 key in Cells.Keys)
        {
            if (key == gridPos)
            {
                return Cells[key];
            }
        }
        GD.Print("Warning: specified grid position does not exist (HexGrid.cs, GetHexAtGridPosition)");
        return null;
    }

    public Hexagon GetHexAtWorldPosition(Vector2 worldPos)
    {
        Vector2 gridPos = WorldToGrid(worldPos);
        return GetHexAtGridPosition(gridPos);
    }

    private Vector2 GetHexGridPosition(Hexagon hex)
    {
        foreach (KeyValuePair<Vector2, Hexagon> kv in Cells)
        {
            if (kv.Value == hex)
            {
                return kv.Key;
            }
        }
        GD.Print("Warning: specified hex does not exist in cells collection (HexGrid.cs, GetHexGridPosition)");
        return new Vector2();
    }

    public Vector2 GetHexWorldPosition(Hexagon hex)
    {
        Vector2 gridPos = GetHexGridPosition(hex);
        return GridToWorld(gridPos);
    }


    private List<Hexagon> GetHexesInArea(List<Hexagon> hexes, int size)
    {
        if (size > 0)
        {
            foreach (Hexagon hex in hexes.ToList())
            {
                foreach (Hexagon neighbourHex in HexNavigation.GetNeighbouringHexes(hex))
                {
                    if (!hexes.Contains(neighbourHex))
                    {
                        hexes.Add(neighbourHex);
                    }
                }
            }
            size -= 1;
            GetHexesInArea(hexes, size);
        }

        return hexes;
    }

    public Hexagon GetFreeNeighbouringHexByLine(Vector2 startWorldPos, Vector2 endWorldPos, List<Hexagon> ignoredHexes, bool atEnd = true)
    {
        List<Hexagon> result = GetHexesInWorldLine(startWorldPos, endWorldPos, 1.5f);
        if (atEnd)
        {
            result.Reverse();
        }
        foreach (Hexagon hex in result)
        {
            if (!hex.Obstacle && !ignoredHexes.Contains(hex))
            {
                return hex;
            }
        }

        // GD.Print("no valid");
        // return GetHexAtWorldPosition(atEnd ? endWorldPos : startWorldPos); // no valid

        return GetHexAtWorldPosition(startWorldPos); // no valid
        // = no valid hexes in line - try area algorithm
        // return GetFreeNeighbouringHexByArea(GetHexAtWorldPosition(endWorldPos), ignoredHexes, new List<Hexagon>());

    }


    internal Vector2 GetRandomNeighbouringHex(Vector2 worldPos)
    {
        Hexagon hex = GetHexAtWorldPosition(worldPos);
        List<Vector2> neighbourHexes = HexNavigation.GetNeighbouringGridPositions(hex);
        return neighbourHexes[_rand.Next(0, neighbourHexes.Count)];
    }

    // VERY SLOW
    public Hexagon GetFreeNeighbouringHexByArea(Hexagon hex, List<Hexagon> ignoredHexes, List<Hexagon> checkedHexes)
    {
        List<Hexagon> result = new();
        checkedHexes.Add(hex);
        List<Hexagon> neighbourHexes = HexNavigation.GetNeighbouringHexes(hex);
        foreach (Hexagon checkedHex in checkedHexes)
        {
            neighbourHexes.Remove(checkedHex);
        }
        foreach (Hexagon neighbourHex in neighbourHexes)
        {
            if (!neighbourHex.Obstacle && !ignoredHexes.Contains(neighbourHex))
            {
                result.Add(neighbourHex);
            }
        }

        if (result.Count > 0)
        {
            return result[0];
        }
        else
        {
            foreach (Hexagon neighbourHex in neighbourHexes)
            {
                return GetFreeNeighbouringHexByArea(neighbourHex, ignoredHexes, checkedHexes);
            }
        }
        GD.Print("no valid neighbouring hexes");
        return hex; // = no valid hexes
    }

    // TODO: consider reworking if inefficient or inaccurate
    // https://gamedev.stackexchange.com/questions/57087/line-draw-on-even-q-vertical-flat-topped-hex-grid/57098#57098
    // https://stackoverflow.com/questions/3233522/elegant-clean-special-case-straight-line-grid-traversal-algorithm
    private List<Hexagon> GetHexesInWorldLine(Vector2 startWorldPos, Vector2 endWorldPos, float sensitivity = 1.1f) // higher sensitivity = thicker line
    {
        List<Hexagon> result = new List<Hexagon>();
        float increment = _sideLength / sensitivity;
        startWorldPos = GetCorrectedWorldPosition(startWorldPos);
        endWorldPos = GetCorrectedWorldPosition(endWorldPos);

        Vector2 incrementVec = (endWorldPos - startWorldPos).Normalized() * increment;
        Vector2 currentPos = startWorldPos;

        // GetNode<Label>("Debug/DebugInfo1").Text = "Distance start to next pos: " + startWorldPos.DistanceTo(currentPos + incrementVec).ToString();
        // GetNode<Label>("Debug/DebugInfo2").Text = "Increment value: " + increment.ToString();
        for (float distance = 0; distance < startWorldPos.DistanceTo(endWorldPos); distance += increment)
        {
            Hexagon h = GetHexAtWorldPosition(currentPos);
            if (!result.Contains(h))
            {
                result.Add(h);
            }
            currentPos += incrementVec;
        }

        // if the sensitivity is too low, the end hex sometimes is missed, so this includes it
        Hexagon endHex = GetHexAtWorldPosition(endWorldPos);
        if (!result.Contains(endHex))
        {
            result.Add(endHex);
        }
        return result;
    }

    public int GetHexDistanceByWorld(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        return GetHexesInWorldLine(startWorldPos, endWorldPos).Count - 1;
    }
    public int GetHexDistanceByGrid(Vector2 startGridPos, Vector2 endGridPos)
    {
        return GetHexesInWorldLine(GridToWorld(startGridPos), GridToWorld(endGridPos)).Count - 1;
    }

    public bool IsLOSBlocked(Vector2 startWorldPos, Vector2 endWorldPos)
    {
        List<Hexagon> hexesInLOS = GetHexesInWorldLine(startWorldPos, endWorldPos);
        if (hexesInLOS.Count > 0)
        {
            hexesInLOS.RemoveAt(hexesInLOS.Count - 1);
        }
        foreach (Hexagon hex in hexesInLOS)
        {
            if (hex.Obstacle)
            {
                return true;
            }
        }
        return false;
    }

    private Tuple<List<Hexagon>, List<Hexagon>> GetHexesInLOS(Vector2 startGridPos, Vector2 endGridPos)
    {
        List<Hexagon> visibleHexes = GetHexesInWorldLine(startGridPos, endGridPos);
        List<Hexagon> blockedHexes = new List<Hexagon>();
        for (int i = 0; i < visibleHexes.ToList().Count; i++)
        {
            if (visibleHexes.ToList()[i].Obstacle)
            {
                blockedHexes.AddRange(visibleHexes.GetRange(i, visibleHexes.ToList().Count - 1));
                visibleHexes.RemoveRange(i, visibleHexes.ToList().Count - i);
                break;
            }
        }

        return Tuple.Create<List<Hexagon>, List<Hexagon>>(visibleHexes, blockedHexes);
    }

    // This should be called whenever a hex property such as Obstacle bool changes, to recalculate navigation (and display)
    public void UpdateNavigationAndDisplay()
    {
        UpdateNavigation();
        UpdateDisplay();
    }

    public void UpdateNavigation()
    {
        HexNavigation.RecalculateAStarMap();
    }

    public void UpdateDisplay()
    {
        List<Hexagon> obstacles = new List<Hexagon>();
        foreach (Hexagon h in Cells.Values)
        {
            if (h.Obstacle)
            {
                obstacles.Add(h);
            }
        }
        _hexGridVisualiser.HighlightHexes(Cells.Values.ToList(), new Color(1, 1, 1));
        _hexGridVisualiser.HighlightHexes(obstacles, new Color(0, 1, 1));
    }


    // // TESTING
    // public override void _Input(InputEvent ev)
    // {
    //     base._Input(ev);

    //     if (ev.IsActionPressed("ui_focus_next") && !ev.IsEcho())
    //     {
    //         _hexGridVisualiser.ToggleGrid();
    //     }

    //     if (ev.IsActionPressed("ui_select") && !ev.IsEcho())
    //     {

    //         Vector2 mousePos = _hexDisplay.GetGlobalMousePosition();
    //         Hexagon hex = GetHexAtWorldPosition(mousePos);
    //         GetNode<Label>("Debug/DebugInfo1").Text = "Selected hex at grid position: " + GetHexGridPosition(hex);

    //         // string neighbouringHexesStr = "Neighbouring hexes' grid positions:";
    //         // foreach (Hexagon neighbouringHex in GetNeighbouringHexes(hex))
    //         // {
    //         //     neighbouringHexesStr += ", " + GetHexGridPosition(neighbouringHex);
    //         // }

    //     }

    //     // TESTING AREA CODE
    //     // if (ev is InputEventMouseButton btn)
    //     // {
    //     //     if (btn.Pressed)
    //     //     {
    //     //         Vector2 mousePos = _hexDisplay.GetGlobalMousePosition();
    //     //         Hexagon hex = GetHexAtWorldPosition(mousePos);
    //     //         string areaHexesStr = "Hexes' in area of 2 grid positions:";
    //     //         List<Hexagon> hexesInArea = GetHexesInArea(new List<Hexagon>() {hex}, 2);
    //     //         foreach (Hexagon areaHex in hexesInArea)
    //     //         {
    //     //             areaHexesStr += ", " + GetHexGridPosition(areaHex);
    //     //         }
    //     //         _hexGridVisualiser.ResetDrawing();
    //     //         _hexGridVisualiser.HighlightHexes(hexesInArea, new Color(0,1,0));
    //     //         GetNode<Label>("Debug/DebugInfo2").Text = areaHexesStr;
    //     //     }
    //     // }

    //     // TESTING LOS CODE
    //     // if (ev is InputEventMouseButton btn)
    //     // {
    //     //     if (btn.Pressed)
    //     //     {
    //     //         _hexGridVisualiser.ResetDrawing();
    //     //         UpdateDisplay();
    //     //         Vector2 mousePos = _hexDisplay.GetGlobalMousePosition();
    //     //         if (btn.ButtonIndex == MouseButton.Left)
    //     //         {
    //     //             _selectedPos1 = mousePos;
    //     //         }
    //     //         else if (btn.ButtonIndex == MouseButton.Right)
    //     //         {
    //     //             _selectedPos2 = mousePos;
    //     //         }
    //     //         // else if (btn.ButtonIndex == MouseButton.Middle)
    //     //         // {
    //     //         //     Hexagon blockThisHex = GetHexAtWorldPosition(mousePos);
    //     //         //     blockThisHex.Obstacle = !blockThisHex.Obstacle;
    //     //         //     // _hexGridVisualiser.HighlightHexes(new List<Hexagon>() {blockThisHex}, new Color(0,1,1));
    //     //         // }
    //     //         _hexGridVisualiser.HighlightHexes(GetHexesInWorldLine(_selectedPos1, _selectedPos2), new Color(1,0,0));
    //     //         GetNode<Label>("Debug/DebugInfo1").Text = "Is line of sight blocked? " + IsLOSBlocked(_selectedPos1, _selectedPos2).ToString();
    //     //     }
    //     // }

    //     // TESTING NAVIGATION CODE
    //     if (ev is InputEventMouseButton btn)
    //     {
    //         if (btn.Pressed)
    //         {
    //             // _hexGridVisualiser.ResetDrawing();
    //             UpdateDisplay();
    //             Vector2 mousePos = _hexDisplay.GetGlobalMousePosition();
    //             if (btn.ButtonIndex == MouseButton.Left)
    //             {
    //                 _selectedPos1 = WorldToGrid(mousePos);
    //             }
    //             else if (btn.ButtonIndex == MouseButton.Right)
    //             {
    //                 _selectedPos2 = WorldToGrid(mousePos);
    //             }
    //             // else if (btn.ButtonIndex == (int) ButtonList.Middle)
    //             // {
    //             //     Hexagon blockThisHex = GetHexAtWorldPosition(mousePos);
    //             //     blockThisHex.Obstacle = !blockThisHex.Obstacle;
    //             //     // _hexGridVisualiser.HighlightObstacle(blockThisHex);
    //             //     _hexNavigation.RecalculateAStarMap();
    //             // }

    //             _hexGridVisualiser.HighlightHexes(HexNavigation.CalculateHexPath(_selectedPos1, _selectedPos2), new Color(1, 0, 0));
    //             // GetNode<Label>("Debug/DebugInfo1").Text = "Is line of sight blocked? " + IsLOSBlocked(_selectedPos1, _selectedPos2).ToString();
    //         }
    //     }
    //     if (ev.IsActionPressed("ui_home"))
    //     {
    //         HexNavigation.ToggleTemporaryObstacle(WorldToGrid(_hexDisplay.GetGlobalMousePosition()), true);
    //     }
    // }

    private Vector2 _selectedPos1 = new Vector2();
    private Vector2 _selectedPos2 = new Vector2();

    public override void _Process(double delta)
    {
        base._Process(delta);
        Vector2 mousePos = _hexGridVisualiser.GetGlobalMousePosition();
        GetNode<Label>("Debug/GridPos").Text = "Hex Grid Pos: " + WorldToGrid(mousePos).ToString();
        GetNode<Label>("Debug/WorldPos").Text = "Hex World Pos: " + GridToWorld(WorldToGrid(mousePos));
        GetNode<Label>("Debug/MousePos").Text = "Mouse Pos: " + mousePos.ToString();
    }
}
