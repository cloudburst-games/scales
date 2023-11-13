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
    public PnlCharacterInfo _characterInfoPanel;
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
    [Export]
    private PackedScene _lblFloatText;
    [Signal]
    public delegate void UIPauseEventHandler(bool pause);
    [Export]
    private Label _lblHint;
    [Export]
    private Panel _pnlShortLog;
    [Export]
    private Panel _pnlAction;
    [Export]
    private TextureRect _texActiveCharacter;
    [Export]
    private ProgressBar _barHealth;
    [Export]
    private ProgressBar _barHerbs;
    [Export]
    private ProgressBar _barCharge;

    public enum StateMode
    {
        BattleIntro, BattleStarted, LogOpened, LogClosed, SpellBookOpened, SpellBookClosed,
        HintClickedCharacterLeftClick, HintClickedCharacterRightClick, HintClickedCharacterEnded
    }

    // private StateMode _state = StateMode.BattleIntro;

    public override void _Ready()
    {
        // connect buttons
        _btnBattleIntroContinue.Pressed += this.OnBtnBattleIntroPressed;
        _btnLog.Pressed += this.OnBtnLogPressed;
        _pnlFullLog.CloseBtn.Pressed += this.OnCloseLogPressed;
        _btnCloseSpellBook.Pressed += this.OnCloseSpellBookPressed;
        _pnlShortLog.Visible = false;
        _characterInfoPanel.HintClickCharacterEnded += () => this.SetState(StateMode.HintClickedCharacterEnded);
        _characterInfoPanel.MouseOverAttributeEntered += (string text) => OnBattleLogEntry(text, false);
        _characterInfoPanel.MouseOverAttributeExited += () => OnBattleLogEntry("", false);
        _characterInfoPanel.PlaceableArea = new Vector2(_characterInfoPanel.GetViewportRect().Size.X, _pnlAction.GlobalPosition.Y);

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
            case StateMode.HintClickedCharacterLeftClick:
                HintClickCharacterStateCommon();
                break;
            case StateMode.HintClickedCharacterRightClick:
                HintClickCharacterStateCommon();
                break;
            case StateMode.HintClickedCharacterEnded:
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                break;

        }
    }

    public void OnCharacterStartTurn(StoryCharacterData characterData)
    {
        _characterInfoPanel.SetPortrait(_texActiveCharacter, characterData);
        UpdateBars(characterData);
    }

    public void UpdateBars(StoryCharacterData characterData)
    {
        _barHealth.Value =
            (float)characterData.Stats[StoryCharacterData.StatMode.Health] / (float)characterData.Stats[StoryCharacterData.StatMode.MaxHealth] * 100f;
        _barCharge.Value =
            (float)characterData.Stats[StoryCharacterData.StatMode.FocusCharge] / (float)characterData.Stats[StoryCharacterData.StatMode.MaxFocusCharge] * 100f;
        _barHerbs.Value =
            (float)characterData.Stats[StoryCharacterData.StatMode.Reagents] / (float)characterData.Stats[StoryCharacterData.StatMode.MaxReagents] * 100f;
    }

    private void HintClickCharacterStateCommon()
    {
        _pnlFullLog.Close();
        _pnlSpellBook.Close();
        EmitSignal(BattleHUD.SignalName.UIPause, true);
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
            _pnlShortLog.Visible = true;
            _persistentLogText.Add(text);
            _RLblFullLog.AddText(text + "\n");
            _lblShortLog.Text = text;
        }
        // if (text == "" && _persistentLogText.Count > 0)
        // {
        //     _lblShortLog.Text = "";// _persistentLogText[^1];
        // }
        else
        {
            _lblHint.Text = text;
            // _lblShortLog.Text = text;
        }
    }

    internal void SetSpellBookDisplayedSpells(Array<SpellEffectManager.SpellMode> spells)
    {
        _cntSpellBook.ShowSpells(spells);
    }

    internal void OnCharacterRoundEffectApplied(CharacterUnit characterUnit, CharacterRoundEffect roundEffect)
    {
        // LblFloatingText lbl = _lblFloatText.Instantiate<LblFloatingText>();
        // AddChild(lbl);
        // lbl.Text = roundEffect.Name;
        // lbl.Start(characterUnit.GlobalPosition);
        OnBattleLogEntry(characterUnit.CharacterData.Name + " affected by: " + roundEffect.Name, true);
    }

    internal void OnCharacterRoundEffectFaded(CharacterUnit characterUnit, CharacterRoundEffect roundEffect)
    {
        OnBattleLogEntry(characterUnit.CharacterData.Name + " no longer affected by: " + roundEffect.Name, true);
    }
    public void OnPnlActionUIHint(PnlAction.UIHint hint)
    {
        OnBattleLogEntry(BattleLogParser.ParseUIHint(hint), false);
    }

    public void OnSpellBookUIHint(SpellEffectManager.Spell spell)
    {
        OnBattleLogEntry(BattleLogParser.ParseSpellHint(spell), false);
    }

    internal void OnCharacterTakingDamage(BattleRoller.RollerOutcomeInformation result, string defender, Vector2 globalPosition)
    {
        if (result.RollerInput == null)
        {
            return;
        }
        LblFloatingText lbl = _lblFloatText.Instantiate<LblFloatingText>();
        string roll = string.Format("{0} + {1} vs {2} + {3}", result.AttackerD20Roll, result.RollerInput.AttackerHitModifier,
            result.DefenderD20Roll, result.RollerInput.DefenderDodgeModifier);
        if (result.RollerInput.AttackType == BattleRoller.AttackType.AreaConfirmedHalfDamage || result.RollerInput.AttackType == BattleRoller.AttackType.AreaConfirmed)
        {
            roll = "";
        }
        string hit = "";
        string damage = result.FinalDamage > 0 ? result.FinalDamage.ToString() : "";
        switch (result.RollResult)
        {
            case BattleRoller.RollResult.CriticalHit:
                hit = "Critical hit!";
                break;
            case BattleRoller.RollResult.CriticalMiss:
                hit = "Critical miss!";
                break;
            case BattleRoller.RollResult.Hit:
                hit = "Hit!";
                break;
            case BattleRoller.RollResult.Miss:
                hit = "Miss!";
                break;
        }
        GetNode("CntHUD").AddChild(lbl);

        lbl.Text = string.Format("{0}{1}", hit, damage != "" ? "\n" + damage : "");
        lbl.Start(globalPosition - new Vector2(0, 300)); ;
        OnBattleLogEntry(string.Format("{3}{0}\n{1} takes {2} damage!", hit, defender, result.FinalDamage, roll == "" ? "" : "Roll " + roll + ": "), true);
    }

    public void OnHintClickCharacter(bool rightClick, StoryCharacterData data)
    {
        if (rightClick)
        {
            _characterInfoPanel.OnRightClick(data);
            SetState(StateMode.HintClickedCharacterRightClick);
        }
        else
        {
            _characterInfoPanel.OnLeftClick(data);
            SetState(StateMode.HintClickedCharacterLeftClick);
        }
    }

}

// internal void OnCharacterTakingDamage(BattleRoller.RollerOutcomeInformation result, string defender, Vector2 globalPosition)
// {
//     LblFloatingText lbl = _lblFloatText.Instantiate<LblFloatingText>();
//     string roll = "";
//     string hit = "";
//     if (result.RollerInput != null)
//     {
//         roll = string.Format("{0} + {1} vs {2} + {3}", result.AttackerD20Roll, result.RollerInput.AttackerHitModifier,
//             result.DefenderD20Roll, result.RollerInput.DefenderDodgeModifier);
//         if (result.RollerInput.AttackType == BattleRoller.AttackType.AreaConfirmedHalfDamage || result.RollerInput.AttackType == BattleRoller.AttackType.AreaConfirmed)
//         {
//             roll = "";
//         }
//         string damage = result.FinalDamage > 0 ? result.FinalDamage.ToString() : "";
//         switch (result.RollResult)
//         {
//             case BattleRoller.RollResult.CriticalHit:
//                 hit = "Critical hit!";
//                 break;
//             case BattleRoller.RollResult.CriticalMiss:
//                 hit = "Critical miss!";
//                 break;
//             case BattleRoller.RollResult.Hit:
//                 hit = "Hit!";
//                 break;
//             case BattleRoller.RollResult.Miss:
//                 hit = "Miss!";
//                 break;
//         }
//         GetNode("CntHUD").AddChild(lbl);
//         // GD.Print(result.RollResult);
//         // GD.Print(globalPosition);
//         // GD.Print(lbl.GetGlobalMousePosition());
//         // GD.Print(lbl.GlobalPosition);

//         lbl.Text = string.Format("{0}{1}", hit, damage != "" ? "\n" + damage : "");
//         lbl.Start(globalPosition - new Vector2(0, 300));
//     }
//     OnBattleLogEntry(string.Format("{3}{0}{1} takes {2} damage!", hit == "" ? "" : hit + "\n", defender, result.FinalDamage, roll == "" ? "" : "Roll " + roll + ": "), true);
// }