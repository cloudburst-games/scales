using Godot;
using System;

public partial class LblBark : Label
{
    [Export]
    private AnimationPlayer _anim;

    // summary generated by chat gippity!!!!
    /// <summary>
    /// Displays a bark in the label.
    /// </summary>
    /// <param name="text">The text of the bark to display.</param>
    /// <remarks>
    /// If an animation is playing, it stops and appends the new text.
    /// If no animation is playing, it sets the label's text to the provided text.
    /// Plays the "AnimBark" animation after handling the text.
    /// </remarks>
    public void Bark(string text)
    {
        if (_anim.IsPlaying())
        {
            _anim.Stop();
            Text = text;

        }
        else
        {
            Text = text;
        }
        _anim.Play("AnimBark");
    }
}
