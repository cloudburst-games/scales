using Godot;
using System.Linq;
using System.Collections.Generic;

public partial class HUD : CanvasLayer
{
	public override void _Ready()
	{
		GetNode<BasePanel>("Control/PnlMenu").Close();
        GetNode<BaseTextureButton>("Control/PnlMenu/VBoxContainer/HBoxContainer/BtnSound").ButtonPressed = AudioServer.IsBusMute(AudioServer.GetBusIndex("Voice"));
        GetNode<BaseTextureButton>("Control/PnlMenu/VBoxContainer/HBoxContainer/BtnMusic").ButtonPressed = AudioServer.IsBusMute(AudioServer.GetBusIndex("Music"));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnBtnSoundPressed()
	{
		GetNode<SettingsAudio>("SettingsManager/Panel/SettingsAudio").SetSoundMute(
			!AudioServer.IsBusMute(AudioServer.GetBusIndex("Voice")));
	}

	private void OnBtnMusicPressed()
	{
		GetNode<SettingsAudio>("SettingsManager/Panel/SettingsAudio").SetMusicMute(
			!AudioServer.IsBusMute(AudioServer.GetBusIndex("Music")));
	}

	private void OnBtnMenuPressed()
	{
		GetNode<BasePanel>("Control/PnlMenu").Open();
	}

}
