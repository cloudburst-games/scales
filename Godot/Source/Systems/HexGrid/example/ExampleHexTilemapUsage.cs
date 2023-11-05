// script demonstrating usage when using hexgrid with tilemap (usually to generate a hex grid on top of a tilemap)
using Godot;
using System;

public partial class ExampleHexTilemapUsage : Node
{

    private TileMap _terrain;

    private HexGrid _hexGrid;
    private HexTilemapIsometricInterface _hexTileInterface;

    public override void _Ready()
    {
        _terrain = GetNode<TileMap>("TerrainBuilder/Tilemaps/Level1");
        _hexGrid = GetNode<HexGrid>("HexGrid");
        _hexTileInterface = new HexTilemapIsometricInterface(_hexGrid, _terrain);
        _hexGrid.AddChild(_hexTileInterface);
        _hexTileInterface.FinishedMarkingObstacles += this.UpdateNavDisplay;

        _hexGrid.Start(_hexTileInterface.GetHexGridBoundsFromTilemap()[0], _hexTileInterface.GetHexGridBoundsFromTilemap()[1], HexGrid.ConstructionMode.WorldSize);

        _hexTileInterface.MarkAllHexObstacles(1);
        UpdateNavDisplay();

    }

    private void UpdateNavDisplay()
    {
        _hexGrid.UpdateNavigationAndDisplay();
    }

    // public override void _Input(InputEvent ev)
    // {
    //     if (ev is InputEventMouseButton btn)
    //     {
    //         if (btn.ButtonIndex == MouseButton.Left && btn.Pressed)
    //         {
    //             Vector2 mousePos = GetGlobalMousePosition();
    //             GD.Print("Hex is an obstacle: ", _hexGrid.GetHexAtWorldPosition(mousePos).Obstacle);
    //         }
    //         else if (btn.ButtonIndex == MouseButton.Middle && btn.Pressed)
    //         {
    //             _hexTileInterface.MarkAllHexObstacles();
    //             UpdateNavDisplay();
    //         }
    //     }
    // }

    // public void Die()
    // {
    //     _hexTileInterface.Die();
    // }
}
