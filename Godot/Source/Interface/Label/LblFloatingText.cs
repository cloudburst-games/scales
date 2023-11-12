using Godot;
using System;

public partial class LblFloatingText : Label
{
    [Export]
    private float _moveDistance = 400f;

    [Export]
    private double _fadeSpeedSeconds = 3.0;

    [Export]
    private double _moveDurationSecs = 4.0;

    [Export]
    Tween.EaseType _easeType = Tween.EaseType.In;

    public override void _Ready()
    {
        // SetPhysicsProcess(false);
        // Start(new Vector2(600, 600));
    }

    public void Start(Vector2 startGlobalPosition)
    {
        startGlobalPosition -= Size / 2f;
        GlobalPosition = startGlobalPosition;
        Tween positionTween = CreateTween();
        positionTween.TweenProperty(this, "global_position", new Vector2(startGlobalPosition.X, startGlobalPosition.Y - _moveDistance), _moveDurationSecs);
        positionTween.SetEase(_easeType);
        Tween alphaTween = CreateTween();
        alphaTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), _fadeSpeedSeconds);
        alphaTween.SetEase(_easeType);


        // SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        // GD.Print(GetGlobalMousePosition());
        // GD.Print(GlobalPosition);
        if (Modulate.A <= 0)
        {
            QueueFree();
        }
    }
}
