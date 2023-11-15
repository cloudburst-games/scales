// using Godot;

public class StoryCharacterJSONInterface
{

    public static StoryCharacterData GetStoryCharacterJSONData(StoryCharacter.StoryCharacterMode storyCharacter)
    {
        string filename = storyCharacter switch
        {
            StoryCharacter.StoryCharacterMode.Enkidu => "Data/Characters/Story/enkidu.json",
            StoryCharacter.StoryCharacterMode.Eresh => "Data/Characters/Story/eresj.json",
            StoryCharacter.StoryCharacterMode.Dumuzi => "Data/Characters/Story/dumuzi.json",
            StoryCharacter.StoryCharacterMode.Gesht => "Data/Characters/Story/gesht.json",
            StoryCharacter.StoryCharacterMode.Gilgam => "Data/Characters/Story/gilgam.json",
            StoryCharacter.StoryCharacterMode.Lugal => "Data/Characters/Story/lugal.json",
            StoryCharacter.StoryCharacterMode.Ningal => "Data/Characters/Story/ningal.json",
            StoryCharacter.StoryCharacterMode.Utug => "Data/Characters/Story/utug.json",
            StoryCharacter.StoryCharacterMode.Mountain => "Data/Characters/Story/mountain_weak.json",
            StoryCharacter.StoryCharacterMode.AscendedMountain => "Data/Characters/Story/mountain_strong.json",
            StoryCharacter.StoryCharacterMode.Tornado => "Data/Characters/Story/tornado_weak.json",
            StoryCharacter.StoryCharacterMode.TornadoLord => "Data/Characters/Story/tornado_strong.json",
            _ => "Data/Characters/Story/enkidu.json",
        };
        JSONDataHandler dataHandler = new();
        StoryCharacterData characterData = dataHandler.LoadFromJSON<StoryCharacterData>(filename, false);
        return characterData;

        // _perksGrid.ShowPerks(characterData.Perks); // TODO
    }

}