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
    public delegate void FavouredGodEventHandler(int which, int scalesImpact, CharacterUnit victim);
    [Export]
    private TextureRect _opponentTex;
    [Export]
    private Label _lblDecideFate;

    [Export]
    private AnimationPlayer _fateDecidedAnim;

    [Export]
    private BaseTextureButton _btnFadeDecidedContinue;
    [Export]
    private Label _lblFateDecided;

    private CharacterUnit _victim = null;

    private int _scalesImpact = 2;
    private bool _signalsConnected = false;

    public delegate void FateToChooseEventHandler(float scaleAnimTime);
    public event FateToChooseEventHandler FateToChoose;

    public delegate void BalancedFateDecidedEventHandler();
    public event BalancedFateDecidedEventHandler BalancedFateDecided;
    public delegate void SkippedDiplomacyEventHandler();
    public event SkippedDiplomacyEventHandler SkippedDiplomacy;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Visible = false;
        _btnDiplomacyContinue.Pressed += () => BalancedFateDecided?.Invoke();
        _btnDiplomacyContinue.Pressed += () =>
        {
            SkippedDiplomacy?.Invoke();
        };
        // Test();
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
            BalancedFateDecided += () => this.OnFateDecided(Scales.FavourMode.Balanced);
        }
        else
        {
            _victim = null;
            _btnDiplomacy.Visible = false;
        }
    }

    public delegate void MovingScalesTowardsShamashEventHandler();
    public event MovingScalesTowardsShamashEventHandler MovingScalesTowardsShamash;
    public delegate void MovingScalesTowardsIshtarEventHandler();
    public event MovingScalesTowardsIshtarEventHandler MovingScalesTowardsIshtar;
    public delegate void ScalesReturningEventHandler();
    public event ScalesReturningEventHandler ScalesReturning;

    // omg godot signals SUCK!!!!!!
    private void ConnectButtonsToScaleTime(float scaleAnimTime)
    {
        _animScales.CurrentAnimation = "Start";
        MovingScalesTowardsIshtar += () => _animScales.Seek(Math.Min(1, _animScales.CurrentAnimationPosition + _scalesImpact / 10f), true);
        MovingScalesTowardsShamash += () => _animScales.Seek(Math.Max(0, _animScales.CurrentAnimationPosition - _scalesImpact / 10f), true);
        ScalesReturning += () => _animScales.Seek(scaleAnimTime, true);
        if (_signalsConnected)
        {
            return;
        }
        _signalsConnected = true;
        _btnWrath.MouseEntered += () => MovingScalesTowardsIshtar?.Invoke();
        // _animScales.Seek(Math.Max(0, _animScales.CurrentAnimationPosition - _scalesImpact / 10f), true);
        _btnWrath.MouseExited += () => ScalesReturning?.Invoke();
        _btnMercy.MouseExited += () => ScalesReturning?.Invoke();//            _animScales.Seek(scaleAnimTime, true);
        _btnMercy.MouseEntered += () => MovingScalesTowardsShamash?.Invoke();
        // _animScales.Seek(Math.Min(1, _animScales.CurrentAnimationPosition + _scalesImpact / 10f), true);
        _btnMercy.Pressed += () =>
        {
            OnFateDecided(Scales.FavourMode.Shamash);
            MovingScalesTowardsShamash?.Invoke();
            // _animScales.Seek(Math.Min(1, _animScales.CurrentAnimationPosition + _scalesImpact / 10f), true);
        };
        _btnDiplomacy.Pressed += () => StartDiplomacy();//OnFateDecided(Scales.FavourMode.Balanced);
        _btnWrath.Pressed += () =>
        {
            OnFateDecided(Scales.FavourMode.Ishtar);
            MovingScalesTowardsIshtar?.Invoke();
            // _animScales.Seek(Math.Max(0, _animScales.CurrentAnimationPosition - _scalesImpact / 10f), true);

        };
        _btnDiplomacyContinue.Pressed += () => _pnlDiplomacy.Close();// StartDiplomacy();
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
        _victim = victim;
        _opponentTex.Texture = GD.Load<Texture2D>(victim.CharacterData.PortraitPath);
        _persuadeOutcome = BattleRoller.RollPersuade(persuader.Rand, persuader.CharacterData.Stats[StoryCharacterData.StatMode.Persuasion], victim.CharacterData.Stats[StoryCharacterData.StatMode.PersuasionResist]);
        string persuaderString = string.Format("{0}, the most charismatic, steps forward to liaise with {1}.", persuader.CharacterData.Name, victim.CharacterData.Name);
        string rollString = string.Format("{4} rolls {0} + {1} vs your captive's {2} + {3}.",
            _persuadeOutcome.AttackerRoll, _persuadeOutcome.AttackerPersuade, _persuadeOutcome.DefenderRoll, _persuadeOutcome.DefenderPersuadeResist, persuader.CharacterData.Name);
        string consequenceString = string.Format(_persuadeOutcome.PersuadeSuccess ?
        "Your charisma has swayed " + victim.CharacterData.Name + ". You have a new companion." :
        "Your opponent rejects your attempts at persuasion. You must decide on mercy or execution. The Gods are watching.");
        _lblDiplomacyOutcome.Text = persuaderString + "\n\n" + rollString + "\n\n" + consequenceString;

        if (victim.CharacterData.Name == "Enkidu" || victim.CharacterData.Name == "marzipan")
        {
            _lblDiplomacyOutcome.Text = "Victory!\n" + victim.CharacterData.Name + " recognises your might, and joins you on your quest!";
            _lblDecideFate.Text = "Decide the fate of your remaining opponents!\nIshtar would be pleased with a show of retribution.\nShamash advises a display of mercy.";
            _animDiplomacy.SpeedScale *= 2;
            _pnlDiplomacy.Open();
            _animDiplomacy.Play("Start");
            _btnDiplomacy.Visible = false;
            SkippedDiplomacy += () => OpenPnlFate(scaleAnimTime);
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

    private async void OnFateDecided(Scales.FavourMode fate)
    {
        string godName = fate == Scales.FavourMode.Ishtar ? "Ishtar" : "Shamash";
        _lblFateDecided.Text = fate != Scales.FavourMode.Balanced ? $"Pleased with your devotion, your Patron {godName} bestows upon you their power. You have more perks, of greater power, to select from." :
            "By defying the Gods, you have lost a chance at their favour.\nYou have fewer and weaker boons to choose from.";

        _fateDecidedAnim.Play("Start");
        await ToSignal(_btnFadeDecidedContinue, BaseTextureButton.SignalName.Pressed);
        _fateDecidedAnim.Play("RESET");
        _animScalesStart.Play("RESET");
        BalancedFateDecided = null;
        SkippedDiplomacy = null;
        MovingScalesTowardsIshtar = null;
        MovingScalesTowardsShamash = null;
        ScalesReturning = null;
        _btnDiplomacy.Visible = true;
        _pnlFate.Close();
        Visible = false;
        EmitSignal(SignalName.FavouredGod, (int)fate, _scalesImpact, _victim); // neutral - no impact. otherwise, impact as per scalesimpact

    }
}
