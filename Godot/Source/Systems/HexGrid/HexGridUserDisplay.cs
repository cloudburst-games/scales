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
        hexSprite.Visible = false;
        AddChild(hexSprite);
        _allSprites.Add(hexSprite);
    }

    public void SetSprites(List<Vector2> moveHexes, List<Vector2> halfMoveHexes, List<Vector2> allHexes)
    {
        SetSpriteAnims(allHexes, "Unavailable");
        SetSpriteAnims(moveHexes, "Action");
        SetSpriteAnims(halfMoveHexes, "Move");
    }

    private void SetSpriteAnims(List<Vector2> gridPositions, string anim)
    {
        GetHexesAtGridPositions(gridPositions).ForEach(x => x.GetNode<AnimationPlayer>("Anim").Play(anim));
        // foreach (Vector2 gridPosition in gridPositions)
        // {
        //     Vector2 worldPosition = _grid.GridToWorld(gridPosition);
        //     foreach (Sprite2D sprite in _allSprites)
        //     {
        //         if (sprite.GlobalPosition == worldPosition)
        //         {
        //             sprite.GetNode<AnimationPlayer>("Anim").Play(anim);
        //         }
        //     }
        // }
    }

    private List<Sprite2D> GetHexesAtGridPositions(List<Vector2> gridPositions)
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

    public void ShowAllHexes(List<Vector2> allHexes)
    {
        GetHexesAtGridPositions(allHexes).ForEach(x => x.Visible = true);
    }

    public void ShowContextualHexes(List<Vector2> moveHexes, List<Vector2> actionHexes)
    {
        HideAllHexes();
        GetHexesAtGridPositions(moveHexes).ForEach(x => x.Visible = true);
        GetHexesAtGridPositions(actionHexes).ForEach(x => x.Visible = true);
    }

    public void HideAllHexes()
    {
        foreach (Sprite2D sprite in _allSprites)
        {
            sprite.GetNode<AnimationPlayer>("Anim").Stop();
            sprite.Visible = false;
        }
    }

}