using Godot;
using System;

public partial class CursorControl : Node
{
    // Load the custom images for the mouse cursor.
    [Export]
    private Resource _select;
    [Export]
    private Resource _melee;
    [Export]
    private Resource _ranged;
    [Export]
    private Resource _spell;
    [Export]
    private Resource _move;
    [Export]
    private Resource _invalid;
    [Export]
    private Resource _wait;
    [Export]
    private Resource _hint;
    private CursorMode _currentCursorMode = CursorMode.Select;

    public enum CursorMode { Select, Melee, Ranged, Spell, Move, Invalid, Wait, Hint }
    public override void _Ready()
    {

        SetCursor(CursorMode.Select);
        // Changes a specific shape of the cursor (here, the I-beam shape).
        // Input.SetCustomMouseCursor(beam, Input.CursorShape.);
    }

    public void SetCursor(CursorMode cursorMode)
    {
        _currentCursorMode = cursorMode;
        switch (cursorMode)
        {
            case CursorMode.Select:
                Input.SetCustomMouseCursor(_select, hotspot: new Vector2(16, 0));
                break;
            case CursorMode.Melee:
                Input.SetCustomMouseCursor(_melee, hotspot: new Vector2(32, 0));
                break;
            case CursorMode.Ranged:
                Input.SetCustomMouseCursor(_ranged, hotspot: new Vector2(32, 0));
                break;
            case CursorMode.Spell:
                Input.SetCustomMouseCursor(_spell, hotspot: new Vector2(16, 16));
                break;
            case CursorMode.Invalid:
                Input.SetCustomMouseCursor(_invalid, hotspot: new Vector2(16, 16));
                break;
            case CursorMode.Move:
                Input.SetCustomMouseCursor(_move, hotspot: new Vector2(16, 16));
                break;
            case CursorMode.Wait:
                Input.SetCustomMouseCursor(_wait, hotspot: new Vector2(16, 16));
                break;
            case CursorMode.Hint:
                Input.SetCustomMouseCursor(_hint, hotspot: new Vector2(16, 16));
                break;
        }
    }

    public CursorMode GetCursor()
    {
        return _currentCursorMode;
    }
}