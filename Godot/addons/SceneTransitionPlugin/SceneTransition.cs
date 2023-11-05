// SceneTransition node. Add in the Scene as a child via the editor, then enter the DestinationPath in the editor/code.
// Add any data to transfer via SharedData field
// Call Start with LoadType arg to transition to destination scene.
using Godot;
using System;

public partial class SceneTransition : Node
{
    public enum LoadType { Simple, AnimatedInteract, AnimatedAuto }

    private bool _activated = false;

    [Export]
    public string DestinationPath { get; set; }

    private PackedScene _transitionToScn;

    // If needing to pass data between scenes, assign SharedData. This will then be passed to the destination scene.
    public ISceneTransitionShareableData SharedData;

    private ISceneTransitionable _destination;
    private PackedScene _transitionFadeScn;
    private PackedScene _transitionAnimatedLoadScn;

    private ISceneTransitionable _source;

    public override void _Ready()
    {
        _transitionFadeScn = GD.Load<PackedScene>("res://addons/SceneTransitionPlugin/SceneTransitionFade.tscn");
        _transitionAnimatedLoadScn = GD.Load<PackedScene>("res://addons/SceneTransitionPlugin/SceneTransitionAnimatedLoad.tscn");
    }

    private void OnFinished()
    {
        GetTree().Paused = false;
        QueueFree();
    }

    public void Start(LoadType loadType)
    {
        // Each SceneTransition node should only be activated once (freed at the end of activation)
        if (_activated)
        {
            return;
        }
        _activated = true;

        GetTree().Paused = true;

        _source = GetOwnerScene();
        var root = GetTree().Root;

        // Move this node to root so it can continue whilst the owner is freed
        GetParent().RemoveChild(this);
        root.CallDeferred("add_child", this);

        if (loadType == LoadType.Simple)
        {
            SimpleLoadScene();
        }
        else
        {
            AnimatedLoadScene(loadType);
        }
    }


    public async void SimpleLoadScene()
    {
        // Loads the scene. If this takes a long time, probably need to use AnimatedLoadScene instead
        _transitionToScn = GD.Load<PackedScene>(DestinationPath);
        _destination = _transitionToScn.Instantiate<ISceneTransitionable>();
        if (!(SharedData == null))
        {
            _destination.OnReceivedSharedData(SharedData);
        }

        // Add the node coordinating transition animation to root so it can continue whilst this node's owner is freed
        Node transitionFade = _transitionFadeScn.Instantiate<Node>();
        AddChild(transitionFade);
        transitionFade.GetNode<AnimationPlayer>("CanvasLayer/Anim").Play("FadeIn");

        // Wait until faded to black before freeing the source scene, and adding the destination node
        await ToSignal(transitionFade.GetNode<AnimationPlayer>("CanvasLayer/Anim"), "animation_finished");

        _source.QueueFree();

        GetTree().Root.CallDeferred("add_child", (Node)_destination);

        transitionFade.GetNode<AnimationPlayer>("CanvasLayer/Anim").Play("FadeOut");

        // When the transition node has completed its final animation, free it, then free this node.
        await ToSignal(transitionFade.GetNode<AnimationPlayer>("CanvasLayer/Anim"), "animation_finished");
        OnFinished();
    }

    private void AnimatedLoadScene(LoadType loadType)
    {
        var _transitionAnimatedLoad = _transitionAnimatedLoadScn.Instantiate<SceneTransitionAnimatedLoad>();
        _transitionAnimatedLoad.DestinationScenePath = DestinationPath;
        _transitionAnimatedLoad.SharedData = SharedData;
        AddChild(_transitionAnimatedLoad);
        _transitionAnimatedLoad.Finished += this.OnFinished;
        // Pass in the source scene so that we can free it whilst faded to black
        _transitionAnimatedLoad.Start(loadType, _source);

    }

    public ISceneTransitionable GetOwnerScene()
    {
        Node n = GetParent();
        while (n != null && !(n is ISceneTransitionable))
        {
            n = n.GetParent();
        }
        if (n is ISceneTransitionable owner)
        {
            return owner;
        }
        return null;
    }

}
