// SceneTransitionAnimatedLoad. Helper class for animating the loading of a destination scene, used by SceneTransition
// Todo: fix progress bar - see *** comments ***

using Godot;
using System;

public partial class SceneTransitionAnimatedLoad : Node
{
	public delegate void FinishedDelegate();
	public event FinishedDelegate Finished;

	public string DestinationScenePath { get; set; }
	private TextureProgressBar _progressBar;
	public ISceneTransitionShareableData SharedData;

	private ISceneTransitionable _destination;

	private Godot.Collections.Array _progress = new();
	private ResourceLoader.ThreadLoadStatus _sceneLoadStatus = ResourceLoader.ThreadLoadStatus.InvalidResource;
	private SceneTransition.LoadType _loadType = SceneTransition.LoadType.Simple;

	public override void _Ready()
	{
		_progressBar = GetNode<TextureProgressBar>("CanvasLayer/Control/TextureProgressBar");

		// *** Until progress bar is fixed, hide it and rely on animation only: ***
		_progressBar.Visible = false;
	}

	public async void Start(SceneTransition.LoadType loadType, ISceneTransitionable source)
	{
		_loadType = loadType;
		GetNode<AnimationPlayer>("CanvasLayer/AnimFade").Play("FadeTo");
		// Hide the continue button if we aren't interacting with the load screen.
		// Done this way so that for animated interaction, the user can decide whether to use the button or not
		if (_loadType != SceneTransition.LoadType.AnimatedInteract)
		{
			GetNode<TextureButton>("CanvasLayer/Control/BtnContinue").Visible = false;
		}
		GetNode<AnimationPlayer>("CanvasLayer/Control/Anim").Play("Loading");
		// Wait until faded in before proceeding (~ < 0.5sec) - and free the source scene when black
		await ToSignal(GetNode<AnimationPlayer>("CanvasLayer/AnimFade"), "animation_finished");
		source.QueueFree();

		// Start loading the scene
		ResourceLoader.LoadThreadedRequest(DestinationScenePath);
	}

	public override void _Process(double delta)
	{

		// Update the progress per frame
		_sceneLoadStatus = ResourceLoader.LoadThreadedGetStatus(DestinationScenePath, _progress);
		// GD.Print(_sceneLoadStatus.ToString());

		// GD.Print(_progress);
		// *** Not working - await godot docs on LoadThreadedGetStatus +- bug fix ***
		// GD.Print(_progress[0]);
		// _progressBar.Value = _progress[0].AsDouble() * 100;

		// IF not fixed and we really want a progress bar, then consider doing it hacky facebook way
		// (time how long it takes on an average phone, then slowly incrememnt the bar towards this time
		// then if loaded early jump to 100%, and if loaded late, stay at 90% and wait until loaded then go
		// to 100)
		// https://github.com/godotengine/godot/issues/56882

		// When loaded, stop the animation and enable the continue button, instance the new scene, add SharedData and
		// add the scene to root. Can then stop processing. If not interactive (i.e. continue hidden) then proceed.
		if (_sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded)
		{
			OnSceneLoaded();
		}
	}

	private async void OnSceneLoaded()
	{
		// See above re progress bar issue
		// _progressBar.Value = 100;
		GetNode<AnimationPlayer>("CanvasLayer/Control/Anim").Stop();

		_destination = ((PackedScene)ResourceLoader.LoadThreadedGet(DestinationScenePath)).Instantiate<ISceneTransitionable>();
		if (!(SharedData == null))
		{
			_destination.OnReceivedSharedData(SharedData);
		}
		GetTree().Root.AddChild((Node)_destination);

		SetProcess(false);

		GetNode<AnimationPlayer>("CanvasLayer/Control/AnimLoaded").Play("Loaded");

		await ToSignal(GetNode<AnimationPlayer>("CanvasLayer/Control/AnimLoaded"), "animation_finished");

		GetNode<TextureButton>("CanvasLayer/Control/BtnContinue").Disabled = false;
		if (_loadType != SceneTransition.LoadType.AnimatedInteract)
		{
			OnBtnContinuePressed();
		}

	}

	private async void OnBtnContinuePressed()
	{
		if (_sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded)
		{
			GetNode<AnimationPlayer>("CanvasLayer/AnimFade").Play("FadeFrom");

			await ToSignal(GetNode<AnimationPlayer>("CanvasLayer/AnimFade"), "animation_finished");

			Finished?.Invoke();
			Finished = null;
		}
		else
		{
			GD.Print("Scene not loaded or in progress");
		}
	}

	public override void _Input(InputEvent ev)
	{
		base._Input(ev);
		if (_sceneLoadStatus != ResourceLoader.ThreadLoadStatus.Loaded ||
			// GetNode<TextureButton>("CanvasLayer/Control/BtnContinue").Disabled || 
			_loadType != SceneTransition.LoadType.AnimatedInteract ||
			GetNode<TextureButton>("CanvasLayer/Control/BtnContinue").Visible)
		{
			return;
		}
		if (ev is InputEventKey || ev is InputEventJoypadButton)
		{
			OnBtnContinuePressed();
			SetProcessInput(false);
		}
		else if (ev is InputEventMouseButton evm)
		{
			if (evm.ButtonIndex != MouseButton.WheelUp && evm.ButtonIndex != MouseButton.WheelLeft &&
				evm.ButtonIndex != MouseButton.WheelDown && evm.ButtonIndex != MouseButton.WheelRight)
			{
				OnBtnContinuePressed();
				SetProcessInput(false);
			}
		}
	}
}
