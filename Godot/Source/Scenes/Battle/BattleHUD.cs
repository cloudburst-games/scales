using Godot;
using System;
using System.Collections.Generic;

public partial class BattleHUD : CanvasLayer
{
    // Reference nodes
    [Export]
    private BasePanel _pnlBattleIntro;
    [Export]
    private BaseButton _btnBattleIntroContinue;
    [Export]
    private Label _lblIntro;
    [Export]
    private Label _lblShortLog;
    [Export]
    private BaseButton _btnLog;
    [Export]
    private BasePanel _pnlFullLog;
    [Export]
    private RichTextLabel _RLblFullLog;

    [Signal]
    public delegate void UIPauseEventHandler(bool pause);

    public enum StateMode { BattleIntro, BattleStarted, LogOpened, LogClosed }

    // private StateMode _state = StateMode.BattleIntro;

    public override void _Ready()
    {
        // connect buttons
        _btnBattleIntroContinue.Pressed += this.OnBtnBattleIntroPressed;
        _btnLog.Pressed += this.OnBtnLogPressed;
        _pnlFullLog.CloseBtn.Pressed += this.OnCloseLogPressed;

        // SetState(StateMode.BattleIntro);
    }

    private void OnBtnLogPressed()
    {
        SetState(StateMode.LogOpened);
    }

    public void SetIntroText(string text)
    {
        _lblIntro.Text = text;
    }

    private void OnCloseLogPressed()
    {
        SetState(StateMode.LogClosed);
    }

    // BattleIntro => BattleStarted
    // BattleStarted => 

    public void SetState(StateMode stateMode)
    {
        switch (stateMode)
        {
            case StateMode.BattleIntro:
                _pnlBattleIntro.Open();
                break;
            case StateMode.BattleStarted:
                _pnlBattleIntro.Close();
                break;
            case StateMode.LogOpened:
                _pnlFullLog.Open();
                EmitSignal(BattleHUD.SignalName.UIPause, true);
                break;
            case StateMode.LogClosed:
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                break;
        }
    }

    private void OnBtnBattleIntroPressed()
    {
        SetState(StateMode.BattleStarted);
    }

    private List<string> _persistentLogText = new();

    public void OnBattleLogEntry(string text, bool persist)
    {
        if (persist)
        {
            _persistentLogText.Add(text);
            _RLblFullLog.AddText(text + "\n");
        }
        if (text == "" && _persistentLogText.Count > 0)
        {
            _lblShortLog.Text = "";// _persistentLogText[^1];
        }
        else
        {
            _lblShortLog.Text = text;
        }
    }
}
