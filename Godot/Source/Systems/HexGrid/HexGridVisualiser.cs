using Godot;
using System.Linq;
using System.Collections.Generic;

public partial class HexGridVisualiser : Node2D
{
    public List<Hexagon> HexesToDraw {get; set;} = new List<Hexagon>();
    private Color _lineColour = new Color(0,0,1);
    private float _width = 5;
    private HexGrid.OrientationMode _orientation = HexGrid.OrientationMode.FlatTop;
    private bool _paintHexes = true;
    private bool _showGrid = true;

    [Export]
    private Texture2D _hexTexture;
    // private bool _isometric;
    public override void _Ready()
    {
        
    }

    public override void _Draw()
    {
        base._Draw();
        foreach (Hexagon hex in HexesToDraw)
        {
            List<Vector2> hexPointPositions = hex.GetPointPositions();
            
            // Paint hexes
            if (_hexTexture != null && _paintHexes)
            {
                hex.HexSprite = PaintHex(hexPointPositions[0], hex.S, hex.O, hex.A);
            }

            if (_showGrid)
            {
                // Draw lines
                for (int i = 0; i < hexPointPositions.Count; i++)
                {
                    if (i == hexPointPositions.Count-1)
                    {
                        DrawLine(hexPointPositions[i], hexPointPositions[0], _lineColour, _width);
                        continue;
                    }
                    DrawLine(hexPointPositions[i], hexPointPositions[i+1], _lineColour, _width);
                }
            }
        }
        _paintHexes = false; // only paint hexes once, unless told otherwise
    }
    
    private Sprite2D PaintHex(Vector2 hexOrigin, float s, float o, float a)
    {
        Sprite2D sprite = new Sprite2D();
        sprite.ZIndex = -1;
        // sprite.Modulate = new Color(1,1,1,0.2f); // debug
        sprite.Texture = _hexTexture;
        sprite.Position = _orientation == HexGrid.OrientationMode.PointyTop ? hexOrigin + new Vector2(0,o + s/2f)
            : hexOrigin + new Vector2(s/2, a);
        sprite.RotationDegrees = _orientation == HexGrid.OrientationMode.PointyTop ? 0 : 90;            
        sprite.Scale *= s/137f; // experiment with "137" depending on the size of the sprite
        AddChild(sprite);
        return sprite;
    }

    public void Start(HexGrid.OrientationMode orientation)
    {
        _orientation = orientation;
        QueueRedraw();
    }

    // TEST METHOD
    public void HighlightHexes(List<Hexagon> hexes, Color colour)
    {
        foreach (Hexagon hex in hexes)
        {
            if (hex.HexSprite != null)
            {
                // hex.HexSprite.QueueFree();
                // hex.HexSprite = PaintHex(hex.GetPointPositions()[0], hex.S, hex.O, hex.A);

                // if (!hex.Obstacle)
                // {
                //     hex.HexSprite.Modulate = new Color(1,1,1);;
                // }
                // else
                // {
                //     hex.HexSprite.Modulate = new Color(0,1,1);
                // }
                hex.HexSprite.Modulate = colour;
            }
        }
    }

    // public void HighlightObstacle(Hexagon hex)
    // {
    //     hex.HexSprite.QueueFree();
    //     if (hex.Obstacle)
    //     {
    //         // Sprite obstacleSprite = new Sprite();
    //         // obstacleSprite.Texture = GD.Load<Texture>("res://HexGrid/example/apple_tree.png");
    //         // hex.HexSprite = obstacleSprite;
    //         // obstacleSprite.ZIndex = 1;
    //         // obstacleSprite.Position = _orientation == HexGrid.OrientationMode.PointyTop 
    //         //     ? hex.GetPointPositions()[0] + new Vector2(0,hex.O + hex.S/2f)
    //         //     : hex.GetPointPositions()[0] + new Vector2(hex.S/2, hex.A)
    //         //     + new Vector2(25, -30);
    //         // obstacleSprite.RotationDegrees += 45;
    //         // AddChild(obstacleSprite);
    //         hex.HexSprite = PaintHex(hex.GetPointPositions()[0], hex.S, hex.O, hex.A);
    //         hex.HexSprite.Modulate = new Color(0,1,1);
    //     }
    //     else
    //     {
    //         hex.HexSprite = PaintHex(hex.GetPointPositions()[0], hex.S, hex.O, hex.A);
    //     }
    // }

    public void ResetDrawing()
    {
        foreach (Node n in GetChildren())
        {
            if (n is Sprite2D s)
            {
                if (s.Modulate != new Color(0,1,1))
                {
                    s.Modulate = new Color(1,1,1);
                }
            }
        }
        // foreach (Hexagon hex in HexesToDraw)
        // {
        //     hex.HexSprite = null;
        // }
        QueueRedraw();
    }

    public void ToggleGrid()
    {
        // _showGrid = !_showGrid;
        Visible = !Visible;
        // Update();
    }
}
