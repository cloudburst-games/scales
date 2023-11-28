// SettingsAudio: a component of the SettingsManager
using Godot;
using System.Collections.Generic;

public partial class SettingsAudio : Control
{

	//https://godotengine.org/qa/40911/best-way-to-create-a-volume-slider

	[Export]
	private AudioStream  _voiceSample;
	[Export]
	private AudioStream  _effectsSample;
	[Export]
	private AudioStream  _musicSample;
	
	public delegate void SettingsChangedDelegate();
	public event SettingsChangedDelegate SettingsChanged;

	private AudioStreamPlayer _soundPlayer;
	private Dictionary<string, AudioStream> _busSamples;
	private Dictionary<string, HSlider> _busSliders;
	bool _refreshing = false; // needed?

	public override void _Ready()
	{
		_soundPlayer = GetNode<AudioStreamPlayer>("AudioStreamPlayer");

		_busSamples = new Dictionary<string, AudioStream>() {
		{"Voice", _voiceSample}, {"Effects", _effectsSample}, {"Music", _musicSample},
	};
		_busSliders = new Dictionary<string, HSlider>() {
		{"Voice", GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/HBoxVoice/HSlider")},
		{"Effects", GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/HBoxEffects/HSlider")},
		{"Music", GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/HBoxMusic/HSlider")},
		{"Master", GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/HBoxMaster/HSlider")}
		};
		foreach (HSlider slider in _busSliders.Values)
		{
			slider.MinValue = 0.0001f;
			slider.MaxValue = 1f;  
		}

		Refresh();
	}

	public void Refresh()
	{
		_refreshing = true;

		foreach (KeyValuePair<string, HSlider> busSliderPair in _busSliders)
		{
			busSliderPair.Value.Value = Mathf.Exp((AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex(busSliderPair.Key))/20f));
			if (!busSliderPair.Value.IsConnected("value_changed", new Callable(this, nameof(OnHSliderChanged))))
			{
				busSliderPair.Value.ValueChanged += (double val) => OnHSliderChanged(val, busSliderPair.Key);
			}
		}
		_refreshing = false;
	}

	public void SetSoundMute(bool mute)
	{
		AudioServer.SetBusMute(AudioServer.GetBusIndex("Voice"), mute);
		AudioServer.SetBusMute(AudioServer.GetBusIndex("Effects"), mute);
		SettingsChanged?.Invoke();
	}
	public void SetMusicMute(bool mute)
	{
		AudioServer.SetBusMute(AudioServer.GetBusIndex("Music"), mute);
		SettingsChanged?.Invoke();
	}

	private void OnHSliderChanged(double value, string busName)
	{
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(busName), Mathf.Log((float)_busSliders[busName].Value)*20);
		if (!_refreshing)
		{
			SettingsChanged?.Invoke();
		}
		if (_refreshing || busName == "Master")
		{
			return;
		}
		_soundPlayer.Stream = _busSamples[busName];
		_soundPlayer.Bus = busName;
		_soundPlayer.Play();
	}

	public void Exit()
	{
		SettingsChanged = null;
	}

}
