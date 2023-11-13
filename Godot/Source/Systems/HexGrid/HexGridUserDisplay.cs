using Godot;
using System;
using System.Collections.Generic;

public partial class HexGridUserDisplay : Node2D
{
    public enum DisplayMode
    {
        ShowAllHexes,
        ShowContextualHexes,
        HideAllHexes
    }

    [Export]
    private PackedScene _baseHexSprite;

    private HexGrid _grid;

    private Dictionary<Vector2, Hexagon> _validCells = new();
    private List<Sprite2D> _allSprites = new();

    [Export]
    private Color _allColor = new(1, 1, 1);
    [Export]
    private Color _allColorOscillate = new(1, 1, 1);
    [Export]
    private Color _actionColor = new(0, 1, 1);
    [Export]
    private Color _actionColorOscillate = new(0, .5f, .5f);
    [Export]
    private Color _moveColor = new(0, 0, 1);
    [Export]
    private Color _moveColorOscillate = new(0, 0, 0.5f);
    [Export]
    private float _opacity = 0.3f;


    public void Init(HexGrid hexGrid)
    {
        _grid = hexGrid;
        _validCells.Clear();
        foreach (Sprite2D sprite in _allSprites)
        {
            sprite.QueueFree();
        }
        _allSprites.Clear();
        foreach (KeyValuePair<Vector2, Hexagon> kv in hexGrid.Cells)
        {
            if (!kv.Value.Obstacle)
            {
                _validCells.Add(kv.Key, kv.Value);
            }
        }

        foreach (Vector2 k in _validCells.Keys)
        {
            GenerateHexSpriteAt(k);
        }
    }

    private void GenerateHexSpriteAt(Vector2 gridPosition)
    {
        Vector2 worldPosition = _grid.GridToWorld(gridPosition);
        Sprite2D hexSprite = _baseHexSprite.Instantiate<Sprite2D>();
        hexSprite.GlobalPosition = worldPosition;
        hexSprite.Material = (Material)hexSprite.Material.Duplicate(true);
        AddChild(hexSprite);
        _allSprites.Add(hexSprite);

        //debugging:
        // Label lbl = new()
        // {
        //     Text = gridPosition.ToString(),
        //     Scale = new Vector2(2, 2),
        //     GlobalPosition = worldPosition,
        //     ZIndex = 10,
        //     ZAsRelative = false
        // };
        // AddChild(lbl);
    }

    public void SetSprites(List<Vector2> moveHexes, List<Vector2> halfMoveHexes, List<Vector2> allHexes)
    {
        // System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        HideAllHexes();
        SetSpriteColors(allHexes, _allColor, _allColorOscillate);
        SetSpriteColors(moveHexes, _actionColor, _actionColorOscillate);
        SetSpriteColors(halfMoveHexes, _moveColor, _moveColorOscillate);
        // Stop the stopwatch
        // stopwatch.Stop();

        // Print the elapsed time
        // GD.Print($"Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
    }

    private void SetSpriteColors(List<Vector2> gridPositions, Color color1, Color color2)
    {
        foreach (Sprite2D sprite in GetSpritesAtGridPositions(gridPositions))
        {
            ShaderMaterial mat = (ShaderMaterial)sprite.Material;
            mat.SetShaderParameter("color_start", color1);
            mat.SetShaderParameter("color_end", color2);
            mat.SetShaderParameter("alpha", _opacity);
        }
    }

    private List<Sprite2D> GetSpritesAtGridPositions(List<Vector2> gridPositions)
    {
        List<Sprite2D> result = new();
        foreach (Vector2 gridPosition in gridPositions)
        {
            Vector2 worldPosition = _grid.GridToWorld(gridPosition);
            foreach (Sprite2D sprite in _allSprites)
            {
                if (sprite.GlobalPosition == worldPosition)
                {
                    result.Add(sprite);
                }
            }
        }
        return result;
    }

    public void ShowHexes(List<Vector2> hexes)
    {
        foreach (Sprite2D sprite in GetSpritesAtGridPositions(hexes))
        {
            ShaderMaterial mat = (ShaderMaterial)sprite.Material;
            mat.SetShaderParameter("alpha", _opacity);
        }
    }

    public void ShowContextualHexes(List<Vector2> moveHexes, List<Vector2> actionHexes)
    {
        HideAllHexes();
        ShowHexes(moveHexes);
        ShowHexes(actionHexes);
    }

    public void HideAllHexes()
    {
        foreach (Sprite2D sprite in _allSprites)
        {
            ShaderMaterial mat = (ShaderMaterial)sprite.Material;
            mat.SetShaderParameter("alpha", 0f);
        }
    }

}