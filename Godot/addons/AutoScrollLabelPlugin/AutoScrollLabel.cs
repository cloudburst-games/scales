// Simply allows for autoscrolling scrollable text, e.g. used in storytelling
using Godot;
using System;

public partial class AutoScrollLabel : RichTextLabel
{
    [Export(PropertyHint.Range, "2,500")]
    private float _scrollSpeed = 60f;

    [Export]
    private bool _autoScroll = true;

    private int _skippedFrame = 1;
    private double _lastScrollValue = -1;

	public override void _Ready()
	{
        GetVScrollBar().GuiInput += OnScrollBarGUIInput;
	}

    // Stop autoscroll when the user attempts to scroll the bar manually
    private void OnScrollBarGUIInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton)
        {
            _autoScroll = false;
        }
    }

    public override void _Process(double delta)
	{
        if (!_autoScroll)
        {
            return;
        }
        // VScrollBar doesnt work with low scroll speeds.. so we need to implement by skipping frames
        if (_scrollSpeed < 30)
        {
            int framesToSkip = Convert.ToInt32(30 / _scrollSpeed);
            _skippedFrame = (_skippedFrame + 1) % framesToSkip;
            
            if (_skippedFrame != 0)
            {
                return;
            }

        }
        if (_lastScrollValue <= GetVScrollBar().Value)
        {
            _lastScrollValue = GetVScrollBar().Value;
            GetVScrollBar().Value += Math.Max(30, _scrollSpeed) * delta;
        }
	}

}
