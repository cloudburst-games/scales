// Tool to generate tilesets from Tilesheet with associated JSON
// Tested with the FOSS free texture packer tool: http://free-tex-packer.com/
using Godot;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public partial class TilesetGenerator : Node
{
    // Data container for tile atlas JSON
	private TextureAtlasData _tileAtlasData = new TextureAtlasData();
    [Export]
	private Resource _tileAtlas = ResourceLoader.Load("res://Source/Utils/Terrain/Examples/ExampleTilesheet/Tilesheet.png");

    [Export]
    private int _animFPS = 5;

    [Export]
    private Vector2I _tileSize = new Vector2I(128,64);

    public override void _Ready()
    {
        PopulateTileAtlasData();
        GetNode<FileDialog>("FileDialog").PopupCentered();
    }

    // Get the data from the tilesheet JSON and place it into the _tileAtlasData var
    private void PopulateTileAtlasData()
    {
        string JSONpath = ProjectSettings.GlobalizePath(_tileAtlas.ResourcePath);
        JSONpath = JSONpath.Substring(0,JSONpath.Length-3);
        JSONpath += "json";
		// // Read and convert the JSON file holding terrain atlas data
		string tileData = System.IO.File.ReadAllText (JSONpath);
		JsonConvert.PopulateObject(tileData,_tileAtlasData);
    }

	// This method generates the terrain tiles from the json file.
	public void MakeTileSet(string path)
	{
        // Make the tileset
        TileSet tileset = new TileSet();
        tileset.TileSize = _tileSize;
        tileset.TileShape = TileSet.TileShapeEnum.Isometric;
        tileset.TileLayout = TileSet.TileLayoutEnum.DiamondDown;
        
        tileset.AddCustomDataLayer(0);
        // Make source
        TileSetAtlasSource source = new();
        tileset.AddSource(source);
        // Set the source texture
        source.Texture = (Texture2D) _tileAtlas;
        source.TextureRegionSize = _tileSize * 2 ;//new Vector2I(256,128);
        // Add and set the custom data layer to contain our transition rules
        tileset.AddCustomDataLayer(0);
        tileset.SetCustomDataLayerName(0, "rules");
        tileset.SetCustomDataLayerType(0, Variant.Type.String);

        // Store the animated tiles that we have already made (otherwise would loop over and make for each frame)
        List<string> spriteGroupsFinished = new List<string>();
        // Get a list of all the rules; will be using this when setting animated tiles
        List<string> rulesList = MakeRulesListFromTextureAtlasData(_tileAtlasData);


        // Loop through each frame (tile data container) in the JSON data
		foreach (TextureAtlasData.Frame fr in _tileAtlasData.frames)
		{
            // 1. Get the name - this is our transition rules for the tile
            string name = fr.filename.Split ('.') [0];

            // 2. Translate the position on the PNG to an atlasCoord (e.g. [512,256] becomes [2, 2]). This is 
            //  how Godot finds the tile on the TileSet that is generated
            Vector2I atlasCoord = new Vector2I(fr.frame["x"]/source.TextureRegionSize.X,fr.frame["y"]/
                source.TextureRegionSize.Y);

            // 3. Do not generate duplicate tiles
            if (source.HasTile(atlasCoord))
            {
                continue;
            }

            // 4. And skip animated tiles for now - we will be doing them separately. We only add the first frame
            //  here. The rules data is attached to this frame and it is selected when the tile is painted.
            if (name.Length > 6)
            {
                if (name[7] != '1')
                {
                    continue;
                }
            }
            // 5. Create the non-animated tile with the specified coordinate. Note in Godot 4 TileSet, the Atlas 
            //  coordinate doubles as the ID
            source.CreateTile(atlasCoord);

            // 6. Now we need to attach the tile rules to the tile. In Godot 4 each tile can have data attached.
            //  We make a custom tile data called "rules" and attach the transition rules (name).
            //  Then when the terrain is painted, the correct tile can be selected from the rules.
            TileData data = source.GetTileData(atlasCoord,0);
            data.SetCustomData("rules", name.Length > 6 ? name.Substring(0,6) : name);
			
            // 7. Animated tiles have more than 6 characters e.g. 0000-a-1
            if (name.Length > 6)
            {
                // Animated tiles are grouped together by the first 6 chars, e.g. 0000-a is a spritegroup.
                // They are all dealt with together. So if another tile in the same spritegroup is iterated
                //  over, then it is skipped as it was already processed.
                if (spriteGroupsFinished.Contains(name.Substring(0, 6)))
                {
                    continue;
                }

                string spriteGroup = name.Substring(0, 6);

                // For each group of animated tiles, we find the first tile, calculate the number of frames,
                //  then attach this data to the first tile (atlasCoord) - so it is treated as an animated
                //  tile with the right no. frames. The frames must be horizontally aligned - in the PNG.
                if (name[7] == '1')
                {
                    int numberOfFrames = GetNumberOfTilesInSpriteGroup(rulesList, spriteGroup);
                    source.SetTileAnimationColumns(atlasCoord, numberOfFrames);
                    source.SetTileAnimationFramesCount(atlasCoord, numberOfFrames);
                    source.SetTileAnimationSpeed(atlasCoord,_animFPS);
                }

                spriteGroupsFinished.Add(spriteGroup);

            }
            
		}

		ResourceSaver.Save (tileset, path + ".tres");

        GetNode<Button>("BtnDone").Visible = true;
	}

    // Simply gets the number of tiles in a spritegroup from the list of rules
    public int GetNumberOfTilesInSpriteGroup(List<string> rulesList, string spriteGroup) // 5 chars
    {
        int res = 0;
        foreach (string s in rulesList)
        {
            if (s.Length > 6)
            {
                if (s.Substring(0, 6) == spriteGroup)
                {
                    res += 1;
                }
            }
        }
        return res;
    }

    public List<string> MakeRulesListFromTextureAtlasData(TextureAtlasData data)
    {
        List<string> result = new();
        foreach (TextureAtlasData.Frame fr in data.frames)
        {
            result.Add(fr.filename.Split ('.') [0]);
        }
        return result;
    }

    public void OnFileDialogConfirmed()
    {
        string path = GetNode<FileDialog>("FileDialog").CurrentPath;
        MakeTileSet(path);
    }

    public void OnBtnDonePressed()
    {
        GetTree().Quit();
    }
}
