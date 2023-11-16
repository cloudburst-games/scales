// 1. sort out character selection.
// - start with a list of CharacterUnits
// - populate the HBoxContainer with new BaseTextureButtons based on this list (free children first)
// - create a dict as well to connect each button with a characterunit
// - the first character starts selected (so the button is grayed out)
// - when a character is selected, set _activeCharacter to this one

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PnlCharacterSelect : Panel
{

    [Export]
    private ButtonGroup _characterSelectBtnGroup;

    [Export]
    private HBoxContainer _HBoxCharacterSelect;

    [Export]
    private Label _lblCharacterName;

    [Export]
    private TextureRect _texCharacterPortrait;

    private Dictionary<CharacterUnit, BaseTextureButton> _companionButtons = new();

    [Signal]
    public delegate void CharacterSelectedEventHandler(CharacterUnit cUnit, bool toggled);

    public void Start(List<CharacterUnit> companions)
    {
        _HBoxCharacterSelect.GetChildren().ToList().ForEach(x => x.QueueFree());
        companions.ForEach(x =>
        {
            _HBoxCharacterSelect.AddChild(NewCompanionButton(x));
        });
        _companionButtons.First().Value.ButtonPressed = true;
    }

    private BaseTextureButton NewCompanionButton(CharacterUnit characterUnit)
    {
        StoryCharacterData data = characterUnit.CharacterData;
        Texture2D portrait = GD.Load<Texture2D>(data.PortraitPath);
        BaseTextureButton btn = new()
        {
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            TextureNormal = GD.Load<Texture2D>(data.CharacterBtnNormalPath),
            TexturePressed = GD.Load<Texture2D>(data.CharacterBtnPressedPath),
            ButtonGroup = _characterSelectBtnGroup,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ToggleMode = true
        };
        _companionButtons[characterUnit] = btn;
        btn.Toggled += (bool toggled) =>
        {
            EmitSignal(SignalName.CharacterSelected, characterUnit, toggled);
            if (toggled)
            {
                _lblCharacterName.Text = data.Name;
                _texCharacterPortrait.Texture = portrait;
            }
        };
        return btn;

    }
}
