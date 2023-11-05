using Godot;
using System;

public partial class HexModifier : Node
{
    private HexGrid _hexGrid;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void Init(HexGrid grid)
    {
        _hexGrid = grid;
    }
    
    private void ToggleObstacleAtWorldPos(Vector2 worldPos, bool setAsObstacle)
    {
        // await ToSignal(GetTree(), "process_frame");
        // await ToSignal(GetTree(), "process_frame");
        // await ToSignal(GetTree(), "process_frame");
        // await ToSignal(GetTree(), "process_frame");
        // await ToSignal(GetTree(), "process_frame");
        // await ToSignal(GetTree(), "process_frame");
        // await ToSignal(GetTree(), "process_frame");
        _hexGrid.GetHexAtWorldPosition(worldPos).Obstacle = setAsObstacle;
        // _hexGrid.HexNavigation.ToggleTemporaryObstacle(
        //     _hexGrid.WorldToGrid(worldPos), setAsObstacle
        // );
        _hexGrid.UpdateNavigationAndDisplay();
        // _hexGrid.UpdateDisplay();
    }

    public void OnCharacterMovingInBattle(Vector2 worldPosOfCharacter, bool moving)
    {
        // if ((CharacterUnit.ActionMode) actionState == CharacterUnit.ActionMode.Idle ||
        //     (CharacterUnit.ActionMode) actionState == CharacterUnit.ActionMode.WaitingBattle)
        // {
        ToggleObstacleAtWorldPos(worldPosOfCharacter, !moving);
        // }
    }
}
