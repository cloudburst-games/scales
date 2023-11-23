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
    [Export]
    private Panel _pnlScales;
    [Export]
    private AudioContainer _audioCloseBook;
    [Export]
    private AudioContainer _audioOpenBook;
    [Export]
    private AnimationPlayer _animTutorial;
    [Export]
    private BaseTextureButton _btnMenu;
    [Export]
    private BaseTextureButton _btnMenuClose;
    [Export]
    private BasePanel _pnlMenu;
    [Export]
    private BaseTextureButton _btnGridAll;
    [Export]
    private BaseTextureButton _btnGridContextual;
    [Export]
    private BaseTextureButton _btnGridNone;
    [Signal]
    public delegate void GridDisplayBtnPressedEventHandler(int gridDisplay);
    [Export]
    private BaseTextureButton _btnSettings;
    [Export]
    private SettingsManager _settingsManager;

    // public delegate void PortraitMouseEnteredEventHandler(StoryCharacterData data);
    // public event PortraitMouseEnteredEventHandler PortraitMouseEntered;

    private HBoxTurnOrder _hBoxTurnOrder;
    // private Callable _hBoxOnEnteredCallable;
    // private Callable _hBoxOnExited;
    public enum StateMode
    {
        BattleIntro, BattleStarted, LogOpened, LogClosed, SpellBookOpened, SpellBookClosed,
        HintClickedCharacterLeftClick, HintClickedCharacterRightClick, HintClickedCharacterEnded,
        MenuOpened, MenuClosed,
        SettingsOpened,
        SettingsClosed
    }

    // private StateMode _state = StateMode.BattleIntro;

    public override void _Ready()
    {
        // connect buttons
        _btnBattleIntroContinue.Pressed += this.OnBtnBattleIntroPressed;
        _btnLog.Pressed += this.OnBtnLogPressed;
        GetNode<BaseTextureButton>("CntHUD/PnlFullLog/BtnLogClose").Pressed += this.OnCloseLogPressed;
        _btnCloseSpellBook.Pressed += this.OnCloseSpellBookPressed;
        _pnlShortLog.Visible = false;
        _characterInfoPanel.HintClickCharacterEnded += () => this.SetState(StateMode.HintClickedCharacterEnded);
        _characterInfoPanel.MouseOverAttributeEntered += (string text) => OnBattleLogEntry(text, false);
        _characterInfoPanel.MouseOverAttributeExited += () => OnBattleLogEntry("", false);
        _pnlScales.MouseEntered += () => OnBattleLogEntry("Mystical gauges reflecting the cosmic balance between Shamash and Ishtar.", false);
        _pnlScales.MouseExited += () => OnBattleLogEntry("", false);
        _characterInfoPanel.PlaceableArea = new Vector2(_characterInfoPanel.GetViewportRect().Size.X, _pnlAction.GlobalPosition.Y);
        _btnMenu.Pressed += () => this.SetState(StateMode.MenuOpened);
        _btnMenuClose.Pressed += () => { this.SetState(StateMode.MenuClosed); };
        _btnSettings.Pressed += () => { this.SetState(StateMode.SettingsOpened); };
        _settingsManager.FinalClosed += () => this.SetState(StateMode.SettingsClosed);

        _btnGridAll.Pressed += () => EmitSignal(SignalName.GridDisplayBtnPressed, (int)HexGridUserDisplay.DisplayMode.ShowAllHexes);
        _btnGridContextual.Pressed += () => EmitSignal(SignalName.GridDisplayBtnPressed, (int)HexGridUserDisplay.DisplayMode.ShowContextualHexes);
        _btnGridNone.Pressed += () => EmitSignal(SignalName.GridDisplayBtnPressed, (int)HexGridUserDisplay.DisplayMode.HideAllHexes);
        _hBoxTurnOrder = GetNode<HBoxTurnOrder>("CntHUD/PnlAction/HBoxContainer/VBoxContainer/HBoxTurnOrder");
        _texActiveCharacter.MouseExited += () => _hBoxTurnOrder.OnMouseExited();
        _texActiveCharacter.MouseEntered += this.OnTexActiveCharacterMouseEntered;
        _hBoxTurnOrder.CharacterClicked += OnHintClickCharacter;

        // _hBoxOnEnteredCallable = new Callable(_hBoxTurnOrder, HBoxTurnOrder.MethodName.OnMouseEntered);
        // _hBoxOnExited = new Callable(_hBoxTurnOrder, HBoxTurnOrder.MethodName.OnMouseExited);
        // SetState(StateMode.BattleIntro);
        InitActionBtns();
    }

    private void InitActionBtns()
    {

        // magic strings are annoying BUT EXPORTED VARIABLES KEEP DISAPPEARING!!!
        BtnActions btnActions = GetNode<BtnActions>("CntHUD/PnlAction/HBoxContainer/VBoxContainer/HBoxBtnsLog/BtnActions");
        var actionBtnsDict = btnActions.ActionBtns = new() {
            {Battler.ActionMode.Melee, GetNode<TextureButton>("CntHUD/PnlAction/PnlActions/HBoxContainer/BtnAttack")},
            {Battler.ActionMode.Shoot, GetNode<TextureButton>("CntHUD/PnlAction/PnlActions/HBoxContainer/BtnShoot")},
            {Battler.ActionMode.Cast, GetNode<TextureButton>("CntHUD/PnlAction/PnlActions/HBoxContainer/BtnCastSpell")},
            {Battler.ActionMode.Move, GetNode<TextureButton>("CntHUD/PnlAction/PnlActions/HBoxContainer/BtnMove")},
        };

        foreach (Battler.ActionMode actionMode in actionBtnsDict.Keys)
        {
            actionBtnsDict[actionMode].Pressed += () => btnActions.OnActionBtnPressed(actionMode);
        }
    }


    private void OnCloseSpellBookPressed()
    {
        _audioCloseBook.Play();
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
        // GD.Print(stateMode.ToString());
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
                _pnlMenu.Close();
                _settingsManager.Hide();
                _audioOpenBook.Play();
                EmitSignal(BattleHUD.SignalName.UIPause, true);
                break;
            case StateMode.LogClosed:
                _audioCloseBook.Play();
                _pnlFullLog.Close();
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                break;
            case StateMode.SpellBookClosed:
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                _pnlSpellBook.Close();
                break;
            case StateMode.SpellBookOpened:
                _pnlSpellBook.Open();
                _pnlFullLog.Close();
                _pnlMenu.Close();
                _settingsManager.Hide();
                _audioOpenBook.Play();
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
            case StateMode.MenuClosed:
                _pnlMenu.Close();
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                break;
            case StateMode.MenuOpened:
                EmitSignal(BattleHUD.SignalName.UIPause, true);
                _pnlSpellBook.Close();
                _pnlFullLog.Close();
                _settingsManager.Hide();
                _pnlMenu.Open();
                break;
            case StateMode.SettingsClosed:
                _settingsManager.Hide();
                EmitSignal(BattleHUD.SignalName.UIPause, false);
                break;
            case StateMode.SettingsOpened:
                _settingsManager.Show();
                _pnlSpellBook.Close();
                _pnlFullLog.Close();
                _pnlMenu.Close();
                EmitSignal(BattleHUD.SignalName.UIPause, true);
                break;

        }
    }

    private StoryCharacterData _mouseOverPortraitData = null;

    private void OnTexActiveCharacterMouseEntered()
    {
        if (_mouseOverPortraitData != null)
        {
            _hBoxTurnOrder.OnMouseEntered(_mouseOverPortraitData);
        }

    }

    // if (!cUnit.IsConnected(CharacterUnit.SignalName.BattleTurnEnded, _battleTurnEndedCallable))
    public void OnCharacterStartTurn(StoryCharacterData characterData)
    {
        // if (_texActiveCharacter.IsConnected(TextureRect.SignalName.MouseEntered, _hBoxOnEnteredCallable))
        // {
        //     _texActiveCharacter.Disconnect(TextureRect.SignalName.MouseEntered, _hBoxOnEnteredCallable);
        // }
        // _texActiveCharacter.Connect(TextureRect.SignalName.MouseEntered, _hBoxOnEnteredCallable);
        // _texActiveCharacter.MouseEntered += () => _hBoxTurnOrder.OnMouseEntered(characterData);
        // PortraitMouseEntered = null;
        // PortraitMouseEntered += _hBoxTurnOrder.OnMouseEntered;
        _mouseOverPortraitData = characterData;
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

    internal void SetSpellBookDisplayedSpells(Array<SpellEffectManager.SpellMode> spells, int reagents, int mana)
    {
        _cntSpellBook.ShowSpells(spells, reagents, mana);
    }

    internal void OnCharacterRoundEffectApplied(CharacterUnit characterUnit, CharacterRoundEffect roundEffect)
    {
        // to rework when i add explicit effect IDs - after the spell system rework
        if (roundEffect.Name.StartsWith("Leadership") || roundEffect.Name.StartsWith("Scales") || roundEffect.Name.StartsWith("Disharmony"))
        {
            return;
        }
        OnBattleLogEntry(characterUnit.CharacterData.Name + " affected by: " + roundEffect.Name, true);
    }

    internal void OnCharacterRoundEffectFaded(CharacterUnit characterUnit, CharacterRoundEffect roundEffect)
    {
        // to rework when i add explicit effect IDs - after the spell system rework
        if (roundEffect.Name.StartsWith("Leadership") || roundEffect.Name.StartsWith("Scales") || roundEffect.Name.StartsWith("Disharmony"))
        {
            return;
        }
        OnBattleLogEntry(characterUnit.CharacterData.Name + " no longer affected by: " + roundEffect.Name, true);
    }
    public void OnPnlActionUIHint(PnlAction.UIHint hint)
    {
        OnBattleLogEntry(BattleLogParser.ParseUIHint(hint), false);
    }

    public void OnSpellBookUIHint(SpellEffectManager.Spell spell, bool canAfford)
    {
        OnBattleLogEntry(BattleLogParser.ParseSpellHint(spell, canAfford), false);
    }

    public enum HintMode { ActionHex, DefaultAction, MoveHex, Spellbook, SpellTarget, EndTurn, RightClick }

    public System.Collections.Generic.Dictionary<HintMode, string> _tutorialHintStrings = new()
    {
        {HintMode.ActionHex,        "Hint: clicking on a purple hex will allow you to still act after moving."},
        {HintMode.DefaultAction, "Hint: click on the flashing action button to change the default action."},
        {HintMode.MoveHex,"Hint: moving onto a blue hex prevents you from attacking or casting spells."},
        {HintMode.Spellbook, "Hint: if you know any spells, you can click on the spellbook icon to cast a spell."},
        {HintMode.SpellTarget,"Hint: when you have a spell selected, click on an enemy if it is a hostile spell, or an ally otherwise."},
        {HintMode.EndTurn,"Hint: to end your turn early, click on the tick button"},
        {HintMode.RightClick, "Hint: right click on other heroes to view their strengths and weaknesses"},
    };

    public System.Collections.Generic.Dictionary<HintMode, string> _tutorialHintAnims = new()
    {
        {HintMode.ActionHex,        "Start"},
        {HintMode.DefaultAction, "Action"},
        {HintMode.MoveHex,"Start"},
        {HintMode.Spellbook, "Spellbook"},
        {HintMode.SpellTarget,"Start"},
        {HintMode.EndTurn,"EndTurn"},
        {HintMode.RightClick, "Start"},
    };

    private List<HintMode> _hints = new() { HintMode.ActionHex, HintMode.DefaultAction, HintMode.MoveHex, HintMode.Spellbook, HintMode.SpellTarget, HintMode.EndTurn, HintMode.RightClick };

    [Export]
    private TextureButton _btnActions;
    [Export]
    private TextureButton _btnSpellbook;
    [Export]
    private TextureButton _btnEndTurn;

    public void TutorialHint()
    {
        if (_hints.Count > 0)
        {
            _animTutorial.Stop();
            _pnlShortLog.Modulate = new Color(1, 1, 1);
            _btnActions.Modulate = new Color(1, 1, 1);
            _btnSpellbook.Modulate = new Color(1, 1, 1);
            _btnEndTurn.Modulate = new Color(1, 1, 1);
            OnBattleLogEntry(_tutorialHintStrings[_hints[0]], true);
            _animTutorial.Play(_tutorialHintAnims[_hints[0]]);
            _hints.RemoveAt(0);
        }
    }

    internal void OnCharacterTakingDamage(BattleRoller.RollerOutcomeInformation result, string defender, Vector2 globalPosition, bool died)
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
        OnBattleLogEntry(string.Format("{3}{0}\n{1} takes {2} damage!{4}", hit, defender, result.FinalDamage, roll == "" ? "" : "Roll " + roll + ": ", died ? $" {defender} falls!" : ""), true);
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

    internal void OnSpellBookCostsUIHint(SignalValueHolder values, int display)
    {
        if (((SignalValueHolder.DisplayMode)display) == SignalValueHolder.DisplayMode.None)
        {

            OnBattleLogEntry("", false);
        }
        else
        {
            SignalValueHolder.DisplayMode dis = (SignalValueHolder.DisplayMode)display;
            string output = dis == SignalValueHolder.DisplayMode.Mana ? values.Mana.ToString() + " mana remaining. Used for the magicks of Shamash."
                : values.Reagent.ToString() + " reagents remaining. Used for Ishtari rituals.";
            OnBattleLogEntry(output, false);
        }
    }
    public void Exit()
    {
        // PortraitMouseEntered = null;
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