// UI idea: for actions, click button to pop out the actions (to the right, over the combat log) -> select which action is preferred -> Melee/Ranged/Spells/Move
// Need a separate button still to select a spell - this opens the spellbook, and when a spell is selected, the action preferred is set to Spells also (if cancelled nothing happens)
// Aim to have buttons equal - consider moving ToggleGrid in or out of Menu (which would also have resume/settings/anim speed/Main Menu)

using Godot;
using Godot.Collections;
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
    [Export]
    private BasePanel _pnlSpellBook;
    [Export]
    private BaseButton _btnCloseSpellBook;
    [Export]
    private CntSpellBook _cntSpellBook;
    [Signal]
    public delegate void UIPauseEventHandler(bool pause);

    public enum StateMode { BattleIntro, BattleStarted, LogOpened, LogClosed, SpellBookOpened, SpellBookClosed }

    // private StateMode _state = StateMode.BattleIntro;

    public override void _Ready()
    {
        // connect buttons
        _btnBattleIntroContinue.Pressed += this.OnBtnBattleIntroPressed;
        _btnLog.Pressed += this.OnBtnLogPressed;
        _pnlFullLog.CloseBtn.Pressed += this.OnCloseLogPressed;
        _btnCloseSpellBook.Pressed += this.OnCloseSpellBookPressed;

        // SetState(StateMode.BattleIntro);
    }

    private void OnCloseSpellBookPressed()
    {
        SetState(StateMode.SpellBookClosed);
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
                _pnlSpellBook.Close();
                EmitSignal(BattleHUD.SignalName.UIPause, true);
                break;
            case StateMode.LogClosed:
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                break;
            case StateMode.SpellBookClosed:
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                _pnlSpellBook.Close();
                break;
            case StateMode.SpellBookOpened:
                _pnlSpellBook.Open();
                _pnlFullLog.Close();
                EmitSignal(BattleHUD.SignalName.UIPause, true);
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

    internal void SetSpellBookDisplayedSpells(Array<SpellEffectManager.SpellMode> spells)
    {
        _cntSpellBook.ShowSpells(spells);
    }

    internal void OnCharacterRoundEffectApplied(CharacterRoundEffect roundEffect)
    {
        GD.Print("todo: announce on log ", roundEffect.Name);
    }
}
