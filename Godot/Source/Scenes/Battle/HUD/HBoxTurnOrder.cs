

// maximum number of elements adds up to a total size of 3391
// size of a portrait texturerect is 140
// variable numbers of portraits separated by panel bars which should be approximately 14 pixels
using Godot;
using System;
using System.Collections.Generic;

public partial class HBoxTurnOrder : HBoxContainer
{
    // UI elements
    private Dictionary<string, Texture2D> _portraitElementsDict = new();

    private List<Control> _currentElements = new();

    [Export]
    private PackedScene _roundSeparatorScn;

    private const float TOTAL_WIDTH_CAPACITY = 2960;
    private const float PORTRAIT_WIDTH = 140;
    private const float SEPARATOR_WIDTH = 14;


    public override void _Ready()
    {
    }

    public void StoreAllPortraits(Dictionary<string, Texture2D> portraits)
    {
        _portraitElementsDict.Clear();
        _portraitElementsDict = portraits;
    }

    public void OnCharacterTurnStart(List<StoryCharacterData> charactersRemaining, List<StoryCharacterData> allCharacters)
    {
        UpdateTurnOrderUI(charactersRemaining, allCharacters);
    }

    private void UpdateTurnOrderUI(List<StoryCharacterData> charactersRemaining, List<StoryCharacterData> allCharacters)
    {
        foreach (Control element in _currentElements)
        {
            element.QueueFree();
        }
        _currentElements.Clear();


        float currentCapacity = 0;
        bool firstRound = true;

        while (currentCapacity < TOTAL_WIDTH_CAPACITY)
        {

            foreach (StoryCharacterData character in firstRound ? charactersRemaining : allCharacters)
            {
                if (currentCapacity + PORTRAIT_WIDTH > TOTAL_WIDTH_CAPACITY)
                {
                    break;
                }
                CreatePortrait(character);
                currentCapacity += PORTRAIT_WIDTH;
            }

            if (currentCapacity + PORTRAIT_WIDTH > TOTAL_WIDTH_CAPACITY)
            {
                break;
            }
            CreateRoundSeparator();
            currentCapacity += SEPARATOR_WIDTH;
            firstRound = false;
        }
    }

    private void CreatePortrait(StoryCharacterData character)
    {
        TextureRect portrait = new()
        {
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            Texture = _portraitElementsDict[character.Name]
        };
        AddChild(portrait);
        _currentElements.Add(portrait);
        portrait.MouseEntered += () => this.OnMouseEntered(character);
        portrait.MouseExited += this.OnMouseExited;
    }
    private void CreateRoundSeparator()
    {
        Control roundSeparator = _roundSeparatorScn.Instantiate<Control>();
        // roundSeparator.SizeFlagsStretchRatio = 0.1f;
        AddChild(roundSeparator);
        _currentElements.Add(roundSeparator);
    }

    public void OnMouseEntered(StoryCharacterData storyCharacter)
    {
        _currentMouseover = storyCharacter;
    }

    public void OnMouseExited()
    {
        _currentMouseover = null;
    }

    private StoryCharacterData _currentMouseover = null;

    [Signal]
    public delegate void CharacterClickedEventHandler(bool rightClick, StoryCharacterData data);

    public override void _Input(InputEvent ev)
    {
        if (!ev.IsEcho())
        {
            if (ev.IsPressed())
            {

                if (ev is InputEventMouseButton btn && (btn.ButtonIndex == MouseButton.Left || btn.ButtonIndex == MouseButton.Right))
                {
                    if (_currentMouseover != null)
                    {
                        // GD.Print("clicked on ", _currentMouseover.Name);
                        EmitSignal(SignalName.CharacterClicked, btn.ButtonIndex == MouseButton.Right, _currentMouseover);
                    }
                }

            }
        }
    }

    // todo - something simlar for single portrait (accessed in BattleHUD)
    // converge code and reduce duplication
    // connect signals to pnlcharacterinfo access
    // ? mouse cursor


}
