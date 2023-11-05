// GlobalAudio singleton. Keeps audio files within the SceneTree when marked as global for uninterrupted sound.
using Godot;
using System;

public partial class GlobalAudio : Node
{
	public bool ContainsAudioContainer(string globalName)
	{
		return GetAudioContainerByGlobalName(globalName) != null;        
	}

	private AudioContainer GetAudioContainerByGlobalName(string globalName)
	{
		for (int i = 0; i < GetChildCount(); i++)
		// foreach (Node n in GetChildren())
		{
			if (GetChild(i) is AudioContainer audioContainer)
			{
				if (audioContainer.GlobalName == globalName)
				{
					return audioContainer;
				}
			}
		}
		return null;
	}

	public void Pause(string globalName)
	{
		AudioContainer audioContainer = GetAudioContainerByGlobalName(globalName);
		if (audioContainer != null)
		{
			audioContainer.Pause();
		}
	}

	public void Resume(string globalName)
	{
		AudioContainer audioContainer = GetAudioContainerByGlobalName(globalName);
		if (audioContainer != null)
		{
			audioContainer.Resume();
		}
	}
}
