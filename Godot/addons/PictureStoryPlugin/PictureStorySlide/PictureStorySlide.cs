// PictureStorySlide: for use as child of PictureStoryContainer node
using Godot;
using System;

public partial class PictureStorySlide : AnimationPlayer
{
	// Transition to the next slide after AutoTransitionTime expires
	[Export]
	public bool AutoTransitionEnabled {get; set;} = true;
	[Export]
	public float AutoTransitionTime {get; set;} = 3f;

	// Any key or mouse button or joypad button input will skip to the next slide
	[Export]
	public bool InputSkipsSlide {get; set;} = true;

	// Can assign a button that progresses to the next slide
	[Export]
	public TextureButton ButtonToContinue {get; private set;}

	private bool _visible = true;
	public bool Visible {
		get => _visible;
		set {
			_visible = value;
			BaseProject.Utils.Node.SetVisibleRecursive(this, value);
		}
	}
	private Color _modulate = new Color(1,1,1,1);

	public Color Modulate {
		get => _modulate;
		set {
			_modulate = value;
			BaseProject.Utils.Node.SetModulateRecursive(this, value);
		}
	}

	public override void _Ready()
	{
		if (!AutoTransitionEnabled)
		{
			AutoTransitionTime = float.MaxValue;
		}
		if (ButtonToContinue != null)
		{
			ButtonToContinue.Pressed+=this.OnButtonToContinuePressed;
		}
	}

	private void OnButtonToContinuePressed()
	{
		ButtonToContinue.Disabled = true;
	}
}
