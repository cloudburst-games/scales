using Godot;
using System;

[Tool]
public partial class BaseTextureButton : TextureButton
{
    [Export]
    private string _text;
    [Export]
    private AudioStream _hoverSample;// = GD.Load<AudioStreamWav>("res://addons/BaseTextureButtonPlugin/hover.wav");
    [Export]
    private AudioStream _clickedSample;// = GD.Load<AudioStreamWav>("res://addons/BaseTextureButtonPlugin/click.wav");

    private AudioStreamPlayer2D _player;

    [Export(PropertyHint.Range, "-80, 24")]
    private float _volumeDb = 0;

    private Label _lbl;

    public override void _Ready()
    {
        if (_hoverSample != null || _clickedSample != null)
        {
            _player = new()
            {
                VolumeDb = _volumeDb,
                Bus = "Effects"
            };
            AddChild(_player);
            _player.GlobalPosition = this.GlobalPosition;
        }
        if (_hoverSample != null)
        {
            MouseEntered += this.OnMouseEntered;
        }
        if (_clickedSample != null)
        {
            Pressed += this.OnPressed;
        }
        if (_text != "")
        {
            GenerateLabel();
        }
    }

    private void GenerateLabel()
    {
        _lbl = new()
        {
            Text = _text,
            Size = this.Size,
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorsPreset = (int)LayoutPreset.Center,
            LayoutMode = 1

        };
        // _lbl.AutowrapMode = TextServer.AutowrapMode.Word;
        // _lbl.Size = this.Size;
        _lbl.VisibilityChanged += this.OnVisibilityChanged;
        AddChild(_lbl);
        // await ToSignal(GetTree(), "process_frame");
        // _lbl.AutowrapMode = TextServer.AutowrapMode.Word;
        // _lbl.Size = this.Size;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            if (_lbl == null && _text != "")
            {
                GenerateLabel();
            }
            else if (_lbl != null)
            {
                _lbl.Text = _text;
            }
        }
    }

    public async void OnVisibilityChanged() // label size gets bugged when not visible on adding child.. so when visible
                                            // wait a frame then set the size again
    {
        if (!Visible)
        {
            return;
        }
        await ToSignal(GetTree(), "process_frame");
        if (_lbl != null)
        {
            _lbl.Size = this.Size;
        }
    }

    private void OnMouseEntered()
    {
        _player.Stream = _hoverSample;
        _player.Play();
    }

    private void OnPressed()
    {
        _player.Stream = _clickedSample;
        _player.Play();
    }
}
