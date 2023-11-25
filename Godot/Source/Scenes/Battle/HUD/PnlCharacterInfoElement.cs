using Godot;
using System;

public partial class PnlCharacterInfoElement : Panel
{
    [Export]
    private Label _key;

    [Export]
    private Label _value;

    public void Set(string key, string value, Color color)
    {
        _key.Text = key;
        _value.Text = value;
        _value.Modulate = color;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
