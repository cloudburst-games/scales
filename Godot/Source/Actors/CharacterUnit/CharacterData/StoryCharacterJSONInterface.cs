// using Godot;

public class StoryCharacterJSONInterface
{

	public static StoryCharacterData GetStoryCharacterJSONData(StoryCharacter.StoryCharacterMode storyCharacter)
	{
		string filename = storyCharacter switch
		{
			StoryCharacter.StoryCharacterMode.Enkidu => "Data/Characters/Story/enkidu.json",
			StoryCharacter.StoryCharacterMode.Peasant => "Data/Characters/Story/peasant.json",
			StoryCharacter.StoryCharacterMode.Villager => "Data/Characters/Story/villager.json",
			StoryCharacter.StoryCharacterMode.Gesht => "Data/Characters/Story/gesht.json",
			StoryCharacter.StoryCharacterMode.Gilgam => "Data/Characters/Story/gilgam.json",
			StoryCharacter.StoryCharacterMode.Priestess => "Data/Characters/Story/priestess.json",
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
