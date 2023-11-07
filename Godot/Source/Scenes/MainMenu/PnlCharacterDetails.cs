using Godot;
using System;

public partial class PnlCharacterDetails : Panel
{
    [Signal]
    public delegate void ConfirmedStoryEventHandler(int characterSelected);

    [Export]
    private Label _characterName;
    [Export]
    private Label _patron;
    [Export]
    private Label _description;
    [Export]
    private Label _physicalDamage;
    [Export]
    private GridContainer _perksGrid;
    [Export]
    private BaseButton _btnConfirmStory;
    private StoryCharacter.StoryCharacterMode _characterSelected = StoryCharacter.StoryCharacterMode.Enkidu;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _btnConfirmStory.Pressed += this.OnBtnConfirmPressed;
        OnCharacterSelected(StoryCharacter.StoryCharacterMode.Enkidu);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void OnCharacterSelected(StoryCharacter.StoryCharacterMode storyCharacter)
    {
        _characterSelected = storyCharacter;
        StoryCharacterData characterData = StoryCharacterJSONInterface.GetStoryCharacterJSONData(storyCharacter);
        _characterName.Text = characterData.Name;
        _description.Text = characterData.Description;
        _patron.Text = characterData.PatronGod;
        _physicalDamage.Text = characterData.PhysicalDamageStrength.ToString();

        // _perksGrid.ShowPerks(characterData.Perks); // TODO
    }

    private void OnBtnConfirmPressed()
    {
        EmitSignal(SignalName.ConfirmedStory, (int)_characterSelected);
    }
}
