// ScreenShaker2D: shakes a 2D camera to a specified magnitude, for a specified duration
// Usage:
// Add as a child of a Camera2D
// Set the duration and magnitude variables as desired
// Activate Start (in editor) or Shake() (in code)

using Godot;
using System;

public partial class ScreenShaker2D : Node
{
    private bool _start = false;

    [Export]
    private float _duration = 0.18f;

    [Export]
    private float _magnitude = 3f;

    [Export]
    public bool Start {
        get {
            return _start;
        }
        set {
            _start = value;
            if (_start)
            {
                Shake();
            }
        }
    }

    private Camera2D _camera;
    private Godot.RandomNumberGenerator _rand = new();
    private Vector2 _initialOffset;
    
    public override void _Ready()
    {
        if (GetParent() is Camera2D camera)
        {
            _camera = camera;
        }
        else
        {
            GD.PrintErr("Error: ScreenShaker2D must be a child of Camera2D");
            throw new Exception();
        }

    }

    // Call this externally to cause the shake effect
    public async void Shake()
    {
        if (!IsInsideTree())
        {
            await this.ToSignal(this, "ready");
        }
        _start = false;
        // Upon a new shake, start time at 0
        double time = 0;
        _rand.Randomize();
        float adjustedMagnitude = _magnitude * GetTree().Root.GetNode<GlobalSettings>("GlobalSettings").ScreenShakePercentModifier;

        if (adjustedMagnitude == 0)
        {
            return;
        }

        // Until time elapsed passes the specified duration of the shake
        while (time < _duration)
        {   
            // Increment _time but not beyond max duration
            time += _camera.GetProcessDeltaTime();
            time = Math.Min(time, _duration);

            // Every frame set the offset to a random x and y value within the specified magnitude
            // So with a larger magnitude the offset will be greater and the screen will appear to shake more
            Vector2 offset = new Vector2();
            offset.X = _rand.RandfRange(-adjustedMagnitude, adjustedMagnitude);
            offset.Y = _rand.RandfRange(-adjustedMagnitude, adjustedMagnitude);
            _camera.Offset = _initialOffset + offset;
            GD.Print(_camera.Offset);
            // Must be called otherwise the screen will freeze throughout the loop
            await _camera.ToSignal(_camera.GetTree(), "process_frame");
        }

        // After the shake set time back to 0 so we can shake all over again when needed
        _camera.Offset = _initialOffset;

    }

}