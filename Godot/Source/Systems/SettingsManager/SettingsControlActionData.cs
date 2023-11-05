// Container for saveable controls data

public class SettingsControlActionData
{
    public enum ActionMode { Key, MouseBtn, JoyBtn, JoyMotion }

    public ActionMode CurrentActionMode {get; set;} = ActionMode.Key;
    public ActionRemapButton.ActionOrder CurrentActionOrder {get; set;} = ActionRemapButton.ActionOrder.Primary;
    
    // key
    public long KeyCode {get; set;} = (long) Godot.Key.None;
    public long PhysicalKeycode {get; set;} = (long) Godot.Key.None;
    public bool CtrlPressed {get; set;} = false;
    public bool AltPressed {get; set;} = false;
    public bool MetaPressed {get; set;} = false;
    public bool ShiftPressed {get; set;} = false;

    // mouse
    public long MouseButtonIndex {get; set;} = 0;
    
    // joy
    public int JoypadDevice {get; set;} = 0;
    public long JoypadButtonIndex {get; set;} = 0;
    public int JoypadAxis {get; set;} = 0;
    public float JoypadAxisValue {get; set;} = 0;
}