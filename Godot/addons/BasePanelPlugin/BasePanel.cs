using Godot;
using System;

[Tool]
public partial class BasePanel : Panel
{
    private Vector2 _dragPoint = new Vector2(-9999, -9999);

    [Export]
    private bool _snapToEdges = true;
    [Export]
    private bool _draggable = true;
    [Export]
    private bool _closeOnLoseFocus = false;
    [Export]
    private bool _bringToFrontOnClick = true;
    [Export]
    private bool _takeFocus = true;
    [Export(PropertyHint.None, "Only if _takeFocus is enabled.")]
    private Color _tintBGColor = new(1, 1, 1, 0.1f);

    [Export]
    private PackedScene _closeBtnScn;

    [Export]
    private bool _startClosed = false;

    private ColorRect _focusRect;
    public TextureButton CloseBtn { get; set; }

    private Callable _generateFocusRectCallable;

    [Signal]
    public delegate void FocusRectGeneratedEventHandler();

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        _generateFocusRectCallable = new Callable(this, nameof(GenerateFocusRect));
        GuiInput += this.OnGuiInput;

        SnapToEdges();

        if (_closeBtnScn != null)
        {
            GenerateCloseBtn();
        }
        if (_takeFocus)
        {
            GetParent().Connect("ready", _generateFocusRectCallable);
        }
        InitStartClosed();
    }

    private async void InitStartClosed()
    {
        if (_startClosed)
        {
            if (_takeFocus)
            {
                await ToSignal(this, SignalName.FocusRectGenerated);
            }
            Close();
        }
    }

    private void GenerateFocusRect()
    {
        _focusRect = new()
        {
            Color = _tintBGColor,
            Size = GetViewportRect().Size,
        };
        GetParent().CallDeferred(Node.MethodName.AddChild, _focusRect);
        // GetParent().AddChild(_focusRect);
        _focusRect.GlobalPosition = new Vector2(0, 0);
        // GetParent().MoveChild(_focusRect, GetIndex());        
        GetParent().CallDeferred(Node.MethodName.MoveChild, _focusRect, GetIndex());

        if (GetParent().IsConnected("ready", _generateFocusRectCallable))
        {
            GetParent().Disconnect("ready", _generateFocusRectCallable);
        }
        EmitSignal(SignalName.FocusRectGenerated);
    }

    private void GenerateCloseBtn()
    {
        CloseBtn = (TextureButton)_closeBtnScn.Instantiate();
        // _closeBtn.AnchorsPreset = (int) LayoutPreset.TopRight;
        // _closeBtn.LayoutMode = 1;
        CloseBtn.Position = new Vector2(Size.X - CloseBtn.Size.X, 0);
        AddChild(CloseBtn);
        CloseBtn.Pressed += this.Close;
    }

    private void OnGuiInput(InputEvent ev)
    {
        if (_bringToFrontOnClick)
        {
            if (LeftClicked(ev))
            {
                BringToFront();
            }
        }
        if (_draggable)
        {
            DoDrag(ev);
        }
    }

    private bool LeftClicked(InputEvent ev)
    {
        if (ev is InputEventMouseButton mouseBtn)
        {
            if (mouseBtn.ButtonIndex == MouseButton.Left)
            {
                if (mouseBtn.Pressed)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void DoDrag(InputEvent ev)
    {
        if (ev is InputEventMouseButton mouseBtn)
        {
            if (mouseBtn.ButtonIndex == MouseButton.Left)
            {
                if (mouseBtn.Pressed)
                {
                    _dragPoint = GetGlobalMousePosition() - Position;
                    MouseDefaultCursorShape = CursorShape.Drag; // this is bugged and for some reason messes up the global mouse position
                    if (_bringToFrontOnClick)
                    {
                        BringToFront();
                    }
                }
                else
                {
                    _dragPoint = new Vector2(-9999, -9999);
                    MouseDefaultCursorShape = CursorShape.Arrow; // this is bugged and for some reason messes up the global mouse position
                }
            }

        }

        if (ev is InputEventMouseMotion && _dragPoint != new Vector2(-9999, -9999))
        {
            Position = GetGlobalMousePosition() - _dragPoint;

            SnapToEdges();
        }
    }

    private void SnapToEdges()
    {
        if (!_snapToEdges)
        {
            return;
        }
        if (Position.X < 0 || Position.X + Size.X > GetViewportRect().Size.X)
        {
            Position = new Vector2(Position.X < 0 ? 0 : GetViewportRect().Size.X - Size.X, Position.Y);
        }
        if (Position.Y < 0 || Position.Y + Size.Y > GetViewportRect().Size.Y)
        {
            Position = new Vector2(Position.X, Position.Y < 0 ? 0 : GetViewportRect().Size.Y - Size.Y);
        }
    }

    public void Close()
    {
        Visible = false;
        FreeFocusRect();
    }

    private void FreeFocusRect()
    {
        if (_focusRect != null)
        {
            _focusRect.QueueFree();
            _focusRect = null;
        }
    }

    public void Open()
    {
        Visible = true;
        BringToFront();
    }

    public new void QueueFree()
    {
        FreeFocusRect();
        base.QueueFree();
    }

    public void BringToFront()
    {
        // if (GetParent().GetChildCount() - 1 == GetIndex())
        // {
        //     return;
        // }
        int siblingCount = GetParent().GetChildCount();

        GetParent().CallDeferred(Node.MethodName.MoveChild, this, siblingCount - 1);
        if (_takeFocus)
        {
            FreeFocusRect();
            GenerateFocusRect();
        }
    }

    public void SendToBack()
    {
        GetParent().MoveChild(this, 0);
        if (_takeFocus)
        {
            FreeFocusRect();
            GenerateFocusRect();
        }
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            if (CloseBtn == null && _closeBtnScn != null)
            {
                GenerateCloseBtn();
            }
            else if (_closeBtnScn == null)
            {
                if (CloseBtn != null)
                {
                    CloseBtn.QueueFree();
                    CloseBtn = null;
                }
            }
        }
        else
        {
            if (_closeOnLoseFocus)
            {
                if (Input.IsMouseButtonPressed(MouseButton.Left))
                {
                    if (!GetGlobalRect().HasPoint(GetGlobalMousePosition()))
                    {

                        Close();
                    }
                }
            }
        }
    }
}
