using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BattleVictory : Control
{


    [Export]
    private BaseTextureButton _btnMercy;
    [Export]
    private BaseTextureButton _btnDiplomacy;
    [Export]
    private BaseTextureButton _btnWrath;
    [Export]
    private BasePanel _pnlDiplomacy;
    [Export]
    private BasePanel _pnlFate;
    [Export]
    private BaseTextureButton _btnDiplomacyContinue;
    [Export]
    private AnimationPlayer _animDiplomacy;
    [Export]
    private AnimationPlayer _animScalesStart;
    [Export]
    private AnimationPlayer _animScales;
    [Export]
    private Label _lblDiplomacyOutcome;
    private BattleRoller.PersuadeOutcomeInformation _persuadeOutcome;
    [Signal]
    public delegate void FavouredGodEventHandler(int which);
    [Export]
    private TextureRect _opponentTex;
    [Export]
    private Label _lblDecideFate;

    private int _scalesImpact = 2;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _btnMercy.Pressed += () => OnFateDecided(Scales.FavourMode.Shamash);
        _btnDiplomacy.Pressed += () => StartDiplomacy();//OnFateDecided(Scales.FavourMode.Balanced);
        _btnWrath.Pressed += () => OnFateDecided(Scales.FavourMode.Ishtar);
        _btnDiplomacyContinue.Pressed += () => _pnlDiplomacy.Close();// StartDiplomacy();
        Visible = false;

        Test();
    }

    public void Test()
    {
        List<CharacterUnit> playertestInput = new List<CharacterUnit>
        {
            new CharacterUnit
            {
                CharacterData = new StoryCharacterData
                {
                    Name = "Testy",
                    Stats = new Dictionary<StoryCharacterData.StatMode, int>
                    {
                        { StoryCharacterData.StatMode.Persuasion, 5 }
                    }
                },
                Rand = new Random()
            }
        };

        List<CharacterUnit> enemyTesty = new List<CharacterUnit>
        {
            new CharacterUnit
            {
                CharacterData = new StoryCharacterData
                {
                    Name = "enemyiei",
                    PortraitPath = "res://Assets/Graphics/Sprites/Actors/Enkidu/portrait_placeholder.png",
                    Stats = new Dictionary<StoryCharacterData.StatMode, int>
                    {
                        { StoryCharacterData.StatMode.PersuasionResist, 2 }
                    }
                }
            },
            new CharacterUnit
            {
                CharacterData = new StoryCharacterData
                {
                    Name = "Testy",
                    PortraitPath = "res://Assets/Graphics/Sprites/Actors/Enkidu/portrait_placeholder.png",
                    Stats = new Dictionary<StoryCharacterData.StatMode, int>
                    {
                        { StoryCharacterData.StatMode.PersuasionResist, 4 }
                    }
                }
            },
            new CharacterUnit
            {
                CharacterData = new StoryCharacterData
                {
                    Name = "Enakidu",
                    PortraitPath = "res://Assets/Graphics/Sprites/Actors/Enkidu/portrait_placeholder.png",
                    Stats = new Dictionary<StoryCharacterData.StatMode, int>
                    {
                        { StoryCharacterData.StatMode.PersuasionResist, 5 }
                    }
                }
            }
        };

        Start(playertestInput, enemyTesty, 0.3f);
    }

    private void StartDiplomacy()
    {
        _pnlDiplomacy.Open();
        _animDiplomacy.Play("Start");
        if (_persuadeOutcome.PersuadeSuccess)
        {
            _btnDiplomacyContinue.Pressed += () => OnFateDecided(Scales.FavourMode.Balanced);
        }
        else
        {

            _btnDiplomacy.Visible = false;
        }
    }

    private void ConnectButtonsToScaleTime(float scaleAnimTime)
    {
        _animScales.CurrentAnimation = "Start";
        _btnWrath.MouseEntered += () =>
            _animScales.Seek(Math.Max(0, _animScales.CurrentAnimationPosition - _scalesImpact / 10f), true);
        _btnWrath.MouseExited += () =>
            _animScales.Seek(scaleAnimTime, true);
        _btnMercy.MouseExited += () =>
            _animScales.Seek(scaleAnimTime, true);
        _btnMercy.MouseEntered += () =>
            _animScales.Seek(Math.Min(1, _animScales.CurrentAnimationPosition + _scalesImpact / 10f), true);
        // _animScales.Seek(scaleAnimTime, true);
    }

    internal void Start(List<CharacterUnit> playerControlled, List<CharacterUnit> enemies, float scaleAnimTime) //(int protagnoistPersuade, int defenderPersuade, Random rand)
    {
        Visible = true;
        ConnectButtonsToScaleTime(scaleAnimTime);
        // GetNode<TextureRect>("PnlFate/VBoxContainer/PnlScales/TextureRect").SelfModulate = new Color(1, 1, 1, 1);
        // GetNode<TextureRect>("PnlFate/VBoxContainer/PnlScales/TextureRect").Visible = true;
        CharacterUnit persuader = playerControlled.OrderByDescending(x => x.CharacterData.Stats[StoryCharacterData.StatMode.Persuasion]).FirstOrDefault();
        CharacterUnit victim = enemies.Find(x => x.CharacterData.Name == "Enkidu" || x.CharacterData.Name == "marzipan")
                        ?? enemies[persuader.Rand.Next(0, enemies.Count)];
        _opponentTex.Texture = GD.Load<Texture2D>(victim.CharacterData.PortraitPath);
        _persuadeOutcome = BattleRoller.RollPersuade(persuader.Rand, persuader.CharacterData.Stats[StoryCharacterData.StatMode.Persuasion], victim.CharacterData.Stats[StoryCharacterData.StatMode.PersuasionResist]);
        string persuaderString = string.Format("{0}, the most charismatic, steps forward to liaise with {1}.", persuader.CharacterData.Name, victim.CharacterData.Name);
        string rollString = string.Format("{4} rolls {0} + {1} vs your captive's {2} + {3}.",
            _persuadeOutcome.AttackerRoll, _persuadeOutcome.AttackerPersuade, _persuadeOutcome.DefenderRoll, _persuadeOutcome.DefenderPersuadeResist, persuader.CharacterData.Name);
        string consequenceString = string.Format(_persuadeOutcome.PersuadeSuccess ?
        "Your charisma has swayed " + victim.CharacterData.Name + ". You have a new companion. By defying the Gods, you have lost a chance at their favour." :
        "Your opponent rejects your attempts at persuasion. You must decide on mercy or execution. The Gods are watching.");
        _lblDiplomacyOutcome.Text = persuaderString + "\n\n" + rollString + "\n\n" + consequenceString;

        if (victim.CharacterData.Name == "Enkidu" || victim.CharacterData.Name == "marzipan")
        {
            _lblDiplomacyOutcome.Text = victim.CharacterData.Name + " recognises your might, and joins you on your quest!";
            _lblDecideFate.Text = "Decide the fate of your remaining opponents!";
            _animDiplomacy.SpeedScale *= 2;
            _pnlDiplomacy.Open();
            _animDiplomacy.Play("Start");
            _btnDiplomacy.Visible = false;
            _btnDiplomacyContinue.Pressed += () =>
            {
                OpenPnlFate(scaleAnimTime);
            };
        }
        else
        {
            OpenPnlFate(scaleAnimTime);
        }
    }

    private async void OpenPnlFate(float scaleAnimTime)
    {
        // I don't know why this is needed, as it isnt even playing an animation...
        if (_animScales.IsPlaying())
        {
            await ToSignal(_animScales, AnimationPlayer.SignalName.AnimationFinished);
        }
        _pnlFate.Open();
        _animScalesStart.Play("Start");
        _animScales.Seek(scaleAnimTime, true);
    }

    private void OnFateDecided(Scales.FavourMode fate)
    {
        _pnlFate.Close();
        EmitSignal(SignalName.FavouredGod, (int)fate, _scalesImpact); // neutral - no impact. otherwise, impact as per scalesimpact

        // NEXT we need to sort out attributing perks -:
        // animate a perks panel which will say YOU ARE FAVOURED BY (whichever God you favoured) then show 3 perks. you can drag them to a character slot and the character wil get the perk.
        // so if u have 3 charactes u can divvy out all 3 perks
        // until we have tex icons for them, probably use label as placeholder
        // they will be draggable, not buttons. may need to figure something out for this. if they are close enough to the perk slot they snap to it and the character name lights up.
        // if all characters have a perk, it will not be one of the available 3 options.
        // then when all 3 are snapped, the continue button is enabled. otherwise it stays disabled.
    }
}
