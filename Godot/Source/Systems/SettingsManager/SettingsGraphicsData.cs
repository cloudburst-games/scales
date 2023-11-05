// Container for saveable graphics data

using Godot;

public class SettingsGraphicsData
{
    public Godot.DisplayServer.WindowMode CurrentWindowMode {get; set;}
    public int CurrentScreen {get; set;}
    public Godot.Input.MouseModeEnum CurrentMouseMode {get; set;}
    public float CurrentScreenShakeMagnitude {get; set;}
    public Godot.DisplayServer.VSyncMode CurrentVSyncMode {get; set;}
    public Vector2I CurrentWindowSizeOption {get; set;}
}