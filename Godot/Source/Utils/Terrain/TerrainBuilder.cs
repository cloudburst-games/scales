// Terrain script
// Paint terrain with calculated tile transitions

// TODO (see also gitlab)
// - Add elevation tiles and transitions
// - Add multiple brush sizes
// - Replicate homm terrain editor functionality

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class TerrainBuilder : Node2D
{
    // determines what happens after FileDialog confirmation
    private int _saveMode = 0; // 0 = tscn, 1 = save terrainData, 2 = load terrainData
    
    // Set in ready, the size of each cell.
	private Vector2 _tileSize;
    // Start off with 0 (water) - can be changed via UI
	private int _selectedTerrainTile = 0;

    // Tilemaps are generated according to the tileset.
    [Export]
    private TileSet _currentTileset;// = GD.Load<TileSet>("res://Test3.tres");

	[Export]
	private Vector2I _gridSize;

    // Links each terrain type to the appropriate Grid tilemap (e.g. water 0 -> Level1, shore 1 -> Level1, earth 2 -> Level2) 
    private Dictionary<int, TerrainGrid> _terrainGridDict = new Dictionary<int, TerrainGrid>();

    public override void _Ready()
    {
        GetNode<TerrainGrid>("Tilemaps/Level1").TileSet = _currentTileset;
        GetNode<TerrainGrid>("Tilemaps/Level1").AtlasTileList = GetNode<TerrainGrid>("Tilemaps/Level1").GenerateAtlasTileList(_currentTileset);
        GetNode<TerrainGrid>("Tilemaps/Level1").CachedTileData = GetNode<TerrainGrid>("Tilemaps/Level1").GenerateCachedTileData();
        _tileSize = GetNode<TerrainGrid>("Tilemaps/Level1").TileSet.TileSize;

        // First generate the Grids (tilemaps) from each tile type in the tileset.
        GenerateTilemaps();
        // Set the scroll boundaries of the camera according to the grid size and tile size.
		FindAndSetCameraBoundaries();
        // Shows the grid using GridVisualiser based on tile and grid size.
		RenderGrid();
    }

    // Set the camera boundaries according to the grid size
    // Note, we may need to re-set them when the camera zooms in and out (ideally adjust this within the camera code)
	private void FindAndSetCameraBoundaries()
	{
        Vector2 xBuffer = new Vector2((float)Math.Log(GetMainGridSize().X), 0)*(GetMainGridSize().X*5);
        Vector2 yBuffer = new Vector2(0,(float)Math.Log(GetMainGridSize().Y))*(GetMainGridSize().Y*5);
        Vector2[] edgeCoords = new Vector2[4] {
            GetNode<TileMap>("Tilemaps/Level1").MapToLocal(new Vector2I(GetMainGridSize().X/2, 0)) - yBuffer,
            GetNode<TileMap>("Tilemaps/Level1").MapToLocal(new Vector2I(GetMainGridSize().X, GetMainGridSize().Y/2)) + xBuffer*2f,
            GetNode<TileMap>("Tilemaps/Level1").MapToLocal(new Vector2I(GetMainGridSize().X/2, GetMainGridSize().Y)) + yBuffer,
            GetNode<TileMap>("Tilemaps/Level1").MapToLocal(new Vector2I(0, GetMainGridSize().Y/2)) - xBuffer*2f,
        };
		Vector2 centre = GetNode<TileMap>("Tilemaps/Level1").MapToLocal(GetMainGridSize()/2);
        GetNode<Cam2DTopDown>("Cam2DTopDown").EdgeType = Cam2DTopDown.Camera2DEdgeType.Diamond;
        GetNode<Cam2DTopDown>("Cam2DTopDown").EdgeCoords = edgeCoords;
        GetNode<Cam2DTopDown>("Cam2DTopDown").MakeCurrent();
        GetNode<Cam2DTopDown>("Cam2DTopDown").Position = centre;
	}

    // Draw the grid according to grid and tile sizes
	public void RenderGrid()
	{
		GetNode<TerrainGridVisualiser>("GridVisualiser").SetGrid(_tileSize, GetMainGridSize());
	}

	private Vector2I GetMainGridSize()
	{
		return new Vector2I(GetNode<TerrainGrid>("Tilemaps/Level1").MainGrid.Count, GetNode<TerrainGrid>("Tilemaps/Level1").MainGrid[0].Count);
	}

    /*
    old transition rules (heroes 3 style):
// This method sets surrounding tiles according to transition rules.
	private void CalcTerrainBorders(Vector2 gridPosition, Vector2[] calcList)
	{
		int x = (int)gridPosition.X;
		int y = (int)gridPosition.Y;

		// Cycle through surrounding tiles and compare current tile with surrounding tiles. 
		for (int i = 0; i <= calcList.GetUpperBound(0); i++)
		{
			int borderX = (int)calcList [i].X;
			int borderY = (int)calcList [i].Y;

			// Only calculate if the border grid contains the tile
			if (BorderGridContains(calcList[i])) {
				// If current tile is water: 
				if (_borderGrid[x] [y] == 0)
				{
					// if ANY surrounding tiles are not water or not shore [if > 0], make them shore (1)
					if (_borderGrid [borderX] [borderY] > 1)
						_borderGrid [borderX] [borderY] = 1;
				}
				// If current tile is not water
				else
				{
                    
					// if ANY surrounding tiles are water [if == 0], make them shore.
					if (_borderGrid [borderX] [borderY] == 0) {
						_borderGrid [borderX] [borderY] = 1;
					}
					//If ANY surrounding tiles are not the same as current tile [if != currentTile] and are land, make them earth (2)
					else if (_borderGrid [borderX] [borderY] > 1 && _borderGrid [borderX] [borderY] != _borderGrid[x][y])
						_borderGrid [borderX] [borderY] = 2;						
				}
			}
		}

	}
    */

	// testing - for picking different terrain tiles
	private void OnOptionButtonItemSelected(int index)
	{
		_selectedTerrainTile = index;
	}

	public override void _Process(double delta)
	{
		// Crude placeholder code to disallow input if the mouse is over the UI. Adjust if we change the UI
		if (GetViewport().GetMousePosition().Y < 40 || GetNode<OptionButton>("HUD/OptionButton").ButtonPressed
			|| GetNode<FileDialog>("HUD/SaveDialog").Visible)
		{
			return;
		}
		if (Input.IsActionPressed("ui_select"))
		{
            SetTerrain();
		}
        if (Input.IsActionJustPressed("ui_hide"))
        {
            GetNode<TerrainGridVisualiser>("GridVisualiser").Visible = !GetNode<TerrainGridVisualiser>("GridVisualiser").Visible;
        }
        if (Input.IsActionJustPressed("ui_accept"))
        {
            GD.Print(GetNode<TerrainGrid>("Tilemaps/Level1").CorrectedLocalToMap(GetGlobalMousePosition()));
            // Vector2I localPos = GetNode<TerrainGrid>("Tilemaps/Level1").CorrectedLocalToMap(GetGlobalMousePosition());
            // GD.Print(GetNode<TerrainGrid>("Tilemaps/Level1").CorrectedMapToLocal(localPos));

        }
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            GD.Print(GetGlobalMousePosition());
        }
	}

    // Set the terrain to selected when we click. Loops through all the Grids higher than the selected tile.
    public void SetTerrain()
    {
        Vector2 worldPos = GetGlobalMousePosition() - Position + new Vector2(0, _tileSize.Y/2f);
        // Loop through all of the different terrain tiles in descending order (from highest to lowest), e.g. snow (3), earth (2)...
        foreach (int terrainTile in _terrainGridDict.Keys.OrderByDescending(x => x))
        {
            // If the selected terrain is greater than or equal to the tile we are setting, then we need to make changes to that tilemap
            // E.g. if we are setting earth (2), we will adjust snow and earth tilemaps
            // We tell the Grid script at each tilemap at or above the selected terrain (_tilesGridDict[terrainTile]) that we..
            // .. want to change the terrain to _selectedTerrainTile at worldPos
            if (terrainTile >= _selectedTerrainTile)
            {
                _terrainGridDict[terrainTile].SetGridTerrain(worldPos, _selectedTerrainTile);
                
            }
        }
    }

    // This is called after a signal is emitted to say that a tile is drawn, and needs shore painted around/at it.
    private void OnGridTileAtTerrainBorder(Vector2 worldPos)
    {
        foreach (int terrainTile in _terrainGridDict.Keys.OrderByDescending(x => x))
        {
            GetNode<TerrainGrid>("Tilemaps/Level1").SetGridTerrain(worldPos, 1);
        }
    }

	private void OnSaveDialogConfirmed()
	{

        string suffix = _saveMode == 0 ? ".tscn" : ".tdat";
		string path = GetNode<FileDialog>("HUD/SaveDialog").CurrentPath;
        if (_saveMode == 2)
        {
            UnpackTerrainData(ProjectSettings.GlobalizePath(path));
            return;
        }
		if (!path.EndsWith(suffix))
		{
			path += suffix;
		}
        if (_saveMode == 0)
        {
		    SaveTscn(path);
        }
        else
        {
            SavePackedTerrainData(ProjectSettings.GlobalizePath(path));
        }
	}

    // Save the Terrain node, with child Tilemaps node, and all of its children
	private void SaveTscn(string path)
	{
		PackedScene packedScene = new PackedScene();
		Node terrain = this.Duplicate(0);
        foreach (Node n in terrain.GetChildren())
        {
            n.Free();
        }
        Node tilemaps = GetNode("Tilemaps").Duplicate(0);
        terrain.AddChild(tilemaps);
        tilemaps.Name = "Tilemaps";
        tilemaps.Owner = terrain;

        foreach (Node n in tilemaps.GetChildren())
        {
            n.Free();
        }

        for (int i = 0; i < GetNode("Tilemaps").GetChildCount(); i++)
        {
            Node grid = GetNode("Tilemaps").GetChild(i).Duplicate(0);
            tilemaps.AddChild(grid);
            grid.Name = GetNode("Tilemaps").GetChild(i).Name;
            grid.Owner = terrain;
        }

        // Node unifiedGrid = GetNode<UnifiedGrid>("UnifiedGrid").Duplicate()

		packedScene.Pack(terrain);
		ResourceSaver.Save(packedScene, path);
	}


    // Packs terrain data into a binary file
    private TerrainData SavePackedTerrainData(string path)
    {
        TerrainData terrainData = new TerrainData();
        foreach (TerrainGrid grid in GetNode("Tilemaps").GetChildren())
        {
            terrainData.GridDatas.Add(grid.GetPackedGridData());
        }
        
        JSONDataHandler dataHandler = new();
        dataHandler.SaveToDisk(terrainData, path, false);

        return terrainData;
    }


    // Generates each Grid (tilemap) from each tile type in the tileset.
    // First, generate a grid and set the terrain type for our starting grid (Level1)
    // Then loop through all the tile IDs, and get the name of each tile
    // (by convention, 4 digits, with 0 = water/empty and other number = terrain)
    // Duplicate the starting Grid to make a new Grid from Level2, set the name and terrain type appropriately,
    // add as a child and generate the grid.
    // Connect the signal that tells us when surrounding shore needs to be generated.
    // Finally, add an entry to our tiles-grid dictionary to link the new terrain type to the correct Grid.
    private void GenerateTilemaps()
    {
        GetNode<TerrainGrid>("Tilemaps/Level1").InitGrid(_gridSize);
        GetNode<TerrainGrid>("Tilemaps/Level1").GridTerrain = 1;
        _terrainGridDict[0] = GetNode<TerrainGrid>("Tilemaps/Level1");
        _terrainGridDict[1] = GetNode<TerrainGrid>("Tilemaps/Level1");
        
        TileSetAtlasSource source = (TileSetAtlasSource) _currentTileset.GetSource(0);

        // Start from level 2
        int n = 2;
        foreach (Vector2I atlasCoord in GetNode<TerrainGrid>("Tilemaps/Level1").AtlasTileList)// GetTilesIds())
        {
            TileData data = source.GetTileData(atlasCoord, 0);
            string rules = (string) data.GetCustomData("rules");
            // var data = source.GetTileData()
                if (rules.Contains(n.ToString()))
                {
                    TerrainGrid newTileMap = (TerrainGrid) GetNode<TerrainGrid>("Tilemaps/Level1").Duplicate();
                    newTileMap.AtlasTileList = newTileMap.GenerateAtlasTileList(_currentTileset);
                    newTileMap.CachedTileData = GetNode<TerrainGrid>("Tilemaps/Level1").CachedTileData;
                    newTileMap.Name = "Level" + n;
                    newTileMap.GridTerrain = n;
                    GetNode("Tilemaps").AddChild(newTileMap);
                    newTileMap.InitGrid(_gridSize);
                    newTileMap.AtTerrainBorder+=this.OnGridTileAtTerrainBorder;
                    _terrainGridDict[n] = GetNode<TerrainGrid>("Tilemaps/Level" + n);
                    n+=1;
                }
        }
        // By default, all tiles are set to 0 (water in Level1, empty in higher levels)
        // Render the tiles in Level1 to show the water at the beginning.
        // It may be that we need to render all levels if we start opening and editing terrains.
        GetNode<TerrainGrid>("Tilemaps/Level1").SetAllTileTextures();
    }
    
    // Unpacks terrain data from a binary file
    // TODO - unify with GenerateTilemaps(). To do this, would need to spawn the new grids in GenerateTilemaps rather than
    // starting with a grid and duplicating it.
    // DO as part of godot 4 refactor.
    private void UnpackTerrainData(string fileName)
    {
        PackedScene gridScn = GD.Load<PackedScene>("res://Source/Utils/Terrain/TerrainGrid.tscn");


        JSONDataHandler dataHandler = new();
        TerrainData terrainData = dataHandler.LoadFromJSON<TerrainData>(fileName, false);

        // kill all the current tilemap children
        foreach (TerrainGrid grid in GetNode("Tilemaps").GetChildren())
        {
            grid.Free();
        }
        _terrainGridDict.Clear();

        // Loop through each GridData (containing MainGrid and BorderGrid) in the Terrain Data
        // Make the new Grid tilemap from each one and populate the variables accordingly
        for (int i = 0; i < terrainData.GridDatas.Count; i++)
        {
            TerrainGrid newGrid = (TerrainGrid) gridScn.Instantiate();
            // GetNode<TerrainGrid>("Tilemaps/Level1").CachedTileData;
            newGrid.TileSet = _currentTileset;
            newGrid.AtlasTileList = newGrid.GenerateAtlasTileList(_currentTileset);
            newGrid.CachedTileData = newGrid.GenerateCachedTileData();
            GetNode("Tilemaps").AddChild(newGrid);
            newGrid.Name = "Level" + (i+1);
            GridData gridData = terrainData.GridDatas[i];
            newGrid.MainGrid = gridData.MainGrid;
            newGrid.BorderGrid = gridData.BorderGrid;
            newGrid.SetGridSize(new int[] {gridData.MainGrid.Count, gridData.MainGrid[0].Count});
            newGrid.GridTerrain = i+1;
            
            // only for level 2 and above (i.e. beyond water/shore) should signals go down
            // link terrain appropriately to each grid
            if (i >= 2)
            {
                newGrid.AtTerrainBorder+=this.OnGridTileAtTerrainBorder;
                _terrainGridDict[i] = GetNode<TerrainGrid>("Tilemaps/Level" + i);
            }
        }

        // level 1 is water
        _terrainGridDict[0] = GetNode<TerrainGrid>("Tilemaps/Level1");
        _terrainGridDict[1] = GetNode<TerrainGrid>("Tilemaps/Level1");
        _gridSize = new Vector2I (GetNode<TerrainGrid>("Tilemaps/Level1").MainGrid.Count, GetNode<TerrainGrid>("Tilemaps/Level1").MainGrid[0].Count);

        // Paint all the tilemaps according to grid
        foreach (TerrainGrid grid in GetNode("Tilemaps").GetChildren())
        {
            grid.SetAllTileTextures();
        }
        // Reset camera and the grid lines
		FindAndSetCameraBoundaries();
		RenderGrid();
        GetNode<Cam2DTopDown>("Cam2DTopDown").SetProcess(true);
    }

    private void OnBtnSaveDataPressed()
    {
        _saveMode = 1;
        GetNode<FileDialog>("HUD/SaveDialog").FileMode = FileDialog.FileModeEnum.SaveFile;
        GetNode<FileDialog>("HUD/SaveDialog").Title = "Save Terrain Data";
        GetNode<FileDialog>("HUD/SaveDialog").PopupCentered();
        GetNode<Cam2DTopDown>("Cam2DTopDown").SetProcess(false);
    }
    private void OnSaveDialogShow()
    {
        GetNode<Cam2DTopDown>("Cam2DTopDown").SetProcess(false);
    }

    private void OnSaveDialogHide()
    {
        GetNode<Cam2DTopDown>("Cam2DTopDown").SetProcess(true);
    }



	private void OnBtnSavePressed()
	{
        _saveMode = 0;
        GetNode<FileDialog>("HUD/SaveDialog").FileMode = FileDialog.FileModeEnum.SaveFile;
        GetNode<FileDialog>("HUD/SaveDialog").Title = "Save Scene Data";
		GetNode<FileDialog>("HUD/SaveDialog").PopupCentered();
	}
	private void OnBtnLoadDataPressed()
	{
        _saveMode = 2;
        GetNode<FileDialog>("HUD/SaveDialog").FileMode = FileDialog.FileModeEnum.OpenFile;
        GetNode<FileDialog>("HUD/SaveDialog").Title = "Load Terrain Data";
		GetNode<FileDialog>("HUD/SaveDialog").PopupCentered();
	}

	private void OnBtnQuitPressed()
	{
		GetTree().Quit();
	}
}
