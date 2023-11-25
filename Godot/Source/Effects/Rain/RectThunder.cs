using Godot;
using System;

public partial class RectThunder : ColorRect
{
    [Export]
    private Timer _thunderTimer;
    [Export]
    private AnimationPlayer _thunderAnim;

    private Random _rand = new();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _thunderTimer.Timeout += this.OnThunderTimerTimeout;
        SetThunderTimer();
    }

    private void OnThunderTimerTimeout()
    {
        _thunderAnim.Play("Thunder");
        SetThunderTimer();
    }

    private void SetThunderTimer()
    {
        _thunderTimer.WaitTime = _rand.Next(20, 40);
        _thunderTimer.Start();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
