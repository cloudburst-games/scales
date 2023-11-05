// This is where all of the grid logic is calculated and terrain tiles are painted.
// It follows slightly different rules for each terrain
// E.g. water and shore are drawn at the base (so this tilemap is at the bottom), and earth draws empty tiles if 0000
using Godot;
using System;
using System.Collections.Generic;

public partial class TerrainGrid : TileMap
{

    // Each Level of terrain, e.g. water+shore, earth, snow, contains its own grid storing terrain data
    // It also contains its own bordergrid storing border terrain data for calculating terrain transitions
	public List<List<Dictionary<string,object>>> MainGrid {get; set;} = new List<List<Dictionary<string, object>>>();
	public List<List<byte>> BorderGrid {get; set;} = new List<List<byte>>();

    // This is set when the Grid is initialised, synchronised with all other Grids
    private int[] _gridSize;

    // This is the terrain type assigned to this Grid
    public int GridTerrain {get; set;}
    private Random _rand = new Random();

    public List<Vector2I> AtlasTileList {get; set;}
    public Dictionary<Vector2I, string> CachedTileData {get; set;} = new();

    // This signal is called when terrain is set at higher level Grids (e.g. earth, snow), and there is an empty border
    // This is so that we know to ask the Level1 Grid (water+shore) to set shore at those areas, otherwise there would be ..
    // ... an unnatural border with water (e.g. earth -> water)
    [Signal]
    public delegate void AtTerrainBorderEventHandler(Vector2 worldPos);
	public override void _Ready()
	{
        
	}

    // This is to improve performance. We make a dictionary of the atlas coord to tile rules,
    //  so we don't need to keep getting tile data from each tile, which is a very slow operation.
    public Dictionary<Vector2I, string> GenerateCachedTileData()
    {
        Dictionary<Vector2I, string> result = new();
        TileSetAtlasSource source = (TileSetAtlasSource) TileSet.GetSource(0);
        foreach (Vector2I tile in AtlasTileList)
        {
            TileData data = source.GetTileData(tile, 0);
            string tileRules = (string) data.GetCustomData("rules");
            result[tile] = tileRules;
        }
        return result;
    }

    // Generate a list of tile coordinates, so we don't have to keep finding it - to save performance.
    public List<Vector2I> GenerateAtlasTileList(TileSet tileset)
    {
        List<Vector2I> result = new();
        TileSetAtlasSource source = (TileSetAtlasSource) tileset.GetSource(0);
        for (int x = 0; x < source.GetAtlasGridSize().X; x++)
        {
            for (int y = 0; y < source.GetAtlasGridSize().Y; y++)
            {
                Vector2I pos = new Vector2I(x, y);
                if (source.HasTile(pos))
                {
                    Vector2I tile = source.GetTileAtCoords(pos);
                    if (!result.Contains(tile))
                    {
                        result.Add(tile);
                    }
                }
            }
        }
        return result;
    }

    public void InitGrid(Vector2I gridSize)
    {
        _gridSize = new int[2] {gridSize.X, gridSize.Y};
		MakeMainGrid(new Vector2(gridSize[0], gridSize[1]));
    }

    public void SetGridSize(int[] gridSize)
    {
        _gridSize = gridSize;
    }


	private void MakeMainGrid(Vector2 size)
	{
		int id = 0;

		for (int x = 0; x < size.X; x++)
		{

			// Make a column list
			List<Dictionary<string, object>> column = new List<Dictionary<string, object>> ();
			// Add the column list to the grid list
			MainGrid.Add (column);

			// While we are looping for each column, we want to add cells (i.e. rows) to the column list. So we nest a loop within this loop.
			// For each row we want to make (x = cols, y = rows)
			for (int y = 0; y < size.Y; y++)
			{
				// Make a dictionary containing the cell data
				Dictionary<string, object> cell = new Dictionary<string, object> ();
				// Add the dictionary to the column list we are currently looping through
				column.Add (cell);

				// Add any data we want to the cell dictionary
				// E.G. cellDict.Add("Obstacle", true)
				cell ["ID"] = id;

				// By default, terrain is all water
				cell["Terrain"] = "0000";

				id+=1;
			}
		}
		
		MakeBorderGrid();
	}


	private void MakeBorderGrid()
	{

		// For tile transitions, we make a separate border grid. THis is the same as the grid except it contains two additional columns and rows.
		// No dictionaries as we ONLY store terrain data here.
		// For the border grid, we set some rules, e.g. grass can only have shore adjacent, and water can only have shore adjacent
		// If the border grid showed tiles as follows, with g = grass, sn = snow, w = water, we can end up with this:
		//
		//	g | g | g | g
		//	-	-	-	-
		//	s | s | s | s
		//	-	-	-	-
		//	w | w | w | w

		// We then use these borders to calculate our individual tile composition using our main grid:
		// gg | gg | gg
		// ss | ss | ss
		// --	--	 --
		// ss | ss | ss
		// ww | ww | ww

		// As this shows, the border grid needs to be bigger than the actual grid.
		// Calculating transitions this way reduces number of tile permutations and tiles that need to be drawn
		// For ease of calculation, we use bytes to represent terrain types. 0 = water. 1 = shore. 2 = earth. 3 = grass. 4 = snow. etc.

		for (int col = 0; col < GetMainGridSize().X + 2; col++)
		{
			// Make a border row list
			List<byte> borderColList = new List<byte> ();

			// Add it to the border grid list:
			BorderGrid.Add (borderColList);
			for (int row = 0; row < GetMainGridSize().Y + 2; row++)
			{
				borderColList.Add (0);
			}
		}
	}


	// Console -for debugging
	public void PrintGrid()
	{
		foreach (List<Dictionary<string, object>> x in MainGrid)
		{
			string y = "";
			foreach (Dictionary<string,object> cell in x)
			{
				y += (string)cell["Terrain"] + "|";
			}
			GD.Print(y);
			GD.Print("------------------------------------------------------------");
		}
	}
    // Console -for debugging
	public void PrintBorderGrid()
	{
		foreach (List<Byte> x in BorderGrid)
		{
			string y = "";
			foreach (byte cell in x)
			{
				y += cell + "|";
			}
			GD.Print(y);
			GD.Print("------------------------------------------------------------");
		}
	}

	private Vector2 GetMainGridSize()
	{
		return new Vector2(MainGrid.Count, MainGrid[0].Count);
	}


	// Sets the selected tile onto the border grid (the larger grid), which contains a cell for each quarter of the grid cell.
	// Adjusts the border grid according to border rules (e.g. water (0) only next to shore (1), grass only next to earth or shore)
	// Then creates a grid tile from the border grid by joining each quarter together.
	// E.g.
	// 0 | 1
	// 0 | 1
	// .. becomes -> 0011 on the grid

    // This method is called from Terrain.cs when trying to set terrain.
    // It can be called if the terrain type being set is displayed at a lower tilemap (i.e. below) this tilemap.
    // It can also be called if the terrain type being set is at this tilemap.
	public void SetGridTerrain(Vector2 worldPos, int selectedTerrainTile, bool atBorders = false)
	{
        // First we need to remember if we are setting water.
        // This is because when setting water, we need to make higher surrounding tiles empty, to prevent them bordering on water.
        bool water = selectedTerrainTile == 0;

        // If the terrain type of this Grid is displayed over the terrain we are trying to set, we need to set this...
        // terrain to empty (0).
        // Note 0 erases terrain unless we are on the water/shore Grid, in which case it sets water.
        // For example, if we are setting earth (2), then at the snow Grid (3) it will set to empty instead.
        // This is so that snow would not be displayed above earth
        if (GridTerrain > selectedTerrainTile)
        {
            selectedTerrainTile = 0;
        }

        // We use grid coordinates for TileMap operations
        Vector2I gridPos = CorrectedLocalToMap(worldPos);
        
		if (!GridContains(gridPos) && !atBorders)
		{
			// GD.Print("Invalid grid position");
			return;
		}
		// GD.Print(gridPos);
		int x = (int)gridPos.X;
		int y = (int)gridPos.Y;

		// If we are at the right or bottom edges, then extend the grid calculation beyond the border.
		// This prevents the edges being locked to shore (because at the start, all the tiles are by default set to water)
		if (x == GetMainGridSize().X - 1)
		{
			SetGridTerrain(CorrectedMapToLocal(new Vector2I(x+1, y)), selectedTerrainTile, true);
		}
		if (y == GetMainGridSize().Y - 1)
		{
			SetGridTerrain(CorrectedMapToLocal(new Vector2I(x, y+1)), selectedTerrainTile, true);
		}

        
		// We set the selected coordinate on the grid to the selected tile
		BorderGrid[x][y] = (byte) selectedTerrainTile;
		
		// We make an array containing the 8 surrounding grid coordinates so we can calculate transitions
		Vector2I[] calcList = new Vector2I[8] { 
			new Vector2I (gridPos.X-1, gridPos.Y-1),
			new Vector2I (gridPos.X-1, gridPos.Y) ,
			new Vector2I (gridPos.X-1, gridPos.Y+1) ,
			new Vector2I (gridPos.X, gridPos.Y-1) ,
			new Vector2I (gridPos.X, gridPos.Y+1) ,
			new Vector2I (gridPos.X+1, gridPos.Y-1) ,
			new Vector2I (gridPos.X+1, gridPos.Y) ,
			new Vector2I (gridPos.X+1, gridPos.Y+1)
		};


		// We call this method to set surrounding tiles according to transition rules
		CalcTerrainBorders (gridPos, calcList, water);

		// For each of the surrounding grid coordinates in the above array
		for (int i = 0; i <= calcList.GetUpperBound(0); i++)
		{
			// If the grid contains these grid coordinates, we generate terrain data from the border list into the actual grid
			if (GridContains (calcList [i])) {
				CalcBorderTilesToGrid (calcList [i]);
			}
		}
		// And don't forget to also do this for the selected grid coordinate (the central tile)
		CalcBorderTilesToGrid (gridPos);

        // And make sure the grid coordinate is also not a water tile
        // This sets the terrain at Level1 to shore whenever we set a tile, so that e.g. snow does not appear to be floating on water
        EmitSignal(SignalName.AtTerrainBorder, CorrectedMapToLocal(gridPos));

		SetTileGroup(gridPos);

		// For debugging
		// PrintGrid();
	}

	// This method sets surrounding tiles according to transition rules.
    // These transition rules are for direct transitions (i.e. like in age of empires, HOMM4)
    // Can easily change this code if we want to go for border transitions, such as in HOMM2/3.
    // This would only need a single tilemap, but many more tiles.
	private void CalcTerrainBorders(Vector2 gridPosition, Vector2I[] calcList, bool water)
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
				// If current tile is empty/water: 
				if (BorderGrid[x] [y] == 0)
				{
                    // If the bordering tile is not empty/water, and we are currently setting water,
                    // and we are working on a Grid other than the shore+water Grid,
                    // we need to make that tile empty, so that water is only bordered by shore or other water.
                    // Note shore will appear instead, because when we set non-water/shore tiles, shore is always painted beneath.
                    if (BorderGrid [borderX] [borderY] != 0 && GridTerrain != 1 && water)
                    {
                        BorderGrid [borderX] [borderY] = 0;
                    }
				}
				// If current tile is not empty/water
				else
				{   
                    // If the bordering tile is empty and we are not on the shore+water Grid, then the bordering tile needs to have shore painted beneath.
                    if (BorderGrid [borderX] [borderY] == 0 && GridTerrain != 1)
                    {
                        EmitSignal(SignalName.AtTerrainBorder, CorrectedMapToLocal(calcList[i]));
                    }					
				}
			}
		}

	}

    // Helper method, tells us if there is terrain at the specified position. Currently unused.
    public bool IsTerrainAtWorldPos(Vector2I worldPosition)
    {
        Vector2 gridPos = CorrectedLocalToMap(worldPosition);
        if (BorderGrid[(int)gridPos.X][(int)gridPos.Y] == 0)
        {
            return false;
        }
        return true;
    }


	// Generate terrain data from the border grid into the actual grid.
	// This method gets four surrounding tiles and runs them through BordersToGrid, as we need to be extensive in our border calculations (otherwise would look wrong)
	private void CalcBorderTilesToGrid(Vector2 gridPosition)
	{

		// Make a list for the four tiles we need to generate terrain data (the tile, upper left, upper, and left).
		Vector2[] calcList = new Vector2[4] { 
			new Vector2 (gridPosition .X-1, gridPosition .Y-1) , // upper left
			new Vector2 (gridPosition [0], gridPosition [1]-1) , // left
			new Vector2 (gridPosition [0]-1, gridPosition [1]) , // upper
			new Vector2 (gridPosition [0], gridPosition [1]) // selected coordinate
		};

		// Loop through this list and if the grid coordinates are in the grid, then generate the terrain data for the selected coordinate using the BordersToGrid method.
		for (int i = 0; i <= calcList.GetUpperBound(0); i++)
		{
			if (GridContains (calcList [i])) 
            {
                // GD.Print(calcList[i]);
				BordersToGrid (calcList [i]);
			}
		}
	}

	// Generate the terrain data from the selected coordinate
	// Each tile in the actual grid corresponds to four tiles in the border grid (see explanation and diagram above)
	// E.g. if border grid has four tiles - w / s / w / s, the actual grid terrain key would be wsws. 
	// We use bytes to represent this so really it would be 0101
	// We can then use grid[row][col]["Terrain"] to place our tiles
	private void BordersToGrid(Vector2 gridPosition)
	{
		// For ease we represent x and y separately
		int x = (int)gridPosition.X;
		int y = (int)gridPosition.Y;


        // GD.Print(gridPosition);
        // GD.Print(BorderGrid [x] [y], BorderGrid [x+1][y], BorderGrid [x][y+1], BorderGrid [x+1][y+1]);

		// Get the separate tiles from the border grid.
		byte topLeft = BorderGrid [x] [y];
		byte topRight = BorderGrid [x+1][y];
		byte botLeft = BorderGrid [x][y+1];
		byte botRight = BorderGrid [x+1][y+1];

		// Combine them to make the terrain tile
		string fourTiles = (topLeft.ToString() + topRight.ToString() +
			botLeft.ToString() + botRight.ToString());

		// Assign this
		MainGrid [x] [y] ["Terrain"] = fourTiles;
	}

	public bool BorderGridContains(Vector2 gridPos)
	{
		if (gridPos.X < GetMainGridSize().X+2 && gridPos.X >= 0)
		{
			if (gridPos.Y < GetMainGridSize().Y+2 && gridPos.Y >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private bool GridContains(Vector2 gridPos)
	{
		if (gridPos.X < MainGrid.Count && gridPos.Y < MainGrid[0].Count
			&& gridPos.X >= 0 && gridPos.Y >= 0)
		{
			return true;
		}
		return false;
	}

    // Currently unused
	private bool GridContainsMinusEnd(Vector2 gridPos)
	{
		if (gridPos.X < MainGrid.Count -1 && gridPos.Y < MainGrid[0].Count -1
			&& gridPos.X >= 0 && gridPos.Y >= 0)
		{
			return true;
		}
		return false;
	}

	// Find the surrounding 15 tiles from the central tile to pass into the SetTile method.
	// When border transitions are calculated in CalcTerrainBorders, surrounding tile IDs are changed.
	// So when placing a tile, need to change texture for all surrounding tiles (surrounding 15)
	private void SetTileGroup(Vector2I gridPos)
	{
		// Make an array size 16 containing the central tile and all surrounding tiles
		Vector2I[] calcArray = new Vector2I[16] { 

			new Vector2I (gridPos [0]-2, gridPos [1]-2),
			new Vector2I (gridPos [0]-2, gridPos [1]-1),
			new Vector2I (gridPos [0]-2, gridPos [1]),
			new Vector2I (gridPos [0]-2, gridPos [1]+1),
			new Vector2I (gridPos [0]-1, gridPos [1]-2),
			new Vector2I (gridPos [0], gridPos [1]-2),
			new Vector2I (gridPos [0]+1, gridPos [1]-2),
			new Vector2I (gridPos [0]-1, gridPos [1]-1),
			new Vector2I (gridPos [0]-1, gridPos [1]),
			new Vector2I (gridPos [0]-1, gridPos [1]+1),
			new Vector2I (gridPos [0], gridPos [1]-1),
			new Vector2I (gridPos [0], gridPos [1]+1),
			new Vector2I (gridPos [0]+1, gridPos [1]-1),
			new Vector2I (gridPos [0]+1, gridPos [1]),
			new Vector2I (gridPos [0]+1, gridPos [1]+1),
			new Vector2I (gridPos [0], gridPos [1])
		};

		// Loop through this array and set tile for each
		for (int i = 0; i <= calcArray.GetUpperBound(0); i++)
		{
			SetTile (calcArray [i]);
		}
	}


	// Set the tile texture for the specified position according to the data in the Grid ([x][y]["Terrain"])
	private void SetTile(Vector2I gridPos)
	{
		// If the grid does not contain the specified position then return (cannot place tile - otherwise would return an error)
		if (!GridContains (gridPos)) {
			//GD.Print (WorldToMap (pos));
			return;
		}
		SetTileTex (gridPos);
        
	}

	// For an existing sprite, we set the region to the correct tile on the tile texture atlas according to the position on the grid
	private void SetTileTex(Vector2I gridPos)//, Sprite sprite)
	{
        
		string terrain = (string) MainGrid[(int)gridPos.X][(int)gridPos.Y]["Terrain"];
        
        // 0000 tiles are empty unless painting shore/water. Otherwise they are water tiles.
        if (Name != "Level1" && terrain == "0000")
        {
            SetCell(0, gridPos);
            return;
        }
		
        List<string> tileNamesToSearch = new List<string>();

        // If the terrain does not exist in its basic form (e.g. 0000 does not exist in the tileset, but variations
        // such as 0000-a do), then do not add it to the tilenames to be used.
        if (GetTileByRules(terrain) != new Vector2I(-1, -1))
        {
            tileNamesToSearch.Add(terrain);
        }

        // Check for the existence of terrain variations from 'a' to 'z', and if they exist,
        //  add them to the selection pool.
        // 'a'-'z' are appended to tile rules if surrounding tiles are expected to change.
        for (char c = 'a'; c <= 'z'; c++)
        {
            string terrainToCheck = String.Format("{0}-{1}", terrain, c);

            if (GetTileByRules(terrainToCheck) != new Vector2I(-1, -1))
            {
                tileNamesToSearch.Add(terrainToCheck);
            }

        }
        
        // Select a tile to paint based on eligible tiles.
        string targetRules = tileNamesToSearch[_rand.Next(0, tileNamesToSearch.Count)];
        // Find the appropriate tile using the tile rules.
        Vector2I tileToPlace = GetTileByRules(targetRules);
        SetCell (0, gridPos, 0, tileToPlace);
	}

    public Vector2I GetTileByRules(string targetRules)
    {
        foreach (Vector2I tile in AtlasTileList)
        {
            string tileRules = CachedTileData[tile];
            if (tileRules == targetRules)
            {
                return tile;
            }
        }
        return new Vector2I(-1, -1);
    }

	public void SetAllTileTextures()
	{
		for (int row = 0; row < GetMainGridSize().Y; row++)
		{
			for (int col = 0; col < GetMainGridSize().X; col++)
			{
				Vector2I gridPosition = new Vector2I (col, row);
				// return if we are out of the grid extents (including right and bottom edges)
				if (!GridContains (gridPosition))
					return;
				SetTileGroup (gridPosition); // And places the sprite
			}
		}
	}

    // Pack the Grid and BorderGrid into GridData for saving and loading
    public GridData GetPackedGridData()
    {
        GridData gridData = new GridData();
        gridData.BorderGrid = BorderGrid;
        gridData.MainGrid = MainGrid;
        return gridData;
    }

    // Unpack the grid data onto this grid
    public void UnpackGridData(GridData gridData)
    {
        BorderGrid = gridData.BorderGrid;
        MainGrid = gridData.MainGrid;
    }

    // This is not really required at present, but if modifications to the method are needed it can be adjusted.
    public Vector2I CorrectedLocalToMap(Vector2 localPosition)
    {
        Vector2 localCorrection = new Vector2(TileSet.TileSize.X/2, 0);
        return LocalToMap(localPosition);// + localCorrection);
        // ))
        // {
        //     GD.Print(GetNode<TerrainGrid>("Tilemaps/Level1").LocalToMap(GetGlobalMousePosition() + 
        //         new Vector2(_tileSize.X/2, 0)));
    }

    public Vector2 CorrectedMapToLocal(Vector2I mapPosition)
    {
        Vector2 localCorrection = new Vector2(-TileSet.TileSize.X/2, 0);
        return MapToLocal(mapPosition);// + localCorrection;
        // ))
        // {
        //     GD.Print(GetNode<TerrainGrid>("Tilemaps/Level1").LocalToMap(GetGlobalMousePosition() + 
        //         new Vector2(_tileSize.X/2, 0)));
    }
}
