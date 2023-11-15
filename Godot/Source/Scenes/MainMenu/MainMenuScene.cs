using Godot;
using System;

public partial class MainMenuScene : Node, ISceneTransitionable
{

    // [Export]
    // private PnlCharacters _pnlCharacters;
    // [Export]
    // private PnlCharacterDetails _pnlCharacterDetails;
    // [Export]
    // private BasePanel _pnlCharacterSelect;
    [Export]
    private PictureStoryContainer _introPictureStory;

    [Export]
    private BaseTextureButton _btnAdventure;

    [Export]
    private SceneTransition _battleSceneTransition;

    [Export]
    private CntPnlAdventures _cntPnlAdventures;

    private BattleDataContainer _battleData = new();

    public override void _Ready()
    {
        GetNode<SettingsManager>("Control/SettingsManager").Hide();
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
        _cntPnlAdventures.NewPressed += (int adventure, int difficulty) => this.OnNewAdventure((CntPnlAdventures.AdventureSelectedMode)adventure, (CntPnlAdventures.DifficultyMode)difficulty);
        // ConnectDifficultyBtns();
        _cntPnlAdventures.ContinuePressed += (int adventure) => this.OnContinueAdventure((CntPnlAdventures.AdventureSelectedMode)adventure);
        _btnAdventure.Pressed += () => _cntPnlAdventures.Visible = true;

        // _pnlCharacters.CharacterClicked += (int num) => _pnlCharacterDetails.OnCharacterSelected((StoryCharacter.StoryCharacterMode)num);

    }

    private void OnContinueAdventure(CntPnlAdventures.AdventureSelectedMode adventure)
    {
        // TODO when checkpoint stuff implemented
        _battleData.AdventureSelected = (int)adventure;
        // _battleData.STOREJSON DATA SOMEHOW FOR TRANSFER!
    }

    private void OnNewAdventure(CntPnlAdventures.AdventureSelectedMode adventure, CntPnlAdventures.DifficultyMode difficulty)
    {
        // In future, connect the right adventure with the right picture story. for now just go directly to gilga
        _battleData.Difficulty = (int)difficulty;
        _battleData.AdventureSelected = (int)adventure;
        _introPictureStory.Finished += () =>
        {
            GetNode<SettingsManager>("Control/SettingsManager").Exit();
            _battleSceneTransition.SharedData = _battleData;
            _battleSceneTransition.Start(SceneTransition.LoadType.AnimatedAuto);
        };
        _introPictureStory.Play();
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

