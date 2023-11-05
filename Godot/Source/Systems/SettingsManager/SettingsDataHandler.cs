
// Operates loading and saving of settings
using Godot;
using System.Collections.Generic;

public partial class SettingsDataHandler : Node
{

    private JSONDataHandler _JSONDataHandler = new();

	public void SaveToFile()
	{
        _JSONDataHandler.SaveToDisk(PackAllSettings(), "/Settings.json");
	}

    public SettingsDataContainer PackAllSettings()
    {
        return new SettingsDataContainer() {
            ActionEventMap = PackActionEventMap(),
            AudioSettings = PackAudioSettings(),
            GraphicsSettings = PackGraphicsSettings(),
		};
    }

	public bool LoadSettings(SettingsDataContainer data = null)
	{
        if (data == null)
        {
            string absolutePath = OS.GetUserDataDir() + "/Settings.json";
            string path = "/Settings.json";
            if (! System.IO.File.Exists(absolutePath))
            {
                GD.Print("SettingsDataHandler.cs: File at " + absolutePath + " not found.");
                return false;
            }
            data = _JSONDataHandler.LoadFromJSON<SettingsDataContainer>(path);
        }
		LoadControls(data.ActionEventMap);
		LoadAudio(data.AudioSettings);
		LoadGraphics(data.GraphicsSettings);
		return true;
	}

    public bool UserSettingsExist()
    {
        return System.IO.File.Exists(OS.GetUserDataDir() + "/Settings.json");
    }


    private void LoadControls(Dictionary<string, List<SettingsControlActionData>> actionEventMap)
    {
        foreach (string action in actionEventMap.Keys)
        {
            AssignControls(action, actionEventMap[action]);
        }
    }
    
    private void AssignControls(string action, List<SettingsControlActionData> events)
    {
        InputMap.ActionEraseEvents(action);
        events.Sort((x, y) => {
            return (int)x.CurrentActionOrder - (int)y.CurrentActionOrder;
        });

        foreach (SettingsControlActionData data in events)
        {
            switch (data.CurrentActionMode)
            {
                case SettingsControlActionData.ActionMode.Key:
                    InputEventKey key = new InputEventKey();
                    key.Keycode = (Godot.Key) data.KeyCode;
                    key.PhysicalKeycode = (Godot.Key) data.PhysicalKeycode;
                    key.CtrlPressed = data.CtrlPressed;
                    key.AltPressed = data.AltPressed;
                    key.MetaPressed = data.MetaPressed;
                    key.ShiftPressed = data.ShiftPressed;
                    InputMap.ActionAddEvent(action, key);
                    break;
                case SettingsControlActionData.ActionMode.MouseBtn:
                    InputEventMouseButton btn = new InputEventMouseButton();
                    btn.CtrlPressed = data.CtrlPressed;
                    btn.AltPressed = data.AltPressed;
                    btn.MetaPressed = data.MetaPressed;
                    btn.ShiftPressed = data.ShiftPressed;
                    btn.ButtonIndex = (Godot.MouseButton) data.MouseButtonIndex;
                    InputMap.ActionAddEvent(action, btn);
                    break;
                case SettingsControlActionData.ActionMode.JoyMotion:
                    InputEventJoypadMotion joypadMotion = new InputEventJoypadMotion();
                    joypadMotion.Device = data.JoypadDevice;
                    joypadMotion.Axis = (Godot.JoyAxis) data.JoypadAxis;
                    joypadMotion.AxisValue = data.JoypadAxisValue;
                    InputMap.ActionAddEvent(action, joypadMotion);                    
                    break;
                case SettingsControlActionData.ActionMode.JoyBtn:
                    InputEventJoypadButton joyBtn = new InputEventJoypadButton();
                    joyBtn.Device = data.JoypadDevice;
                    joyBtn.ButtonIndex = (Godot.JoyButton) data.JoypadButtonIndex;
                    InputMap.ActionAddEvent(action, joyBtn); 
                    break;
            }
        }
    }

	private void LoadGraphics(SettingsGraphicsData data)
	{
        DisplayServer.WindowSetMode(data.CurrentWindowMode);
        DisplayServer.WindowSetCurrentScreen(data.CurrentScreen);
        Input.MouseMode = data.CurrentMouseMode;
        GetTree().Root.GetNode<GlobalSettings>("GlobalSettings").ScreenShakePercentModifier = data.CurrentScreenShakeMagnitude/100f;
        DisplayServer.WindowSetVsyncMode(data.CurrentVSyncMode);
        GetViewport().GetWindow().Size = data.CurrentWindowSizeOption;
	}

	private void LoadAudio(Dictionary<string, float> audioSettings)
	{
		if (audioSettings == null)
		{
			return;
		}
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Voice"), audioSettings["Voice"]);
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Effects"), audioSettings["Effects"]);
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), audioSettings["Music"]);
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), audioSettings["Master"]);
	}

    public SettingsGraphicsData PackGraphicsSettings()
    {
        return new SettingsGraphicsData()
        {
            CurrentWindowMode = DisplayServer.WindowGetMode(),
            CurrentScreen = DisplayServer.WindowGetCurrentScreen(),
            CurrentMouseMode = Input.MouseMode,
            CurrentScreenShakeMagnitude = GetTree().Root.GetNode<GlobalSettings>("GlobalSettings").ScreenShakePercentModifier*100,
            CurrentVSyncMode = DisplayServer.WindowGetVsyncMode(),
            CurrentWindowSizeOption = GetViewport().GetWindow().Size,
        };
    }

	public Dictionary<string, float> PackAudioSettings()
	{
		return new Dictionary<string, float>()
		{
			{"Voice", AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Voice"))},
			{"Effects", AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Effects"))},
			{"Music", AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"))},
			{"Master", AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"))}
		};
	}


    public Dictionary<string, List<SettingsControlActionData>> PackActionEventMap()
    {
        Dictionary<string, List<SettingsControlActionData>> result = new Dictionary<string, List<SettingsControlActionData>>();
        foreach (StringName action in InputMap.GetActions())
        {
            
            if (action.ToString().Substring(0,3) != ("ui_"))
            {
                result[action.ToString()] = new List<SettingsControlActionData>();
                for (int i = 0; i < InputMap.ActionGetEvents(action).Count; i++)
                {
                    if (i > 2)
                    {
                        // do not support more than 3 saved inputs
                        continue;
                    }
                    InputEvent ev = InputMap.ActionGetEvents(action)[i];
                    var savedAction = new SettingsControlActionData();
                    savedAction.CurrentActionOrder = (ActionRemapButton.ActionOrder) i;
                    
                    if (ev is InputEventKey key)
                    {
                        savedAction.CurrentActionMode = SettingsControlActionData.ActionMode.Key;
                        savedAction.KeyCode = (long) key.Keycode;
                        savedAction.PhysicalKeycode = (long) key.PhysicalKeycode;
                        savedAction.CtrlPressed = key.CtrlPressed;
                        savedAction.AltPressed = key.AltPressed;
                        savedAction.MetaPressed = key.MetaPressed;
                        savedAction.ShiftPressed = key.ShiftPressed;
                    }
                    else if (ev is InputEventMouseButton mouseButton)
                    {
                        savedAction.CurrentActionMode = SettingsControlActionData.ActionMode.MouseBtn;
                        savedAction.MouseButtonIndex = (long) mouseButton.ButtonIndex;
                        savedAction.CtrlPressed = mouseButton.CtrlPressed;
                        savedAction.AltPressed = mouseButton.AltPressed;
                        savedAction.MetaPressed = mouseButton.MetaPressed;
                        savedAction.ShiftPressed = mouseButton.ShiftPressed;
                    }
                    else if (ev is InputEventJoypadButton joyButton)
                    {
                        savedAction.CurrentActionMode = SettingsControlActionData.ActionMode.JoyBtn;
                        savedAction.JoypadDevice = joyButton.Device;
                        savedAction.JoypadButtonIndex = (long) joyButton.ButtonIndex;
                    }
                    else if (ev is InputEventJoypadMotion joyMotion)
                    {
                        savedAction.CurrentActionMode = SettingsControlActionData.ActionMode.JoyMotion;
                        savedAction.JoypadDevice = joyMotion.Device;
                        savedAction.JoypadAxis = (int) joyMotion.Axis;
                        savedAction.JoypadAxisValue = joyMotion.AxisValue;
                    }
                    result[action.ToString()].Add(savedAction);
                }
            }
        }

        return result;
    }
}