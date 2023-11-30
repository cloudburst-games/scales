// PictureStoryContainer: coordinates and transitions between slides. Add PictureStorySlide as children.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class PictureStoryContainer : Control
{
    #region Events
    [Signal]
    public delegate void FinishedEventHandler();
    #endregion
    #region EditorProperties
    // TODO - use shaders +- animplayer to make more complex transition types - in PictureStoryTransition.tscn
    [Export(PropertyHint.Enum, "Fade,None,Blend")]
    public string TransitionTypeBetween
    {
        get => _transitionTypeBetween;
        set
        {
            _transitionTypeBetween = value;
            NotifyPropertyListChanged();
        }
    }
    private string _transitionTypeBetween = "Blend";

    [Export(PropertyHint.Enum, "FadeFromBlack,None,Blend")]
    private string _transitionTypeStart = "Blend";

    [Export(PropertyHint.Enum, "FadeToBlack,None,Blend")]
    private string _transitionTypeEnd = "FadeToBlack";

    private float _blendFadeTime = 0.5f;

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        var properties = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        if (_transitionTypeBetween == "Blend" || _transitionTypeEnd == "Blend" || _transitionTypeStart == "Blend")
        {
            properties.Add(new Godot.Collections.Dictionary()
            {
                {"name", "_blendFadeTime" },
                { "type", (int) Variant.Type.Float},
                { "usage", (int) PropertyUsageFlags.Default},
                { "hint", (int) PropertyHint.Range},
                { "hint_string", "0.1,3"}
            });
        }
        return properties;
    }

    [Export]
    public bool Start
    {
        get
        {
            return _start;
        }
        set
        {
            _start = value;
            if (_start)
            {
                Play();
            }
        }
    }
    #endregion
    #region Other Properties
    private AnimTransition _animTransition;

    private int _current = 0;
    private List<PictureStorySlide> _slides = new List<PictureStorySlide>();

    private bool _start = false;

    private Timer _readTimer = new Timer();

    private enum StoryStateMode
    {
        InitialTransition, PlayingSlide, Waiting, BetweenTransition, EndTransition, Finished
    }

    private StoryStateMode _storyState = StoryStateMode.InitialTransition;
    private bool _continuePressed = false;
    #endregion

    public override void _Ready()
    {
        // There has got to be a better way to start it in full screen..
        if (Engine.IsEditorHint())
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            SetDeferred("size", new Vector2(int.Parse(ProjectSettings.GetSetting("display/window/size/viewport_width").ToString()),
                int.Parse(ProjectSettings.GetSetting("display/window/size/viewport_height").ToString())));

        }
        else
        {
            // Referencing nodes and initialising them
            _readTimer.OneShot = true;
            _readTimer.Autostart = false;
            AddChild(_readTimer);
            _readTimer.Timeout += OnReadTimerTimeout;
            PackedScene picStoryTransitionScn = GD.Load<PackedScene>("res://addons/PictureStoryPlugin/PictureStoryContainer/PictureStoryTransition.tscn");
            CanvasLayer picStoryTransition = picStoryTransitionScn.Instantiate<CanvasLayer>();
            picStoryTransition.Name = "PictureStoryTransition";
            AddChild(picStoryTransition);
            _animTransition = picStoryTransition.GetNode<AnimTransition>("AnimTransition");
            // Each transition animation must declare when it is safe to switch slides (i.e. obscured by the anim)
            _animTransition.SafeToSwitchSlides += this.SwitchOpacitiesToNextSlide;
            _animTransition.AnimationFinished += anim_name => OnTransitionAnimationFinished(anim_name);
            Visible = false;
            SetProcessInput(false);

            // Set starting properties for and initialise each slide, and add to _slides list for readability
            for (int i = 0; i < GetChildren().Count; i++)
            {
                if (GetChildren()[i] is PictureStorySlide slide)
                {
                    if (slide.IsPlaying())
                    {
                        slide.Stop();
                    }
                    _slides.Add(slide);
                    slide.Visible = false;
                    slide.Modulate = i == 0 && _transitionTypeStart != "Blend" ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);


                    if (slide.ButtonToContinue != null)
                    {
                        slide.ButtonToContinue.Pressed += this.ForceContinue;
                    }
                }
            }
            // GD.Print(_slides.Count);
        }
    }

    public override void _Input(InputEvent ev)
    {
        base._Input(ev);

        if (!_slides[_current].InputSkipsSlide)
        {
            return;
        }
        if (ev.IsPressed() && !ev.IsEcho())
        {
            if (ev is InputEventKey || ev is InputEventMouseButton || ev is InputEventJoypadButton)
            {
                ForceContinue();
            }
        }
    }


    #region Event callbacks
    private void OnReadTimerTimeout()
    {
        StopSlideAudio(_current);
        NextStoryState();
    }

    private void OnTransitionAnimationFinished(string animName)
    {
        NextStoryState();
    }

    private void OnBlendTweenFinished()
    {
        NextStoryState();
    }

    private void OnCurrentSlideAnimationFinished(string animName)
    {
        StopSlideAudio(_current);
        NextStoryState();
    }
    #endregion

    // Basic state pattern implementation to coordinate each state
    private void NextStoryState()
    {
        // GD.Print("transition from: " + _storyState.ToString());
        switch (_storyState)
        {
            case StoryStateMode.InitialTransition:
                _storyState = StoryStateMode.PlayingSlide;
                break;
            case StoryStateMode.PlayingSlide:
                _storyState = StoryStateMode.Waiting;
                break;
            case StoryStateMode.Waiting:
                _storyState = StoryStateMode.BetweenTransition;
                break;
            case StoryStateMode.BetweenTransition:
                if (_current < _slides.Count - 1)
                {
                    _current += 1;
                    _storyState = StoryStateMode.PlayingSlide;
                }
                else
                {
                    _storyState = StoryStateMode.EndTransition;
                }
                break;
            case StoryStateMode.EndTransition:
                _storyState = StoryStateMode.Finished;
                EmitSignal(SignalName.Finished);
                break;
        }
        // GD.Print("transition to: " + _storyState.ToString());

        Play(_current);

    }

    public async void Play(int from = 0)
    {

        if (Engine.IsEditorHint())
        {
            return;
        }
        if (!IsInsideTree())
        {
            await ToSignal(this, "ready");
        }
        _start = false;

        Visible = true;
        SetProcessInput(true);
        // GD.Print("no" + " " + _slides.Count);
        _slides[from].Visible = true;



        switch (_storyState)
        {
            case StoryStateMode.InitialTransition:
                if (_transitionTypeStart != "None" && _transitionTypeStart != "Blend")
                {
                    _animTransition.Play(_transitionTypeStart);
                }
                else
                {
                    NextStoryState();
                }
                break;
            case StoryStateMode.PlayingSlide:
                if (_current == 0 && _transitionTypeStart == "Blend")
                {
                    StartBlendingToNextSlide(_blendFadeTime);
                }
                _slides[from].AnimationFinished += anim_name => OnCurrentSlideAnimationFinished(anim_name);
                if (_slides[from].GetAnimationList().Length == 0)
                {
                    GD.Print(string.Format("Warning! No animation in PictureStorySlide {0} of {1}!", _slides[from].Name, Name));
                }
                _slides[from].Play(_slides[from].GetAnimationList().Where(x => x != "RESET").ToList()[0]);
                break;
            case StoryStateMode.Waiting:
                if (_continuePressed)
                {
                    // GD.Print("contune pressed");
                    //_continuePressed = false;
                    // _continuePressed = false; GD.Print("conitnue pressed: ", _continuePressed);
                    NextStoryState();
                }
                else if (_slides[_current].AutoTransitionTime > 0)
                {
                    // GD.Print("here");
                    _readTimer.WaitTime = _slides[_current].AutoTransitionTime;
                    _readTimer.Start();
                }
                else
                {
                    NextStoryState();
                }
                break;
            case StoryStateMode.BetweenTransition:
                // Make sure the next slide is visible, to allow transition
                if (_slides.Count > from + 1)
                {
                    _slides[from + 1].Visible = true;
                }
                if (_current >= _slides.Count - 1 || _transitionTypeBetween == "None")
                {
                    NextStoryState();
                }
                else if (_transitionTypeBetween == "Blend")
                {
                    StartBlendingToNextSlide(_blendFadeTime).Finished += OnBlendTweenFinished;
                }
                else
                {
                    _animTransition.Play(_transitionTypeBetween);
                }
                break;
            case StoryStateMode.EndTransition:
                if (_transitionTypeEnd == "Blend")
                {
                    StartBlendingToNextSlide(_blendFadeTime).Finished += OnBlendTweenFinished;
                }
                else
                {
                    _animTransition.Play(_transitionTypeEnd);
                }
                break;
        }
    }

    private void SwitchOpacitiesToNextSlide()
    {
        _slides[_current].Modulate = new Color(1, 1, 1, 0);
        _slides[_current + 1].Modulate = new Color(1, 1, 1, 1);
    }

    private Tween StartBlendingToNextSlide(float fadeTime)
    {
        Tween tween = GetTree().CreateTween();
        // Starting slide
        if (_current == 0 && _storyState == StoryStateMode.PlayingSlide)
        {
            tween.TweenProperty(_slides[_current], "Modulate", new Color(1, 1, 1, 1), fadeTime);
            // return tween;
        }
        // Ending slide
        else if (_current == _slides.Count - 1)
        {
            tween.TweenProperty(_slides[_current], "Modulate", new Color(1, 1, 1, 0), fadeTime);
            // return tween;
        }
        else
        {
            tween.TweenProperty(_slides[_current + 1], "Modulate", new Color(1, 1, 1, 1), fadeTime);
        }
        // tween.TweenProperty(_slides[_current], "Modulate", new Color(1, 1, 1, 0), fadeTime);
        // tween.Parallel().TweenProperty(_slides[_current + 1], "Modulate", new Color(1, 1, 1, 1), fadeTime);
        return tween;
    }


    private void ForceContinue()
    {
        _continuePressed = true;
        // GD.Print("clicked");
        // if (_readTimer.TimeLeft > 0)
        // {
        //     _readTimer.Stop();
        //     _readTimer.EmitSignal("timeout");
        // }
        // else if (_slides[_current].IsPlaying())
        // {
        //     _slides[_current].Stop();
        //     _slides[_current].EmitSignal("animation_finished", _slides[_current].CurrentAnimation);
        // }
        // else
        // {

        _slides[_current].ButtonToContinue.MouseFilter = MouseFilterEnum.Ignore;

        StopAndProgressStoryState();
        // }
    }

    private void StopAndProgressStoryState()
    {
        // if (_storyState == StoryStateMode.Waiting)
        // {
        //     // Already will skip to next slide
        //     return;
        // }
        _readTimer.Stop();
        _slides[_current].Stop();
        StopSlideAudio(_current);
        NextStoryState(); _continuePressed = false;
    }

    private void StopSlideAudio(int slideNumber)
    {
        foreach (Node n in _slides[slideNumber].GetChildren())
        {
            if (n is AudioContainer audio)
            {
                audio.Stop();
            }
        }
    }

}


// Unused code - saving as an example of how to hint_string a list of strings in the editor.
// Add inputs from a list; if empty then any key should work
// private Godot.Collections.Array<string> _fadeInputs = new();
// public Godot.Collections.Array<string> FadeInputs {
// 	get => _fadeInputs;
// 	set
// 	{
// 		_fadeInputs = value;
// 		NotifyPropertyListChanged();
// 	}
// }

// string actionHintString = string.Format("2/2:{0}", // Why 2/2? What does it mean?
// 	GlobalSettings.ALL_INPUT_ACTIONS);
// properties.Add(new Godot.Collections.Dictionary()
// {
// 	{"name", "FadeInputs" },
// 	{ "type", (int) Variant.Type.Array},
// 	{ "usage", (int) PropertyUsageFlags.Default},
// 	{ "hint", (int) PropertyHint.TypeString },
// 	{ "hint_string", actionHintString}
// });
