Working.kra:
1. Set the grid angles to 26.5650 and cell spacing to 460 px
2. Paint the layers. In the template there is Shore+Water, Snow, Dirt, but you can add whichever layer you like, e.g. if you wanted to add Grass.
- Check carefully that the tile works well with other tiles. You can see specific tiles using the layers inside the DiamondAreas group to select them. You can try in a separate file or temporarily to connect them with various other tiles to see how they look.

Output.kra
3. Place individual tiles inside Output.kra
- Use the layers inside the DiamondAreas group to select the area for a specific tile.
- Click on the relevant layer, e.g. Shore+Water, and copy the tile.
- In Output.kra, under the relevant group (make a new group if needed), make a new layer and paste the tile.
- Resize the tile as it will be too big for Output.kra: Layer -> Transform -> Resize Layer. Enable the chain on the right, set Filter to Nearest Neighbour, and set height to 256 pixels.
- Name the layer depending on the tile, with numbers going from top -> right -> left -> bottom. See NUMBERING_EXPLANATION.txt.
- If you are making multiple of the same tile type, you can add -b or -c etc. and the terrain script will randomly select one of these tiles.
- If you are making an animated tile, add -1, -2, -3, etc. at the end of the name for each tile that comprises the animation. E.g. the example animated water tile is made up of 0000-a-1, 0000-a-2, 0000-a-3, 0000-a-4 in Output.kra.

Exporting and making spritesheet
4. Export.
- Tools -> Scripts -> Export Layers
- Select the Output.kra document
- Choose a directory to export to
- Set image extensions to PNG
- Click OK when ready
5. Make the spritesheet
- Open free texture packer
- Uncheck allow rotation and uncheck allow trim
- Set width and height as you like
- Set padding to 0
- Set Texture format to png
- Change Format to JSON (array)
- Export when ready
- IMPORTANT: animated tiles need to be HORIZONTALLY aligned on the exported tilesheet. To see how it should be done, look at the example Tilesheet.png (first four tiles are animated water tile)

Test the spritesheet in Godot:
6. Test: make the tileset in Godot from the spritesheet
- Inside the project directory, make a new folder for your tileset in Utils/Terrain, e.g. Utils/Terrain/MyNewTileset
- Drag your spritesheet png and json file to your tileset folder that you just made
- Open the project in Godot
- Search files for the node 'TilesetGenerator.tscn' and open it
- Click on the root node (TilesetGenerator) on the left SceneTree area
- Drag the spritesheet png to the Tile Atlas box on the right
- Set the FPS to the desired FPS of the animations.
- Press F6 to play the scene
- Type a filename to save the spritesheet as a tileset and click OK then click DONE
7. Test: test out your new tileset
- Make sure you have the project opened in Godot
- Search files for the node 'TerrainBuilder.tscn' and open it
- Click on the root node (Terrain) on the left SceneTree area
- Drag your new tileset (.tres) file into 'Current Tileset'
- Press F6 to play the scene
- Try painting each terrain from the top left (click water+shore for layer 1, earth for layer 2, etc.). It will crash if you try to paint a layer that you haven't made.

When all done, place your completed spritesheet (with the json) in Assets/Graphics/Tiles/YourNewTileset, and push to the git repo