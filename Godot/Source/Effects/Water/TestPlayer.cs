// Isometric water (non-tiled)
// Get isometric png + water mask
// Lay them out as in the test scene
// Apply water shader to 2nd sprite (+ mask), dont forget the light mask
// Add a reflection to the character and add logic to adjust the reflection according to the region rect

using Godot;
using System;

public partial class TestPlayer : Sprite2D
{
    [Export]
    private AnimationPlayer _anim;
    [Export]
    private Sprite2D _reflection;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {

        Vector2 direction = new();

        if (Input.IsKeyPressed(Key.A))
        {
            direction += new Vector2(-1, 0);
        }
        if (Input.IsKeyPressed(Key.D))
        {
            direction += new Vector2(1, 0);
        }
        if (Input.IsKeyPressed(Key.W))
        {
            direction += new Vector2(0, -1);
        }
        if (Input.IsKeyPressed(Key.S))
        {
            direction += new Vector2(0, 1);
        }

        direction = direction.Normalized();
        if (direction.X > 0.5 && direction.Y > 0.5)
        {
            _anim.Play("downright");
        }
        else if (direction.X > 0.5 && direction.Y < -0.5)
        {
            _anim.Play("upright");
        }
        else if (direction.X < -0.5 && direction.Y < -0.5)
        {
            _anim.Play("upleft");
        }
        else if (direction.X < -0.5 && direction.Y > 0.5)
        {
            _anim.Play("downleft");
        }
        else if (direction.X > 0.5 && Math.Abs(direction.Y) < 0.5)
        {
            _anim.Play("right");
        }
        else if (direction.X < -0.5 && Math.Abs(direction.Y) < 0.5)
        {
            _anim.Play("left");
        }
        else if (Math.Abs(direction.X) < 0.5 && direction.Y < -0.5)
        {
            _anim.Play("up");
        }
        else if (Math.Abs(direction.X) < 0.5 && direction.Y > 0.5)
        {
            _anim.Play("down");
        }
        _reflection.RegionRect = this.RegionRect;
        Position += direction * 600 * (float)delta;
    }

}
