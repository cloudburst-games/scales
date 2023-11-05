using Godot;
using System;

public partial class TestCam : Camera2D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private float _speed = 500f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
    public override void _PhysicsProcess(double delta)
    {

        Vector2 dir = new Vector2();

        
        if (Input.IsActionPressed("ui_up"))
        {
            dir += new Vector2(0, -1);
        }
        if (Input.IsActionPressed("ui_down"))
        {
            dir += new Vector2(0, 1);
        }
        if (Input.IsActionPressed("ui_left"))
        {
            dir += new Vector2(-1, 0);
        }
        if (Input.IsActionPressed("ui_right"))
        {
            dir += new Vector2(1, 0);
        }
        Position += dir.Normalized() * _speed * (float) delta;
        
    }
}
