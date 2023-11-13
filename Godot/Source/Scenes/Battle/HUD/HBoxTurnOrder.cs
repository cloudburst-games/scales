

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

    [Export]
    private const float TOTAL_WIDTH_CAPACITY = 3390;
    [Export]
    private const float PORTRAIT_WIDTH = 140;
    [Export]
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
    }
    private void CreateRoundSeparator()
    {
        Control roundSeparator = _roundSeparatorScn.Instantiate<Control>();
        // roundSeparator.SizeFlagsStretchRatio = 0.1f;
        AddChild(roundSeparator);
        _currentElements.Add(roundSeparator);
    }
}
