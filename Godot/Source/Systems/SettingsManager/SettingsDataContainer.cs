// Container for saveable settings
// Need to extend as the Settings Manager evolves

using System.Collections.Generic;

public class SettingsDataContainer : IJSONSaveable
{

	public Dictionary<string, List<SettingsControlActionData>> ActionEventMap {get; set;}
	public Dictionary<string, float> AudioSettings {get; set;}
	// public bool GraphicsFullScreen {get; set;}
    public SettingsGraphicsData GraphicsSettings  {get; set;}
}
