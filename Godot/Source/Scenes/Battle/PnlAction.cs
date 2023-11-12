using Godot;
using System;
using System.Collections.Generic;

public partial class PnlAction : Panel
{
    [Signal]
    public delegate void ActionUIHintEventHandler(int actionUIHint);

    [Export]
    private TextureButton _btnChooseAction;
    [Export]
    private TextureButton _btnChooseSpell;
    [Export]
    private TextureButton _btnEndTurn;
    [Export]
    private TextureButton _btnMenu;
    [Export]
    private TextureButton _btnMelee;
    [Export]
    private TextureButton _btnShoot;
    [Export]
    private TextureButton _btnMove;
    [Export]
    private TextureButton _btnCast;
    [Export]
    private TextureButton _btnLog;
    [Export]
    private TextureButton _btnOptions;

    public enum UIHint { ChooseAction, Melee, Shoot, Cast, Move, None, EndTurn, Options, ChooseSpell, ExpandLog, Menu }

    private Dictionary<TextureButton, UIHint> _buttonToHint = new();

    public override void _Ready()
    {
        InitializeButtonMappings();
        ConnectMouseEvents();
    }

    private void InitializeButtonMappings()
    {
        _buttonToHint[_btnChooseAction] = UIHint.ChooseAction;
        _buttonToHint[_btnChooseSpell] = UIHint.ChooseSpell;
        _buttonToHint[_btnEndTurn] = UIHint.EndTurn;
        _buttonToHint[_btnMenu] = UIHint.Menu;
        _buttonToHint[_btnMelee] = UIHint.Melee;
        _buttonToHint[_btnShoot] = UIHint.Shoot;
        _buttonToHint[_btnMove] = UIHint.Move;
        _buttonToHint[_btnCast] = UIHint.Cast;
        _buttonToHint[_btnLog] = UIHint.ExpandLog;
        _buttonToHint[_btnOptions] = UIHint.Options;
    }

    private void ConnectMouseEvents()
    {
        foreach (var button in _buttonToHint.Keys)
        {
            button.MouseEntered += () => OnBtnMouseEntered(button);
            button.MouseExited += () => OnBtnMouseExited();
        }
    }

    private void OnBtnMouseExited()
    {
        EmitSignal(SignalName.ActionUIHint, (int)UIHint.None);
    }

    private void OnBtnMouseEntered(TextureButton btn)
    {
        EmitSignal(SignalName.ActionUIHint, (int)_buttonToHint[btn]);
    }


}
