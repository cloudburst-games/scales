using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AdventureSelectionState : SelectionState
{
    public AdventureSelectionState(Selection selection)
    {
        this.Selection = selection;
    }
    
    private List<CharacterUnit> _selectedCharacters = new();
    private bool _drawSelectionRect = false;
    private Vector2 _start = new();
    private Vector2 _end = new();
    private Rect2 _drawRect = new();
    private bool _mouseBtnHeldDown = false;

    public override void UnhandledInputUpdate(InputEvent ev)
    {
        base.UnhandledInputUpdate(ev);

        if (ev is InputEventMouseButton btn)
        {

            if (btn.ButtonIndex == MouseButton.Left)
            {
                // TEMP FOR DEWBUG:
                Selection.GetNode<TextureRect>("marker").Visible = false;
                ////
                if (btn.Pressed)
                {
                    if (!Input.IsPhysicalKeyPressed(Key.Shift))
                    {
                        ClearSelectedCharacters();
                    }
                    _start = Selection.GetGlobalMousePosition();
                    _mouseBtnHeldDown = true;
                }
                else
                {

                    foreach (CharacterUnit characterUnit in PlayerCharacters)
                    {
                        if (_drawRect.Abs().HasPoint(characterUnit.GlobalPosition))
                        {
                            OnPlayerCharacterClicked(characterUnit, true);
                        }
                    }
                    _mouseBtnHeldDown = false;
                }
            }
        }
    }

    public override void InputUpdate(InputEvent ev)
    {
        if (ev.IsEcho())
        {
            return;
        }
        if (ev is InputEventMouseButton btn && btn.IsPressed())
        {
            if (btn.ButtonIndex == MouseButton.Right)
            {
                ////
                // TODO _REPLACE WITH FORMATION LOGIC when menu implemented

                //// 5 player characters ////
                // DEFAULT - until selectable via UI:
                // 1 is at mouse click
                // 2 is left of click
                // 3 is right of click
                // 4 is behind 2
                // 5 is behind 3

                // Vector2[] targetPositions = new Vector2[9] {
                //     new Vector2(-50, -50), new Vector2(0, -50), new Vector2(50, -50),
                //     new Vector2(-50, 0), new Vector2(0, 0), new Vector2(50, 0),
                //     new Vector2(-50, 50), new Vector2(0, 50), new Vector2(50, 50)
                // };
                for (int i = 0; i < _selectedCharacters.Count; i++)
                {
                    // Vector2 startGridPos = _selectedCharacters[i].GlobalPosition;
                    // Vector2
                    // HexNavigation.CalculateHexPath(_selectedPos1, _selectedPos2)
                    // _selectedPos1 = WorldToGrid(mousePos);
                    Selection.EmitSignal(Selection.SignalName.MoveTargetSelected, _selectedCharacters[i], Selection.GetGlobalMousePosition());
                    // _selectedCharacters[i].EndTarget = Selection.GetGlobalMousePosition();// + targetPositions[i+3];
                    // _selectedCharacters[i].OnEndTargetSet();
                    // _selectedCharacters[i].NavAgent.TargetPosition = GetGlobalMousePosition() + targetPositions[i+3];
                }
                // TEMP FOR DEWBUG:
                Selection.GetNode<TextureRect>("marker").Visible = true;
                Selection.GetNode<TextureRect>("marker").GlobalPosition = Selection.GetGlobalMousePosition();
                ////
            }
        }
    }

    public override void DrawUpdateOnce()
    {
        base.DrawUpdateOnce();
        if (_mouseBtnHeldDown)
        {
            Selection.DrawRect(_drawRect, new Color(0,1,0), false, 5);
        }
    }

    private void ClearSelectedCharacters()
    {
        foreach (CharacterUnit characterUnit in _selectedCharacters)
        {
            characterUnit.SetDebugText("");
            characterUnit.Selected = false;
        }
        _selectedCharacters.Clear();
    }

    public override void OnPlayerCharacterClicked(CharacterUnit playerCharacter, bool shift)
    {
        if (!shift)
        {
            ClearSelectedCharacters();
        }
        if (!_selectedCharacters.Contains(playerCharacter) && PlayerCharacters.Contains(playerCharacter))
        {
            _selectedCharacters.Add(playerCharacter);
            playerCharacter.Selected = true;
            playerCharacter.SetDebugText("SELECTED");
        }
    }

	public override void ProcessUpdate(double delta)
	{
        if (_mouseBtnHeldDown)
        {
            _end = Selection.GetGlobalMousePosition();
            _drawRect = new Rect2();
            _drawRect.Position = _start;
            _drawRect.Size = new Vector2(_end.X - _start.X, _end.Y - _start.Y);
        }
        Selection.QueueRedraw();
	}
}
