// GlobalSettings script
// Purpose is to contain settings that exist globally between scene transitions
// This essentially only stores the default settings so that the user can easily revert to it
using Godot;
using System;
using System.Collections.Generic;

public partial class GlobalSettings : Node
{
    // Add player-changeable input actions here.
    public const string ALL_INPUT_ACTIONS = "Move Up,Move Down,Move Left,Move Right,Jump,Pause,Journal,Shoot,Map,Melee,Switch Weapon";

    public float ScreenShakePercentModifier {get; set;} = 1;

    public SettingsDataContainer DefaultSettings {get; private set;}
    private SettingsDataHandler _settingsDataHandler = new();
	public override void _Ready()
	{
        AddChild(_settingsDataHandler);
        DefaultSettings = GetDefaultSettings();
	}

    private SettingsDataContainer GetDefaultSettings()
    {
        return _settingsDataHandler.PackAllSettings();
    } 

}
