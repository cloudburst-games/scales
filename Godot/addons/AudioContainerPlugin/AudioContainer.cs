// Play all the sounds of its children

using Godot;
using System;
using System.Collections.Generic;

public partial class AudioContainer : Node
{

	#region Events
	[Signal]
	public delegate void ReadyFinishedEventHandler();
	private bool _readyFinished = false;
	#endregion

	#region Properties
	public enum PlayOrder { Ordered, Random }

	// ** LoopOrder **
	// Off = no looping
	// Once = go through each child then stop (not intended to work with Ordered PlayOrder)
	// Repeat = play continuously through each child (or random if Random PlayOrder) until Stop() called.
	public enum LoopOrder { Off, Once, Repeat }

	private bool _start = false;
	[Export]
	public bool Start {
		get {
			return _start;
		}
		set {
			_start = value;
			if (_start)
			{
				Play();
			}
		}
	}

	[Export]
	private PlayOrder _playOrder = PlayOrder.Random;
	[Export]
	private LoopOrder _loop = LoopOrder.Off;
	[Export]
	private bool _globalAudio = false;

	// Identifier for getting globally playing audio (via GlobalAudio singleton)
	[Export]
	public string GlobalName {get; set;} = "";
	[Export]
	private float _fadeDuration = 1f;

	private List<Node> _allPlayers = new();
	private int _currentIndex = 0;

	// Resuming - hold whether we last intended to pause or resume - prevents pausing during tween whilst resuming
	private bool _resuming = false; 

	private Random _rand = new();
	#endregion

	public async override void _Ready()
	{
		// If the audio is set to global (i.e. not a child of the current scene), it should always process (as the game)
		// pauses during scene changes. 
		// This node is freed if it already exists (avoid duplicates) as a child of the global audio node. Otherwise,
		// it is added to the global audio node.
		if (_globalAudio)
		{
			ProcessMode = ProcessModeEnum.Always;
			if (GetTree().Root.HasNode("GlobalAudio"))
			{
				GlobalAudio globalAudio = GetTree().Root.GetNode<GlobalAudio>("GlobalAudio");
				if (globalAudio.ContainsAudioContainer(GlobalName))
				{
					QueueFree();
					return;
				}
				GetParent().CallDeferred("remove_child", this);
				globalAudio.CallDeferred("add_child", this);
			}
			else
			{
				GD.Print("AudioContainer.cs: Autoload node \"GlobalAudio\" not found.");
			}
		}

		// I think this is necessary. Wait until we are in the scene tree before attaching signals.
		if (!IsInsideTree())
		{
			await ToSignal(this, "tree_entered");
		}
		// Loop through and add all the audio players, and subscribe the events once they are added (wait until)
		// the next frame
		for (int i = 0; i < GetChildCount(); i++)
		{
			Node n = GetChild(i);       
			
			if (n is AudioStreamPlayer || n is AudioStreamPlayer2D || n is AudioStreamPlayer3D)
			{
				_allPlayers.Add(n);
				// Store the original volume of each player - important to restore volume during tweening.
				n.SetMeta("OriginalVolumeDb", GetVolumeDbOfPlayer(n));
			}
			await ToSignal(GetTree(), "process_frame");
			if (n is AudioStreamPlayer player)
			{
				player.Finished+=this.OnFinished;
			}
			else if (n is AudioStreamPlayer2D player2D)
			{
				player2D.Finished+=this.OnFinished;
			}
			else if (n is AudioStreamPlayer3D player3D)
			{
				player3D.Finished+=this.OnFinished;
			}
		}
		
		EmitSignal(SignalName.ReadyFinished);
		_readyFinished = true;
		
	}


	private void OnFinished()
	{
		_currentIndex += 1;
		if (_loop == LoopOrder.Repeat)
		{
			PlayNext();
		}
		else if (_loop == LoopOrder.Once)
		{
			if (_playOrder == PlayOrder.Random)
			{
				GD.Print("AudioContainer.cs: May only Loop Once if Play Order is Ordered");
			}
			else if (_playOrder == PlayOrder.Ordered)
			{
				if (_currentIndex < _allPlayers.Count)
				{
					PlayNext();
				}
			}
		}
	}

	public void PlayNext()
	{
		if (_allPlayers.Count == 0)
		{
			GD.Print("AudioContainer has no playable child nodes.");
			return;
		}
		Node nextPlayer;
		if (_playOrder == PlayOrder.Random)
		{
			_currentIndex = _rand.Next(_allPlayers.Count);
			nextPlayer = _allPlayers[_currentIndex];
		}
		else
		{
			_currentIndex = _currentIndex % _allPlayers.Count;
			nextPlayer = _allPlayers[_currentIndex];
		}
		var playMethod = nextPlayer.GetType().GetMethod("Play");

		// ?. == null propagation
		playMethod?.Invoke(nextPlayer, new object[1] { (float) 0});
		// In case interrupted during tween, set the volume to original when playing from start (Play())
		SetVolumeDb(GetOriginalVolumeDbOfPlayer(nextPlayer));
		
	}

	public void Stop()
	{
		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		var stopMethod = currentPlayer.GetType().GetMethod("Stop");
		stopMethod?.Invoke(currentPlayer, Array.Empty<object>());
	}

	public async void Pause()
	{
		if (!IsPlaying())
		{
			return;
		}
		// If this method was executed more recently than Resume(), sets _resuming to false.
		_resuming = false;
		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		if (currentPlayer is AudioStreamPlayer2D || currentPlayer is AudioStreamPlayer)
		{
			// Only fade if the stream is long enough, otherwise it is pointless.
			if (GetStreamDuration() > _fadeDuration * 2)
			{
				Tween tween = TweenVolume(-50f);
				await ToSignal(tween, "finished");
			}
		}
		// If the Resume() method was called whilst this tween is active, then cancel the pause operation.
		if (_resuming)
		{
			return;
		}

		var streamPausedProperty = currentPlayer.GetType().GetProperty("StreamPaused");
		
		streamPausedProperty?.SetValue(currentPlayer, true);

		if (GetStreamDuration() > _fadeDuration * 2)
		{
			if (currentPlayer is AudioStreamPlayer2D || currentPlayer is AudioStreamPlayer)
			{
				SetVolumeDb(GetOriginalVolumeDbOfPlayer(currentPlayer));
			}
		}
	}

	private Tween TweenVolume(float volume)
	{
		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Linear);
		tween.TweenProperty(currentPlayer, "volume_db", volume, _fadeDuration);
		return tween;
	}

	public async void Play()
	{
		if (!_readyFinished)
		{
			await ToSignal(this, SignalName.ReadyFinished);
		}
		PlayNext();
	}

	public void Resume()
	{
		// Set _resuming to true if this method is called more recently than Pause()
		_resuming = true;
		if (!IsPaused() && !IsPlaying())
		{
			PlayNext();
		}
		else
		{
			Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
			if (IsPaused())
			{
				if (currentPlayer is AudioStreamPlayer2D || currentPlayer is AudioStreamPlayer)
				{
					// If the length is long enough then tween back to the original volume
					if (GetStreamDuration() > _fadeDuration * 2)
					{
						SetVolumeDb(-50);
					}
				}
			}
			var streamPausedProperty = currentPlayer.GetType().GetProperty("StreamPaused");
			streamPausedProperty?.SetValue(currentPlayer, false);

			if (currentPlayer is AudioStreamPlayer2D || currentPlayer is AudioStreamPlayer)
			{
				if (GetStreamDuration() > _fadeDuration * 2)
				{
					TweenVolume(GetOriginalVolumeDbOfPlayer(currentPlayer));
				}
			}
		}
	}


	public bool IsPaused()
	{
		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		var streamPausedProperty = currentPlayer.GetType().GetProperty("StreamPaused");
		if (streamPausedProperty != null)
		{
			return (bool)streamPausedProperty.GetValue(currentPlayer, null);
		}
		return false;
	}

	// I don't think this method is used. It just checks the original vol of the current player.
	// Can we remove it?
	private float GetOriginalVolumeDbCurrent()
	{
		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		return GetOriginalVolumeDbOfPlayer(currentPlayer);
	}

	private float GetOriginalVolumeDbOfPlayer(Node player)
	{
		if (player.HasMeta("OriginalVolumeDb"))
		{
			return player.GetMeta("OriginalVolumeDb").AsSingle();
		}
		return 0;
	}

	// Used at the start of the script to set original volumes
	// Can simplify with GetType().GetProperty
	private float GetVolumeDbOfPlayer(Node player)
	{
		float vol = 0;
		if (player is AudioStreamPlayer audioStreamPlayer)
		{
			vol = audioStreamPlayer.VolumeDb;
		}
		else if (player is AudioStreamPlayer2D audioStreamPlayer2D)
		{
			vol = audioStreamPlayer2D.VolumeDb;
		}
		else if (player is AudioStreamPlayer3D audioStreamPlayer3D)
		{
			vol = audioStreamPlayer3D.VolumeDb;
		}
		return vol;
	}

	// Can simplify with GetType().GetProperty
	private double GetStreamDuration()
	{

		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		AudioStream stream = null;
		if (currentPlayer is AudioStreamPlayer audioStreamPlayer)
		{
			stream = audioStreamPlayer.Stream;
		}
		else if (currentPlayer is AudioStreamPlayer2D audioStreamPlayer2D)
		{
			stream = audioStreamPlayer2D.Stream;
		}
		else if (currentPlayer is AudioStreamPlayer3D audioStreamPlayer3D)
		{
			stream = audioStreamPlayer3D.Stream;
		}
		if (stream == null)
		{
			return 0;
		}
		return stream.GetLength();
	}

	// Can simplify with GetType().GetProperty or similar
	private void SetVolumeDb(float volumeDb)
	{
		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		if (currentPlayer is AudioStreamPlayer audioStreamPlayer)
		{
			audioStreamPlayer.VolumeDb = volumeDb;
		}
		else if (currentPlayer is AudioStreamPlayer2D audioStreamPlayer2D)
		{
			audioStreamPlayer2D.VolumeDb = volumeDb;
		}
		else if (currentPlayer is AudioStreamPlayer3D audioStreamPlayer3D)
		{
			audioStreamPlayer3D.VolumeDb = volumeDb;
		}
	}

	public bool IsPlaying()
	{
		Node currentPlayer = _allPlayers[_currentIndex % _allPlayers.Count];
		var playingProperty = currentPlayer.GetType().GetProperty("Playing");
		if (playingProperty != null)
		{
			return (bool)playingProperty.GetValue(currentPlayer, null);
		}
		return false;
	}

}
