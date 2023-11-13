using Godot;
using System;
using System.Collections.Generic;

public partial class PnlAction : Panel
{
    [Signal]
    public delegate void ActionUIHintEventHandler(int actionUIHint);

    [Export]
    private Control _btnChooseAction;
    [Export]
    private Control _btnChooseSpell;
    [Export]
    private Control _btnEndTurn;
    [Export]
    private Control _btnMenu;
    [Export]
    private Control _btnMelee;
    [Export]
    private Control _btnShoot;
    [Export]
    private Control _btnMove;
    [Export]
    private Control _btnCast;
    [Export]
    private Control _btnLog;
    [Export]
    private Control _btnOptions;
    [Export]
    private Control _barHealth;
    [Export]
    private Control _barCharge;
    [Export]
    private Control _barHerbs;

    public enum UIHint { ChooseAction, Melee, Shoot, Cast, Move, None, EndTurn, Options, ChooseSpell, ExpandLog, Menu, CurrentHealth, CurrentCharge, CurrentHerbs }

    private Dictionary<Control, UIHint> _controlToHint = new();

    public override void _Ready()
    {
        InitializeButtonMappings();
        ConnectMouseEvents();
    }

    private void InitializeButtonMappings()
    {
        _controlToHint[_btnChooseAction] = UIHint.ChooseAction;
        _controlToHint[_btnChooseSpell] = UIHint.ChooseSpell;
        _controlToHint[_btnEndTurn] = UIHint.EndTurn;
        _controlToHint[_btnMenu] = UIHint.Menu;
        _controlToHint[_btnMelee] = UIHint.Melee;
        _controlToHint[_btnShoot] = UIHint.Shoot;
        _controlToHint[_btnMove] = UIHint.Move;
        _controlToHint[_btnCast] = UIHint.Cast;
        _controlToHint[_btnLog] = UIHint.ExpandLog;
        _controlToHint[_btnOptions] = UIHint.Options;
        _controlToHint[_barHealth] = UIHint.CurrentHealth;
        _controlToHint[_barCharge] = UIHint.CurrentCharge;
        _controlToHint[_barHerbs] = UIHint.CurrentHerbs;
    }

    private void ConnectMouseEvents()
    {
        foreach (var ctrl in _controlToHint.Keys)
        {
            ctrl.MouseEntered += () => OnBtnMouseEntered(ctrl);
            ctrl.MouseExited += () => OnBtnMouseExited();
        }
    }

    private void OnBtnMouseExited()
    {
        EmitSignal(SignalName.ActionUIHint, (int)UIHint.None);
    }

    private void OnBtnMouseEntered(Control ctrl)
    {
        EmitSignal(SignalName.ActionUIHint, (int)_controlToHint[ctrl]);
    }


}
