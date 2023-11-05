// Draws an isometric grid for the TerrainBuilder
using Godot;
using System;
using System.Collections.Generic;

public partial class TerrainGridVisualiser : Node2D
{
	private Vector2 _tileSize;
	private Vector2 _gridSize;
	public override void _Ready()
	{
		
	}

	public void SetGrid(Vector2 tileSize, Vector2 gridSize)
	{
		_tileSize = tileSize;
		_gridSize = gridSize;

        // Offset based on horizontal offset (one of the TileSet properties)
        Position = new Vector2(_tileSize.X/2, 0);

		QueueRedraw();
	}

	public override void _Draw()
	{
		Color lineColour = new Color (1, 1, 1);
		float lineWidth = 2;

		float widthIncrement = _tileSize.X / (float)2.0;
		float heightIncrement = _tileSize.Y / (float)2.0;


		for (int y = 0; y < _gridSize.Y + 1; y++ )
		{
			DrawLine (new Vector2 (y * -widthIncrement, y * heightIncrement), new Vector2 (_gridSize.X * widthIncrement - (y * widthIncrement), _gridSize.X * heightIncrement + (y * heightIncrement)), lineColour, lineWidth);

		}

		for (int x = 0; x < _gridSize.X + 1; x++)
		{
			DrawLine (new Vector2 (x * widthIncrement, x * heightIncrement), new Vector2 ( - _gridSize.Y * widthIncrement + (x * widthIncrement), _gridSize.Y * heightIncrement + (x * heightIncrement)), lineColour, lineWidth);

		}
	}
}
