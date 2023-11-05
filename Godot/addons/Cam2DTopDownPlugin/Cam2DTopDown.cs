// Top down camera that can pan, zoom, scroll, and can confine to rhombus or rectangle edges.
using Godot;
using System;

public partial class Cam2DTopDown : Camera2D
{

    [Export]
    private bool _keyToMoveEnabled = true;
    [Export]
    private bool _mouseEdgeToMoveEnabled = true;
    [Export]
    private bool _panToMoveEnabled = true;
    [Export]
    private bool _zoomEnabled = true;
    [Export]
    private Vector2[] _zoomRange = new Vector2[2] {new Vector2(0.5f, 0.5f), new Vector2(2f, 2f)};
    [Export]
    private float _scrollSpeed = 1000f;
    [Export]
    private float _zoomSpeed = 10f;
    [Export]
    public Camera2DEdgeType EdgeType {get; set;} = Camera2DEdgeType.Diamond;
    [Export] // clockwise
    public Vector2[] EdgeCoords = new Vector2[4] {new Vector2(250,0), new Vector2(500, 250), new Vector2(250,500), new Vector2(0, 250)};
    [Export]
    private MouseButton _panMouseButton = MouseButton.Left;

    public enum Camera2DEdgeType { Diamond, Rectangle }

    private Vector2 _prevMousePos;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        DoMovement(delta);
        DoZoom(delta);
	}

    private void DoMovement(double delta)
    {
        Vector2 movement = new();
        Vector2 mousePos = GetViewport().GetMousePosition();
        if (_keyToMoveEnabled)
        {
            movement.Y = Input.IsActionPressed("ui_up") ? -1 : Input.IsActionPressed("ui_down") ? 1 : 0;
            movement.X = Input.IsActionPressed("ui_left") ? -1 : Input.IsActionPressed("ui_right") ? 1 : 0;
        }
        if (_mouseEdgeToMoveEnabled)
        {
            Vector2 screenSize = GetViewportRect().Size;
            movement.Y = mousePos.Y > screenSize.Y * 0.99f ? 1 : mousePos.Y < screenSize.Y * 0.01f ? -1 : movement.Y;
            movement.X = mousePos.X > screenSize.X * 0.99f ? 1 : mousePos.X < screenSize.X * 0.01f ? -1 : movement.X;
        }
        if (_panToMoveEnabled)
        {
            if (Input.IsMouseButtonPressed(_panMouseButton))
            {
                movement = _prevMousePos - mousePos;
            }
        }
        _prevMousePos = mousePos;
        Vector2 nextPos = Position + movement.Normalized() * _scrollSpeed * (float)delta;
        if (!IsAtEdge(nextPos))
        {
            Position = nextPos;
        }
    }

    private void DoZoom(double delta)
    {
        if (_zoomEnabled)
        {
            Vector2 movement = Input.IsActionPressed("ui_zoom_in") || Input.IsActionJustReleased("ui_zoom_in") ? new Vector2(1,1) :
                Input.IsActionPressed("ui_zoom_out") || Input.IsActionJustReleased("ui_zoom_out") ? new Vector2(-1,-1) : new Vector2(0,0);
            if (Input.IsActionPressed("ui_zoom_reset"))
            {
                Zoom = new Vector2(1,1);
            }
            else
            {
                Vector2 nextZoom = Zoom + movement.Normalized() * _zoomSpeed * (float)delta;
                if (nextZoom.X >= _zoomRange[0].X && nextZoom.X <= _zoomRange[1].X && nextZoom.Y >= _zoomRange[0].Y && nextZoom.Y <= _zoomRange[1].Y)
                {
                    Zoom = nextZoom;
                }
            }
        }
    }

    private bool IsAtEdge(Vector2 pos)
    {
        if (EdgeType == Camera2DEdgeType.Rectangle)
        {
            if (pos.X < EdgeCoords[0].X || pos.X > EdgeCoords[1].X || pos.Y < EdgeCoords[0].Y || pos.Y > EdgeCoords[2].Y)
            {
                return true;
            }
        }
        else if (EdgeType == Camera2DEdgeType.Diamond) // explicitly state the condition for ease-of-reading
        {
            // Calculate the origin (centre) based on the edge coordinates
            Vector2 origin = new((EdgeCoords[3].X + EdgeCoords[1].X)/2,(EdgeCoords[0].Y + EdgeCoords[2].Y)/2f);
            // Calculate the radius (distance between centre and edge - for both X and Y)
            Vector2 radii = new(origin.X - EdgeCoords[3].X, origin.Y - EdgeCoords[0].Y);
            // Get the position of the camera relative to the centre,
            // i.e. the distance vector from the centre to the camera position
            Vector2 relativePos = pos - origin;
            // Formula to check if within diamond
            // https://math.stackexchange.com/questions/312403/how-do-i-determine-if-a-point-is-within-a-rhombus
            bool inside = Math.Abs(relativePos.X) * radii.Y + Math.Abs(relativePos.Y) * radii.X <= radii.X * radii.Y;
            return !inside;
        }
        return false;
    }

}