// SettingsManager: Coordinates UI actions with the game settings - the UI glue for the various settings scripts.

// TODO add a Game panel for gameplay specific options

using Godot;
using System;

public partial class SettingsManager : Node
{
    private GlobalSettings _globalSettings;
    private SettingsDataHandler _dataHandler = new SettingsDataHandler();

    public override void _Ready()
    {
        if (GetTree().Root.HasNode("GlobalSettings"))
        {
            _globalSettings = GetTree().Root.GetNode<GlobalSettings>("GlobalSettings");
        }
        else
        {
            GD.PrintErr("Singleton GlobalSettings does not exist.");
            throw new Exception();
        }

        AddChild(_dataHandler);

        GetNode<SettingsControl>("Panel/SettingsControl").SettingsChanged += this.OnSettingsChanged;
        GetNode<SettingsAudio>("Panel/SettingsAudio").SettingsChanged += this.OnSettingsChanged;
        GetNode<SettingsGraphics>("Panel/SettingsGraphics").SettingsChanged += this.OnSettingsChanged;
        SetPnlConfirmCloseVisible(false);
        LoadOrDefault();
        RefreshSettingsDisplay();
        GetNode<TabBar>("Panel/TabBar").CurrentTab = 0;
        OnTabChanged(0);
        Hide();
    }

    public void OnTabChanged(int tab)
    {
        // 0 control; 1 audio; 2 graphics
        GetNode<Control>("Panel/SettingsAudio").Visible = GetNode<Control>("Panel/SettingsControl").Visible =
        GetNode<Control>("Panel/SettingsGraphics").Visible = false;

        switch (tab)
        {
            case 0:
                GetNode<Control>("Panel/SettingsControl").Visible = true;
                break;
            case 1:
                GetNode<Control>("Panel/SettingsAudio").Visible = true;
                break;
            case 2:
                GetNode<Control>("Panel/SettingsGraphics").Visible = true;
                break;
        }
    }

    public void Show()
    {
        RefreshSettingsDisplay();
        GetNode<Panel>("Panel").Visible = true;
    }
    public void Hide()
    {
        GetNode<Panel>("Panel").Visible = false;
    }

    private void RefreshSettingsDisplay()
    {
        GetNode<SettingsControl>("Panel/SettingsControl").Refresh();
        GetNode<SettingsAudio>("Panel/SettingsAudio").Refresh();
        GetNode<SettingsGraphics>("Panel/SettingsGraphics").Refresh();
    }

    private void OnBtnDefaultPressed()
    {
        _dataHandler.LoadSettings(_globalSettings.DefaultSettings);
        GetNode<TextureButton>("Panel/HBoxContainer/BtnApply").Disabled = false;
        RefreshSettingsDisplay();
    }

    private void LoadOrDefault()
    {
        if (_dataHandler.UserSettingsExist())
        {
            _dataHandler.LoadSettings();
        }
        else
        {
            _dataHandler.LoadSettings(_globalSettings.DefaultSettings);
        }
        GetNode<TextureButton>("Panel/HBoxContainer/BtnApply").Disabled = true;
    }

    private void OnBtnClosePressed()
    {
        if (GetNode<TextureButton>("Panel/HBoxContainer/BtnApply").Disabled)
        {
            GetNode<BasePanel>("Panel").Close();
        }
        else
        {
            SetPnlConfirmCloseVisible(true);
        }
    }

    private void SetPnlConfirmCloseVisible(bool show)
    {
        GetNode<Control>("Panel/InputDisabler").Visible = GetNode<Panel>("Panel/PnlConfirmClose").Visible = show;
    }

    private void OnBtnCancelPressed()
    {
        LoadOrDefault();
        RefreshSettingsDisplay();
    }

    private void OnBtnConfirmCancelPressed()
    {
        SetPnlConfirmCloseVisible(false);
    }

    private void OnBtnRevertPressed()
    {
        LoadOrDefault();
        RefreshSettingsDisplay();
        SetPnlConfirmCloseVisible(false);
        GetNode<Panel>("Panel").Visible = false;
    }

    private void OnBtnApplyPressed()
    {
        _dataHandler.SaveToFile();
        GetNode<TextureButton>("Panel/HBoxContainer/BtnApply").Disabled = true;
    }

    private void OnSettingsChanged()
    {
        GetNode<TextureButton>("Panel/HBoxContainer/BtnApply").Disabled = false;
    }

    // On freeing need to run this first! E.g. changing scene.
    public void Exit()
    {
        GetNode<SettingsControl>("Panel/SettingsControl").Exit();
        GetNode<SettingsAudio>("Panel/SettingsAudio").Exit();
        GetNode<SettingsGraphics>("Panel/SettingsGraphics").Exit();
    }
}


