using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PnlCharacters : Panel
{

    [Signal]
    public delegate void CharacterClickedEventHandler(int storyCharacter);

    [Export]
    private TextureButton _enkidu;
    [Export]
    private TextureButton _gilgam;
    [Export]
    private TextureButton _lugal;
    [Export]
    private TextureButton _ningal;
    [Export]
    private TextureButton _utug;
    [Export]
    private TextureButton _eresh;
    [Export]
    private TextureButton _dumuzi;
    [Export]
    private TextureButton _gesht;


    public override void _Ready()
    {
        _enkidu.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Enkidu);
        _gilgam.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Gilgam);
        _lugal.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Priestess);
        _ningal.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Ningal);
        _utug.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Utug);
        _eresh.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Peasant);
        _dumuzi.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Villager);
        _gesht.Pressed += () => OnCharacterPressed(StoryCharacter.StoryCharacterMode.Gesht);


        OnCharacterPressed(StoryCharacter.StoryCharacterMode.Enkidu);
    }


    public override void _Process(double delta)
    {
    }

    private void OnCharacterPressed(StoryCharacter.StoryCharacterMode storyCharacter)
    {
        EmitSignal(SignalName.CharacterClicked, (int)storyCharacter);
    }
}