// Holds TerrainData to be saved or loaded when constructing terrain. Composed of a collection of GridData.
using System.Collections.Generic;

public class TerrainData : IJSONSaveable
{
    public List<GridData> GridDatas = new List<GridData>();
}
