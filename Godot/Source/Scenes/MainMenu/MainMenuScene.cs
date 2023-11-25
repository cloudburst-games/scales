using Godot;
using System;

public partial class MainMenuScene : Node, ISceneTransitionable
{


	[Export]
	private PictureStoryContainer _introPictureStory;

	[Export]
	private BaseTextureButton _btnAdventure;

	[Export]
	private SceneTransition _battleSceneTransition;

	[Export]
	private CntPnlAdventures _cntPnlAdventures;
	[Export]
	private float _musicQuietVol = -15f;
	[Export]
	private BaseTextureButton _btnAbout;
	private BattleDataContainer _battleData = new();
	[Export]
	private BasePanel _pnlAbout;

	public override void _Ready()
	{
		GetNode<SettingsManager>("Control/SettingsManager").Hide();
		// GetNode<BasePanel>("Control/SettingsManager/Panel").CloseOnLoseFocus = true;
		_pnlAbout.GetNode<BaseTextureButton>("BtnClose").Pressed += () => _pnlAbout.Close();
		// _pnlCharacterSelect.Close();
		// GetNode<BasePanel>("Control/PnlAbout").Visible = false;
		// GetNode<PictureStoryContainer>("Control/IntroPictureStory").Finished += this.OnIntroPictureStoryFinished;
		// _pnlCharacterDetails.ConfirmedStory += (int selectedChar) => this.OnConfirmedStory(selectedChar);
		// GetNode<BaseTextureButton>("Control/BtnsContainer/BtnSound").ButtonPressed = AudioServer.IsBusMute(AudioServer.GetBusIndex("Voice"));
		// GetNode<BaseTextureButton>("Control/BtnsContainer/BtnMusic").ButtonPressed = AudioServer.IsBusMute(AudioServer.GetBusIndex("Music"));
		// GetNode<BasePanel>("Control/PnlDifficulty").Close();
		// There are a few different ways to handle music. This approach pauses between main menu and world music.
		// To make it restart as traditional, disable global audio and make the nodes Always
		// To make the same music loop through the whole game, keep all the audio in one container and make it global
		GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").Pause("World");
		GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").Resume("Menu");
		_cntPnlAdventures.Visible = false;
		_cntPnlAdventures.NewPressed += (int adventure, int difficulty, int perk) => this.OnNewAdventure((CntPnlAdventures.AdventureSelectedMode)adventure, (CntPnlAdventures.DifficultyMode)difficulty, (Perk.PerkMode)perk);
		// ConnectDifficultyBtns();
		_cntPnlAdventures.ContinuePressed += (int adventure, int difficulty) => this.OnContinueAdventure((CntPnlAdventures.AdventureSelectedMode)adventure, (CntPnlAdventures.DifficultyMode)difficulty);
		_btnAdventure.Pressed += () => _cntPnlAdventures.Visible = true;
		_btnAbout.Pressed += () => _pnlAbout.Open();
		// _pnlCharacters.CharacterClicked += (int num) => _pnlCharacterDetails.OnCharacterSelected((StoryCharacter.StoryCharacterMode)num);

	}

	private void OnContinueAdventure(CntPnlAdventures.AdventureSelectedMode adventure, CntPnlAdventures.DifficultyMode difficulty)
	{
		string savePath = $"/Checkpoint/GilgAdventure"; // for future adventures would need to vary this
		JSONDataHandler dataHandler = new();
		CheckpointData data = dataHandler.LoadFromJSON<CheckpointData>(savePath);
		_battleData.CheckpointData = data;
		_battleData.Difficulty = (int)difficulty;
		_battleData.PerkSelected = (int)Perk.PerkMode.None;
		_battleData.AdventureSelected = (int)adventure;
		GetNode<SettingsManager>("Control/SettingsManager").Exit();
		_battleSceneTransition.SharedData = _battleData;
		_battleSceneTransition.Start(SceneTransition.LoadType.AnimatedAuto);

		// _battleData.STOREJSON DATA SOMEHOW FOR TRANSFER!
	}

	private void OnNewAdventure(CntPnlAdventures.AdventureSelectedMode adventure, CntPnlAdventures.DifficultyMode difficulty, Perk.PerkMode perkSelected)
	{
		// In future, connect the right adventure with the right picture story. for now just go directly to gilga
		_battleData.Difficulty = (int)difficulty;
		_battleData.AdventureSelected = (int)adventure;
		_battleData.PerkSelected = (int)perkSelected;
		_introPictureStory.Finished += () =>
		{
			GetNode<SettingsManager>("Control/SettingsManager").Exit();
			_battleSceneTransition.SharedData = _battleData;
			_battleSceneTransition.Start(SceneTransition.LoadType.AnimatedAuto);
		};
		_introPictureStory.Play();

		GetTree().Root.GetNode<GlobalAudio>("GlobalAudio").AdjustVolume("Menu", _musicQuietVol);
	}

	public override void _Process(double delta)
	{
	}

	// private void OnConfirmedStory(int selectedChar)
	// {
	// 	_battleData.CharacterSelected = selectedChar;
	// 	GetNode<PictureStoryContainer>("Control/IntroPictureStory").Play();
	// }

	public void OnReceivedSharedData(ISceneTransitionShareableData sharedData)
	{

	}

	// private void OnIntroPictureStoryFinished()
	// {
	//     GetNode<SettingsManager>("Control/SettingsManager").Exit();
	//     _battleSceneTransition.SharedData = _battleData;
	//     _battleSceneTransition.Start(SceneTransition.LoadType.AnimatedAuto);
	// }

	// private void OnBtnPlayPressed()
	// {
	//     GetNode<BasePanel>("Control/PnlDifficulty").Open();
	// }


	private void OnBtnSettingsPressed()
	{
		GetNode<SettingsManager>("Control/SettingsManager").Show();
	}


	// private void OnBtnAboutPressed()
	// {
	//     GetNode<BasePanel>("Control/PnlAbout").Visible = true;
	// }

	private void OnBtnLeaderboardPressed()
	{
		// BattleRoller.Tests();
		// BattleRoller.RollerOutcomeInformation test = BattleRoller.CalculateAttack(new Random(), new BattleRoller.RollerInput());
		// todo 
	}


	// private void OnBtnSoundPressed()
	// {
	//     GetNode<SettingsAudio>("Control/SettingsManager/Panel/SettingsAudio").SetSoundMute(
	//         !AudioServer.IsBusMute(AudioServer.GetBusIndex("Voice")));
	// }

	// private void OnBtnMusicPressed()
	// {
	//     GetNode<SettingsAudio>("Control/SettingsManager/Panel/SettingsAudio").SetMusicMute(
	//         !AudioServer.IsBusMute(AudioServer.GetBusIndex("Music")));
	// }

	private void OnBtnQuitPressed()
	{
		GetTree().Quit();
	}

}

