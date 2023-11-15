// In future can use this to coordinate multiple adventures. When making a new adventure, just instance one of these and give it picture story children.

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class AdventureStoriesHandler : Control
{

    [Export]
    private PictureStoryContainer _defeatPictureStory;
    [Export]
    private PictureStoryContainer _finalVictoryStory;

    [Export]
    private Godot.Collections.Dictionary<int, NodePath> _victoryPictureStoriesNodePaths = new();

    private Dictionary<int, PictureStoryContainer> _victoryPictureStoriesByLevel;

    [Signal]
    public delegate void DefeatStoryFinishedEventHandler();

    [Signal]
    public delegate void FinalVictoryStoryFinishedEventHandler();

    [Signal]
    public delegate void VictoryPictureStoryFinishedEventHandler(int level);

    public override void _Ready()
    {
        _defeatPictureStory.Finished += () => this.EmitSignal(SignalName.DefeatStoryFinished);
        _finalVictoryStory.Finished += () => this.EmitSignal(SignalName.FinalVictoryStoryFinished);

        _victoryPictureStoriesByLevel = new();
        foreach (KeyValuePair<int, NodePath> kv in _victoryPictureStoriesNodePaths)
        {
            _victoryPictureStoriesByLevel[kv.Key] = GetNode<PictureStoryContainer>(kv.Value);
        }
        foreach (KeyValuePair<int, PictureStoryContainer> kv in _victoryPictureStoriesByLevel)
        {
            kv.Value.Finished += () => this.EmitSignal(SignalName.VictoryPictureStoryFinished, kv.Key);
        }

        // Test();
    }

    // private void Test()
    // {
    //     DoVictoryStory(0);
    // }

    public void DoVictoryStory(int level)
    {
        _victoryPictureStoriesByLevel[level].Play();
    }

    public void DoDefeatStory()
    {
        _defeatPictureStory.Play();
    }
}