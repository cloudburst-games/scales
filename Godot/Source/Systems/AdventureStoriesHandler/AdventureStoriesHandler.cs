// In future can use this to coordinate multiple adventures. When making a new adventure, just instance one of these and give it picture story children.

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class AdventureStoriesHandler : Control
{

	// public enum EndingType { Shamash, Balanced, Ishtar } // need to make this independent of this class oin future

	[Export]
	private Godot.Collections.Dictionary<int, NodePath> _defeatPictureStoriesNodePaths = new();
	private Dictionary<int, PictureStoryContainer> _defeatPictureStoriesByLevel;
	// [Export]
	// private PictureStoryContainer _finalVictoryStory;

	[Export]
	private Godot.Collections.Dictionary<int, NodePath> _victoryPictureStoriesNodePaths = new();

	[Export]
	private Godot.Collections.Dictionary<Scales.FavourMode, NodePath> _finalVictoryStoryNodePaths = new();

	private Dictionary<int, PictureStoryContainer> _victoryPictureStoriesByLevel;
	private Dictionary<Scales.FavourMode, PictureStoryContainer> _finalVictoryPictureStories;

	// [Export]
	// private int _finalLevel = 3;

	[Signal]
	public delegate void DefeatStoryFinishedEventHandler();

	[Signal]
	public delegate void FinalVictoryStoryFinishedEventHandler();

	[Signal]
	public delegate void VictoryPictureStoryFinishedEventHandler(int level);

	public override void _Ready()
	{
		// _defeatPictureStory.Finished += () => this.EmitSignal(SignalName.DefeatStoryFinished);
		// _finalVictoryStory.Finished += () => this.EmitSignal(SignalName.FinalVictoryStoryFinished);

		_defeatPictureStoriesByLevel = new();
		foreach (KeyValuePair<int, NodePath> kv in _defeatPictureStoriesNodePaths)
		{
			_defeatPictureStoriesByLevel[kv.Key] = GetNode<PictureStoryContainer>(kv.Value);
		}
		foreach (KeyValuePair<int, PictureStoryContainer> kv in _defeatPictureStoriesByLevel)
		{
			kv.Value.Finished += () =>
			{
				this.EmitSignal(SignalName.DefeatStoryFinished);
				// BaseProject.Utils.Node.GetNodesRecursive(new List<Control>(), kv.Value).ForEach(x => x.MouseFilter = MouseFilterEnum.Ignore);
			};
		}

		_victoryPictureStoriesByLevel = new();
		foreach (KeyValuePair<int, NodePath> kv in _victoryPictureStoriesNodePaths)
		{
			_victoryPictureStoriesByLevel[kv.Key] = GetNode<PictureStoryContainer>(kv.Value);
		}
		foreach (KeyValuePair<int, PictureStoryContainer> kv in _victoryPictureStoriesByLevel)
		{
			kv.Value.Finished += () => this.EmitSignal(SignalName.VictoryPictureStoryFinished, kv.Key);
		}
		_finalVictoryPictureStories = new();
		foreach (KeyValuePair<Scales.FavourMode, NodePath> kv in _finalVictoryStoryNodePaths)
		{
			_finalVictoryPictureStories[kv.Key] = GetNode<PictureStoryContainer>(kv.Value);
		}
		foreach (KeyValuePair<Scales.FavourMode, PictureStoryContainer> kv in _finalVictoryPictureStories)
		{
			kv.Value.Finished += () => this.EmitSignal(SignalName.FinalVictoryStoryFinished);
		}
		BaseProject.Utils.Node.GetNodesRecursive(new List<Control>(), this).Where(x => x is not BaseTextureButton).ToList().ForEach(x => x.MouseFilter = MouseFilterEnum.Ignore);
		// Test();
	}

	// private void Test()
	// {
	//     DoVictoryStory(0);
	// }

	public void DoVictoryStory(int level, Scales.FavourMode favour, int finalLevel)
	{
		// Visible = true;
		GD.Print("is it the last level? ", +level + " " + finalLevel);
		if (level == finalLevel)
		{
			_finalVictoryPictureStories[favour].Play();
			return;
		}
		_victoryPictureStoriesByLevel[level].Play();


	}

	public void DoDefeatStory(int level)
	{
		// Visible = true;
		_defeatPictureStoriesByLevel[level].Play();
	}
}
