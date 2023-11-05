// ActionRemapButton: logic for user changing controls
// Allows for multiple bindings for each action (Primary, Secondary, Tertiary)
// Support for mouse / keyboard / controller

// TODO
// Low priority: controller UI (indicator texture moveable via joypad, "A" to select)
// Low priority: make all the label text more readable, esp controllers
// Low priority: bug on trying to assign modifier + key when already assigned - assigns modifier alone instead
// Low priority: when a key is already bound to the desired input eg binding says A and user tries to bind it to A,
// no error message should occur

using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[Tool]
public partial class ActionRemapButton : TextureButton
{

    private string _action;
    public string Action {
        get => _action;
        set
        {
            _action = value;
            NotifyPropertyListChanged();
        }
    }

    [Export]
    public ActionOrder _actionOrder = ActionOrder.Primary;

    public delegate void KeybindChangedDelegate();
    public event KeybindChangedDelegate KeybindChanged;

    public enum ActionOrder { Primary, Secondary, Tertiary }

    private Label _label;

    private string _currentKeyDisplay;
	
    public delegate void ShowingHintDelegate(string hint);
    public event ShowingHintDelegate ShowingHint;

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        var properties = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        properties.Add(new Godot.Collections.Dictionary()
        {
            {"name", "Action" },
            { "type", (int) Variant.Type.String},
            { "usage", (int) PropertyUsageFlags.Default},
            { "hint", (int) PropertyHint.Enum },
            { "hint_string", GlobalSettings.ALL_INPUT_ACTIONS}
        });
        return properties;
    }

	public override void _Ready()
	{
        if (Engine.IsEditorHint())
        {
            Action = GlobalSettings.ALL_INPUT_ACTIONS.Split(',')[0];
        }
        else
        {
            // KeybindChanged+=this.DisplayCurrentKey;
            _label = GetNode<Label>("Label");
            if (!InputMap.HasAction(_action))
            {
                GD.PrintErr("ActionRemapButton: Specified action " + _action + " does not exist in InputMap");
                throw new Exception();
            }
            if (InputMap.ActionGetEvents(_action).Count < (int) _actionOrder + 1)
            {
                GD.PrintErr("ActionRemapButton: Specified action " + _action + " does not have action order " + _actionOrder);
                throw new Exception();
            }
            SetProcessInput(false);
            
            InputEvent currentKey = InputMap.ActionGetEvents(_action)[(int)_actionOrder];
            _currentKeyDisplay = GetReadableKeyDisplay(currentKey);

            DisplayCurrentKey();
        }
	}

    public void RefreshButtonText()
    {
        InputEvent currentKey = InputMap.ActionGetEvents(_action)[(int)_actionOrder];
        _currentKeyDisplay = GetReadableKeyDisplay(currentKey);

        DisplayCurrentKey();
    }

    public override void _Toggled(bool buttonPressed)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        base._Toggled(buttonPressed);
        SetProcessInput(buttonPressed);
        Disabled = buttonPressed;
        if (buttonPressed)
        {
            _label.Text = "... Key";
            ReleaseFocus();
        }
        else
        {
            DisplayCurrentKey();
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        base._Input(ev);
        if (ev is InputEventMouseMotion)
        {
            return;
        }
        if (ev is InputEventJoypadMotion)
        {
            RemapActionTo(ev);
            ButtonPressed = false;
            return;
        }
        if (ev.IsPressed())
        {
            RemapActionTo(ev);
        }
        else
        {
            ButtonPressed = false;
        } 
    }

    private void RemapActionTo(InputEvent ev)
    {
        // fixes double click bug...
        if (ev is InputEventMouseButton eventMouseButton)
        {
            if (eventMouseButton.DoubleClick)
            {
                eventMouseButton.DoubleClick = false;
            }
        }
        if (ev is InputEventJoypadMotion inputEventJoypadMotion)
        {
            ev = GetDiscreteJoyMotion(inputEventJoypadMotion);
        }
        InputEvent actionPrimary = _actionOrder == ActionOrder.Primary ? ev : InputMap.ActionGetEvents(_action)[0];
        InputEvent actionSecondary = actionPrimary;
        InputEvent actionTertiary = actionPrimary;
        if (InputMap.ActionGetEvents(_action).Count > 1)
        {
            actionSecondary = _actionOrder == ActionOrder.Secondary ? ev : InputMap.ActionGetEvents(_action)[1];
        }
        if (InputMap.ActionGetEvents(_action).Count > 2)
        {
            actionTertiary = _actionOrder == ActionOrder.Tertiary ? ev : InputMap.ActionGetEvents(_action)[2];
        }

        Tuple<string,int> alreadyUsed = EventCurrentlyUsedBy(ev);
        if (alreadyUsed != null)
        {
            ShowingHint?.Invoke("Binding already in use by " + alreadyUsed.Item1);
        }
        else
        {
            InputMap.ActionEraseEvents(_action);
            InputMap.ActionAddEvent(_action, actionPrimary);
            InputMap.ActionAddEvent(_action, actionSecondary);
            InputMap.ActionAddEvent(_action, actionTertiary);
            KeybindChanged?.Invoke();

            _currentKeyDisplay = GetReadableKeyDisplay(ev);
        }

        DisplayCurrentKey();
    }

    // todo: refactor (too repetitive)
    private Tuple<string,int> EventCurrentlyUsedBy(InputEvent ev)
    {
        foreach (string action in InputMap.GetActions())
        {
            if (action.Substring(0, 3) == "ui_") // ignore built in UI
            {
                continue;
            }
            for (int i = 0; i < InputMap.ActionGetEvents(action).Count; i++)
            {
                if (ev is InputEventKey currentEvKey && InputMap.ActionGetEvents(action)[i] is InputEventKey oldEvKey)
                {
                    if (EventKeyTakenBy(currentEvKey, oldEvKey))
                    {
                        return Tuple.Create(action, i);
                    }
                }
                else if (ev is InputEventMouseButton currentEvMouseBtn && 
                    InputMap.ActionGetEvents(action)[i] is InputEventMouseButton oldEvMouseBtn)
                {
                    if (EventMouseButtonTakenBy(currentEvMouseBtn, oldEvMouseBtn))
                    {
                        return Tuple.Create(action, i);
                    }
                }
                else if (ev is InputEventJoypadButton currentEvJoyBtn &&
                    InputMap.ActionGetEvents(action)[i] is InputEventJoypadButton oldEvJoyBtn)
                {
                    if (EventJoypadButtonTakenBy(currentEvJoyBtn, oldEvJoyBtn))
                    {
                        return Tuple.Create(action, i);
                    }
                }
                else if (ev is InputEventJoypadMotion currentEvJoyMotion &&
                    InputMap.ActionGetEvents(action)[i] is InputEventJoypadMotion oldEvJoyMotion)
                {
                    if (EventJoypadMotionTakenBy(currentEvJoyMotion, oldEvJoyMotion))
                    {
                        return Tuple.Create(action, i);
                    }
                }
            }
        }
        return null;
    }

    private void DisplayCurrentKey()
    {
        _label.Text = _currentKeyDisplay;
    }

    // If we want to make tight joypad controls, we will need to comment this out and code for the specific axis range
    private InputEvent GetDiscreteJoyMotion(InputEventJoypadMotion eventJoypadMotion)
    {
        eventJoypadMotion.AxisValue = eventJoypadMotion.AxisValue < 0 ? -1 : 1;
        return eventJoypadMotion;
    }

    private string GetReadableKeyDisplay(InputEvent ev)
    {
        string result = ev.AsText();
		if (ev is InputEventJoypadMotion eventJoypadMotion)
		{
			result = string.Format("{0}, Axis {1}: {2}", eventJoypadMotion.Device == -1 ? "Any Joy" : "Joy " + eventJoypadMotion.Device, eventJoypadMotion.Axis, eventJoypadMotion.AxisValue);
		}
        else if (ev is InputEventJoypadButton eventJoypadButton)
        {
            result = string.Format("{0}, Btn {1}", eventJoypadButton.Device == -1 ? "Any Joy" : "Joy " + eventJoypadButton.Device, eventJoypadButton.ButtonIndex);
        }
        // Get rid of anything inside brackets (including brackets)
        return Regex.Replace(result, @" \(.*?\)","");
    }

	private bool EventKeyTakenBy(InputEventKey currentEv, InputEventKey oldEv)
	{
		return (currentEv.Keycode == oldEv.Keycode || currentEv.PhysicalKeycode == oldEv.Keycode
            || currentEv.Keycode == oldEv.PhysicalKeycode || currentEv.PhysicalKeycode == oldEv.PhysicalKeycode)
            && currentEv.ShiftPressed == oldEv.ShiftPressed && currentEv.CtrlPressed == oldEv.CtrlPressed
            && currentEv.AltPressed == oldEv.AltPressed && currentEv.MetaPressed == oldEv.MetaPressed;
	}
	private bool EventMouseButtonTakenBy(InputEventMouseButton currentEv, InputEventMouseButton oldEv)
	{
		return currentEv.ButtonIndex == oldEv.ButtonIndex
            && currentEv.ShiftPressed == oldEv.ShiftPressed && currentEv.CtrlPressed == oldEv.CtrlPressed
            && currentEv.AltPressed == oldEv.AltPressed && currentEv.MetaPressed == oldEv.MetaPressed;
	}

	private bool EventJoypadButtonTakenBy(InputEventJoypadButton currentEv, InputEventJoypadButton oldEv)
	{
		return currentEv.Device == oldEv.Device && currentEv.ButtonIndex == oldEv.ButtonIndex;
	}

    // If we want to make tight joypad controls, we will need to alter this and code for the specific axis range
	private bool EventJoypadMotionTakenBy(InputEventJoypadMotion currentEv, InputEventJoypadMotion oldEv)
	{
		int currentEvAxisValue = currentEv.AxisValue < 0 ? -1 : 1;
		int oldEvAxisValue = oldEv.AxisValue < 0 ? -1 : 1;
		return currentEv.Device == oldEv.Device && currentEv.Axis == oldEv.Axis && currentEvAxisValue == oldEvAxisValue;
	}

    public void Exit()
    {
        ShowingHint = null;
        KeybindChanged = null;
    }

}