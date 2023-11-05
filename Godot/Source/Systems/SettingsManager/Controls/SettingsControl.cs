// SettingsControl: a component of the SettingsManager
using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsControl : Control
{

    private List<ActionRemapButton> _actionRemapButtons;
    public delegate void SettingsChangedDelegate();
    public event SettingsChangedDelegate SettingsChanged;

	public override void _Ready()
	{
        _actionRemapButtons = new List<ActionRemapButton>();
        // Populate list with all remap buttons
        foreach (Node hBox in GetNode("Panel/ScrollContainer/VBoxContainer").GetChildren())
        {
            foreach (Node n in hBox.GetChildren())
                {
                if (n is ActionRemapButton actionRemapButton)
                {
                    _actionRemapButtons.Add(actionRemapButton);
                }
            }
        }

        // Connect signals
        foreach (ActionRemapButton remapButton in _actionRemapButtons)
        {
            remapButton.ShowingHint+=GetNode<SettingsLabelHint>("Panel/LabelHint").DisplayHintText;
            remapButton.KeybindChanged+=this.OnKeybindChanged;
        }
	}

    private void OnKeybindChanged()
    {
        SettingsChanged?.Invoke();
    }

    public void Refresh()
    {
        foreach (ActionRemapButton remapButton in _actionRemapButtons)
        {
            remapButton.RefreshButtonText();
        }
    }

    public void Exit()
    {
        foreach (ActionRemapButton remapButton in _actionRemapButtons)
        {
            remapButton.Exit();
        }
        SettingsChanged = null;
    }

}
