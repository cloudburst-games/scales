// Hold individual grid data (main and border tiles). A component of the TerrainData.
using System.Collections.Generic;

public class GridData : IJSONSaveable
{
    
	public List<List<Dictionary<string,object>>> MainGrid {get; set;} = new List<List<Dictionary<string, object>>>();
	public List<List<byte>> BorderGrid {get; set;} = new List<List<byte>>();


}
