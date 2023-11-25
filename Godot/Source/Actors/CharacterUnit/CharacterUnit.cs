// CharacterUnit script. Coordinates all the various associated scripts. CLEANING UP CURRENTLY

// TODO for AI: //
// AI VERSION - set up different modes - patrol mode, stationary mode, waypoint mode
// Should be a STACK FSM to logically switch behaviours
// Can switch between moving and barking - BarkState - pauses x number of seconds and says something -
// - if at certain radius from CharacterUnit type Y, activate the bark state, which consists of pause X seconds,
// CharacterUnit trigger type Y, and list of strings (each would be separated by 1 second or something hardcoded)
// Each bark should have a cooldown (not repeated), e.g. 1 minute
// - Export this data in a list
// - would need to store the previous movement mode to return to it
// - export movement behaviours (mode, position vectors, gamestate trigger (TBD - global variable) (or ?level variable -int LevelState), time trigger (if between X and Y time)


using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CharacterUnit : CharacterBody2D
{

    public StoryCharacterData CharacterData { get; set; }
    public SpellEffectManager.SpellMode UISelectedSpell = SpellEffectManager.SpellMode.None;
    public Battler.ActionMode UISelectedAction = Battler.ActionMode.Melee;

    public Vector2 StartingBattleAnimDirection { get; set; } = new();

    // WIP
    // public SpellEffectData SelectedSpell { get; set; } = new SpellEffectData { Name = "Test Spell", Target = SpellEffectData.TargetMode.Enemy };

    // This should be deprecated by CharacterData
    // [Export]
    // public CharacterStats CharacterStats { get; set; }

    // To determine target position when right clicking in adventure map mode
    [Export]
    public int FormationPosition { get; set; } = 1;
    [Export]
    public StatusToPlayerMode StatusToPlayer { get; set; } = StatusToPlayerMode.Neutral;
    [Export]
    private LblBark _lblBark;
    [Export]
    public ProgressBar BarHealth { get; set; }
    private PackedScene _body;// = GD.Load<PackedScene>("res://Source/Actors/CharacterUnit/Bodies/PlayerBody.tscn");

    // For adventure map selection
    [Signal]
    public delegate void CharacterClickedEventHandler(CharacterUnit characterUnit, bool shift);
    // Used by Battle map mode. To determine whether to set an obstacle on the hex or not. Can feasibly send
    // the global position rather than the whole characterunit!
    [Signal]
    public delegate void RemoveObstacleEventHandler(CharacterUnit characterUnit, bool isMoving);
    // Used by the Battler to determine what to do next, e.g. new round, next character turn, end the battle
    [Signal]
    public delegate void BattleTurnEndedEventHandler(CharacterUnit characterUnit);

    [Signal]
    public delegate void CastingEffectEventHandler(SpellEffectManager.Spell spell);// Vector2 origin, Vector2 destination, CharacterUnit targetCharacter, SpellEffectManager.BattleSpellMode spellEffect);

    [Signal]
    public delegate void MovedButStillHaveAPEventHandler();
    [Signal]
    public delegate void DiedEventHandler(CharacterUnit characterUnit);

    [Signal]
    public delegate void TakingDamageEventHandler(BattleRoller.RollerOutcomeInformation result, string defender, Vector2 globalPosition, bool died);

    public Random Rand { get; set; } = new();

    public enum StatusToPlayerMode { Player, Allied, Neutral, Hostile }

    // Adventure map selection
    public bool Selected { get; set; } = false;

    // public bool TakeDamageQueued { get; set; } = false;
    // I dont think this is used - delete if no errors.
    // private List<Vector2> _currentGridPath = new();
    // public List<Vector2> CurrentPath {
    //     get {
    //         return _currentGridPath;
    //     }
    //     set {
    //         _currentGridPath = value;
    //     }
    // }

    // For when a path is set in battle states
    public List<Vector2> BattlePath { get; set; } = new();
    public Vector2 BattleTargetPosition;
    public AnimationTree AnimationTree { get; set; }
    public AnimationPlayer Anim { get; set; }
    public NavigationAgent2D NavAgent { get; set; }

    // Not yet used, but can be useful for activating nearby items, dialogue, etc.
    private Area2D _proximityArea;

    public enum ControlMode { Player, AI }
    private CharacterUnitControlState _controlState;
    public enum ActionMode
    {
        Idle, Moving, Dying, EnteringBattle, ExitingBattle, IdleBattle, MovingBattle, WaitingBattle, MeleeBattle, RangedBattle, CastingBattle,
        TakingDamageBattle,
        DyingBattle
    }
    private CharacterUnitActionState _actionState;
    // This is used for error correction - when a character is stuck in movement state.
    public Vector2 PreviousVelocity = new Vector2();

    public TreeLink CharacterDataTreeLink = new();


    public partial class TreeLink : Node
    {
        [Signal]
        public delegate void RoundEffectAppliedEventHandler(CharacterRoundEffect roundEffect);
        [Signal]
        public delegate void RoundEffectEndedEventHandler(CharacterRoundEffect roundEffect);
    }

    public void SetControlState(ControlMode controlMode)
    {
        switch (controlMode)
        {
            case ControlMode.Player:
                _controlState = new PlayerCharacterUnitControlState(this);
                break;
            case ControlMode.AI:
                _controlState = new AICharacterUnitControlState(this);
                break;
        }
    }

    public ControlMode GetControlState()
    {
        return _controlState is PlayerCharacterUnitControlState ? ControlMode.Player : ControlMode.AI;
    }
    public void SetActionState(ActionMode actionMode)
    {
        if (_actionState != null)
        {
            _actionState.Exit();
        }
        switch (actionMode)
        {
            case ActionMode.Idle:
                _actionState = new IdleCharacterUnitActionState(this);
                break;
            case ActionMode.Moving:
                _actionState = new MovingCharacterUnitActionState(this);
                break;
            case ActionMode.Dying:
                _actionState = new DyingCharacterUnitActionState(this);
                break;
            case ActionMode.EnteringBattle:
                _actionState = new EnteringBattleCharacterUnitActionState(this);
                break;
            case ActionMode.ExitingBattle:
                _actionState = new ExitingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.IdleBattle:
                _actionState = new IdleBattleCharacterUnitActionState(this);
                break;
            case ActionMode.MovingBattle:
                _actionState = new MovingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.MeleeBattle:
                _actionState = new MeleeBattleCharacterUnitActionState(this);
                break;
            case ActionMode.WaitingBattle:
                _actionState = new WaitingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.RangedBattle:
                _actionState = new RangedBattleCharacterUnitActionState(this);
                break;
            case ActionMode.TakingDamageBattle:
                _actionState = new TakingDamageBattleCharacterUnitActionState(this);
                break;
            case ActionMode.DyingBattle:
                _actionState = new DyingBattleCharacterUnitActionState(this);
                break;
            case ActionMode.CastingBattle:
                _actionState = new CastingBattleCharacterUnitActionState(this);
                break;
        }
        // GD.Print(CharacterData.Name + " CHANGED STATE TO " + actionMode);
    }

    // Can make this more readable, e.g. with a switch...
    public ActionMode GetActionState()
    {
        return _actionState is IdleCharacterUnitActionState ? ActionMode.Idle :
            _actionState is MovingCharacterUnitActionState ? ActionMode.Moving :
            _actionState is DyingCharacterUnitActionState ? ActionMode.Dying :
            _actionState is EnteringBattleCharacterUnitActionState ? ActionMode.EnteringBattle :
            _actionState is ExitingBattleCharacterUnitActionState ? ActionMode.ExitingBattle :
            _actionState is IdleBattleCharacterUnitActionState ? ActionMode.IdleBattle :
            _actionState is WaitingBattleCharacterUnitActionState ? ActionMode.WaitingBattle :
            _actionState is RangedBattleCharacterUnitActionState ? ActionMode.RangedBattle :
            _actionState is TakingDamageBattleCharacterUnitActionState ? ActionMode.TakingDamageBattle :
            _actionState is DyingBattleCharacterUnitActionState ? ActionMode.DyingBattle :
            _actionState is MeleeBattleCharacterUnitActionState ? ActionMode.MeleeBattle :
            _actionState is CastingBattleCharacterUnitActionState ? ActionMode.CastingBattle : ActionMode.MovingBattle;
    }

    public override void _Ready()
    {
        Init();
        InitStatusToPlayer();
        // important to set this to the current position, to avoid errors
        NavAgent.TargetPosition = GlobalPosition;
    }

    private void Init()
    {
        // GD.Print("is this run twice? " + CharacterData.Name);
        AddChild(CharacterDataTreeLink);
        CharacterBody bod = _body.Instantiate<CharacterBody>();
        bod.SetBody(this);
        bod.Owner = null;
        bod.QueueFree();
        SetAudioStreamPlayer2DFromPath(GetNode<AudioContainer>("AudioWalk"), CharacterData.AudioWalkPath, "Effects");
        SetAudioStreamPlayer2DFromPath(GetNode<AudioContainer>("AudioHurt"), CharacterData.AudioHurtPath, "Effects");
        SetAudioStreamPlayer2DFromPath(GetNode<AudioContainer>("AudioDie"), CharacterData.AudioDiePath, "Effects");
        SetAudioStreamPlayer2DFromPath(GetNode<AudioContainer>("AudioMelee"), CharacterData.AudioMeleePath, "Effects");
        NavAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        AnimationTree = GetNode<AnimationTree>("AnimTree");
        Anim = GetNode<AnimationPlayer>("Anim");
        _proximityArea = GetNode<Area2D>("ProximityArea");
        GetNode<Sprite2D>("Sprite").Material = (Godot.Material)GetNode<Sprite2D>("Sprite").Material.Duplicate(true);
    }

    private void SetAudioStreamPlayer2DFromPath(AudioContainer n, List<string> paths, StringName bus)
    {
        foreach (string path in paths)
        {
            var player = new AudioStreamPlayer2D()
            {
                Stream = GD.Load<AudioStreamWav>(path),
                Bus = bus,
            };
            n.SoundParent = this;

            n.AddChild(player);
        }
    }

    public void InitStatusToPlayer()
    {
        // OriginalStatusToPlayer = StatusToPlayer;
        switch (StatusToPlayer)
        {
            case StatusToPlayerMode.Player:
                SetControlState(ControlMode.Player);
                ValidEnemyTargets = new() { StatusToPlayerMode.Hostile, StatusToPlayerMode.Neutral };
                ValidAllyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Neutral, StatusToPlayerMode.Allied };
                BarHealth.AddThemeStyleboxOverride("background", GD.Load<StyleBoxFlat>("res://Source/Actors/CharacterUnit/HealthBars/BarPlayerBG.tres"));
                BarHealth.AddThemeStyleboxOverride("fill", GD.Load<StyleBoxFlat>("res://Source/Actors/CharacterUnit/HealthBars/BarPlayerFill.tres"));
                break;
            case StatusToPlayerMode.Allied:
                SetControlState(ControlMode.AI);
                ValidEnemyTargets = new() { StatusToPlayerMode.Hostile };
                ValidAllyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Neutral, StatusToPlayerMode.Allied };
                BarHealth.AddThemeStyleboxOverride("background", GD.Load<StyleBoxFlat>("res://Source/Actors/CharacterUnit/HealthBars/BarAllyBG.tres"));
                BarHealth.AddThemeStyleboxOverride("fill", GD.Load<StyleBoxFlat>("res://Source/Actors/CharacterUnit/HealthBars/BarAllyFill.tres"));
                break;
            case StatusToPlayerMode.Neutral:
                SetControlState(ControlMode.AI);
                ValidEnemyTargets = new() { };
                ValidAllyTargets = new() { };
                break;
            default:
                SetControlState(ControlMode.AI);
                ValidEnemyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Allied };
                ValidAllyTargets = new() { StatusToPlayerMode.Hostile };
                BarHealth.AddThemeStyleboxOverride("background", GD.Load<StyleBoxFlat>("res://Source/Actors/CharacterUnit/HealthBars/BarEnemyBG.tres"));
                BarHealth.AddThemeStyleboxOverride("fill", GD.Load<StyleBoxFlat>("res://Source/Actors/CharacterUnit/HealthBars/BarEnemyFill.tres"));
                break;
        }
    }

    // public StatusToPlayerMode OriginalStatusToPlayer { get; private set; }
    // public StatusToPlayerMode BerserkStatusToPlayer { get; private set; }

    public Vector2 GetProjectileAttackOrigin()
    {
        // GD.Print("glob pos star", GlobalPosition);
        Vector2 result = GlobalPosition;

        Vector2 blendPos = (Vector2)AnimationTree.Get("parameters/Idle/blend_position");
        var sprite = GetNode<Sprite2D>("Sprite");
        // Vector2 texSize = sprite.RegionRect.Size;

        result += blendPos * 10;
        result += sprite.Offset;
        // result += new Vector2(result.X < 0 ? texSize.X * -0.1f : texSize.X * 0.1f, result.Y < 0 ? texSize.Y * -0.1f : texSize.Y * 0.1f);

        return result;

    }

    public void DoBerserkStatusToPlayer()
    {
        SetControlState(ControlMode.AI);
        BerserkValidAllyTargets = new() { };
        BerserkValidEnemyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Allied, StatusToPlayerMode.Hostile, StatusToPlayerMode.Neutral };
        // switch (StatusToPlayer)
        // {
        //     case StatusToPlayerMode.Player:
        //         // BerserkStatusToPlayer = StatusToPlayerMode.Hostile;
        //         BerserkValidEnemyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Allied, StatusToPlayerMode.Hostile };
        //         // ValidAllyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Neutral, StatusToPlayerMode.Allied };
        //         break;
        //     case StatusToPlayerMode.Allied:
        //         // BerserkStatusToPlayer = StatusToPlayerMode.Hostile;
        //         BerserkValidEnemyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Allied, StatusToPlayerMode.Hostile };
        //         // ValidAllyTargets = new() { };
        //         break;
        //     case StatusToPlayerMode.Neutral:
        //         // BerserkStatusToPlayer = StatusToPlayerMode.Hostile;
        //         BerserkValidEnemyTargets = new() { StatusToPlayerMode.Player, StatusToPlayerMode.Allied, StatusToPlayerMode.Hostile, StatusToPlayerMode.Neutral };
        //         // ValidAllyTargets = new() { };
        //         break;
        //     case StatusToPlayerMode.Hostile:
        //         // BerserkStatusToPlayer = StatusToPlayerMode.Allied;
        //         BerserkValidEnemyTargets = new() { StatusToPlayerMode.Hostile, StatusToPlayerMode.Player, StatusToPlayerMode.Allied, };
        //         // ValidAllyTargets = new() { StatusToPlayerMode.Hostile };
        //         break;
        // }
    }

    public void RestoreStatusToPlayer()
    {
        InitStatusToPlayer();
    }

    public List<StatusToPlayerMode> ValidEnemyTargets = new();
    public List<StatusToPlayerMode> ValidAllyTargets = new();
    public List<StatusToPlayerMode> BerserkValidEnemyTargets = new();
    public List<StatusToPlayerMode> BerserkValidAllyTargets = new();

    private void InitCharacterStats()
    {
        // CharacterStats.ResourceLocalToScene = true;
    }

    public void UpdateBarHealth()
    {
        float fract = (float)CharacterData.Stats[StoryCharacterData.StatMode.Health] / (float)CharacterData.Stats[StoryCharacterData.StatMode.MaxHealth] * 100;
        BarHealth.Value = fract;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_controlState == null || _actionState == null)
        {
            return;
        }
        base._PhysicsProcess(delta);
        SetDebugText(GetActionState().ToString());
        if (NavAgent.Velocity != new Vector2(0, 0))
        {
            PreviousVelocity = NavAgent.Velocity;
        }

        _controlState.Update(delta);
        _actionState.Update(delta);
    }

    private void OnNavAgentVelocityComputed(Vector2 safeVelocity)
    {
        MoveAndCollide(safeVelocity);

    }

    // For GUI input such as character selection with the mouse
    public void OnClickAreaGUIInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton btn)
        {
            if (btn.ButtonIndex == MouseButton.Left && btn.Pressed)
            {
                EmitSignal(SignalName.CharacterClicked, this, Input.IsPhysicalKeyPressed(Key.Shift));
            }
        }
    }

    // Check if a given area is near the character unit
    public bool IsAreaNearCharacterUnit(Area2D area)
    {
        foreach (Area2D a in _proximityArea.GetOverlappingAreas())
        {
            if (a.GetParent() == this)
            {
                continue;
            }
            if (a == area)
            {
                return true;
            }
        }
        return false;
    }

    public CharacterUnit MeleeTarget { get; set; } = null;
    // public CharacterUnit RangedTarget { get; set; } = null;

    // public Vector2 SpellDestination { get; set; }
    // public SpellEffectManager.BattleSpellMode SpellEffect { get; set; }
    // public BattleSpellData SpellBeingCastOld { get; set; }

    public void BattleMoveOrder(int moveCost, List<Vector2> worldPath, CharacterUnit targetCharacter = null)
    {
        worldPath.RemoveAt(0);
        CharacterData.Stats[StoryCharacterData.StatMode.ActionPoints] -= moveCost;
        BattlePath = worldPath;
        MeleeTarget = targetCharacter;
    }

    public void BattleMeleeOrder(CharacterUnit targetCharacter)
    {
        MeleeTarget = targetCharacter;
        _actionState.BattleMeleeOrder();
    }

    public SpellEffectManager.Spell SpellBeingCast { get; private set; }
    // internal void BattleShootOrder(CharacterUnit targetCharacter) // DEPRECATED
    // {
    //     RangedTarget = targetCharacter;
    //     SpellDestination = targetCharacter.GlobalPosition;
    //     SpellEffect = SpellEffectManager.BattleSpellMode.Arrow;
    //     _actionState.BattleShootOrderOld(targetCharacter);
    // }

    // consider unifying the shoot and cast methods as they utilise same underpinnings
    internal void BattleShootOrder(SpellEffectManager.Spell spell)
    {
        SpellBeingCast = spell;
        _actionState.BattleShootOrder();
    }

    internal void BattleCastOrder(SpellEffectManager.Spell spell)
    {
        SpellBeingCast = spell;
        _actionState.BattleCastOrder();
    }

    public void OnSpellEffectFinished()
    {
        _actionState.OnSpellEffectFinished();
    }
    public void BattleSkipOrder()
    {
        _actionState.BattleSkipOrder(); // will do something if IdleBattle state
    }

    public bool TurnPending { get; set; } = false;
    public int CurrentLevel { get; set; }

    // summary generated by chat gippity!!!!
    /// <summary>
    /// Attempts to play a random bark associated with the current level of the character.
    /// Barks are stored in a nested list structure where each level has a list of bark sets.
    /// The method checks for the existence of bark data and ensures that there are available
    /// barks for the current level before playing a random one. After playing a bark, it removes
    /// the entire set of barks for the current level, allowing progression to the next set of barks.
    /// </summary>
    public void TryBark()
    {
        if (CharacterData.Barks == null)
        {
            return;
        }

        if (CharacterData.Barks.TryGetValue(CurrentLevel, out List<List<string>> barksForLevel) && barksForLevel != null && barksForLevel.Count > 0)
        {
            if (barksForLevel[0] != null && barksForLevel[0].Count > 0)
            {
                string randomBark = barksForLevel[0][Rand.Next(0, barksForLevel[0].Count)];
                _lblBark.Bark(randomBark);
                CharacterData.Barks[CurrentLevel].RemoveAt(0);
            }
        }
    }


    public void CharacterStartBattleTurn()
    {
        // TryBark();
        // wait for any outstanding HIT animations
        // todo - a proper fix for this
        // GD.Print(CharacterData.Name + " startbattleturn method " +
        // AnimationTree.Get("parameters/conditions/takingdamage"));
        // GD.Print(Anim.CurrentAnimation);
        // if (Anim.IsPlaying() && (bool)AnimationTree.Get("parameters/conditions/takingdamage") == true)
        // {

        //     await ToSignal(AnimationTree, AnimationTree.SignalName.AnimationFinished);
        //     //     await ToSignal(Anim, AnimationPlayer.SignalName.AnimationFinished);
        // }
        // if resuming turn (i.e. NOT ended), then don't reset points etc

        _actionState.BattleIdleOrder();
    }

    // public bool 

    // When the battle ends, the units (in waiting state) will be directed to transition to ExitingBattle (= > Idle)
    public void OnBattleEnd()
    {
        _actionState.BattleEndOrder();
    }

    public void SetDebugText(string text)
    {
        GetNode<Label>("LblDebug").Text = text;
    }

    public void SetFromJSON(StoryCharacter.StoryCharacterMode selectedChar) // before added to tree
    {
        CharacterData = StoryCharacterJSONInterface.GetStoryCharacterJSONData(selectedChar);
        CharacterData.Initialise(RoundEffectAnim, CharacterDataTreeLink);
        _body = GD.Load<PackedScene>(CharacterData.BodyPath);
    }

    [Export]
    public AnimationPlayer RoundEffectAnim { get; set; }

    public BattleRoller.RollerOutcomeInformation TakingDamageResult { get; set; }

    public void TakeDamageOrder(BattleRoller.RollerOutcomeInformation res)
    {
        TakingDamageResult = res;
        _actionState.TakeDamageOrder();
    }

    private Tuple<Color, bool> _lastOutline;

    public void RestoreLastOutline()
    {
        if (_lastOutline == null)
        {
            return;
        }
        SetSpriteOutlineColour(_lastOutline.Item1, _lastOutline.Item2);
    }

    public void SetSpriteOutlineColour(Color color, bool enabled = true)
    {
        if (GetNode<Sprite2D>("Sprite").Material is ShaderMaterial shaderMaterial)
        {
            // GD.Print(CharacterData.Name);
            // GD.Print(StatusToPlayer);
            // GD.Print(color.R + ", " + color.G + ", " + color.B);
            shaderMaterial.SetShaderParameter("width", enabled ? 3f : 0f);
            shaderMaterial.SetShaderParameter("outline_color_origin", color);
            _lastOutline = new Tuple<Color, bool>(color, enabled);
        }
    }

    public void ShowSpriteOutline(bool enabled = true)
    {
        if (GetNode<Sprite2D>("Sprite").Material is ShaderMaterial shaderMaterial)
        {
            shaderMaterial.SetShaderParameter("width", enabled ? 3f : 0f);
        }
    }
    internal void OnBattleFavour(Scales.FavourMode favour, bool extreme)
    {
        int magnitude = 3;
        if (extreme)
        {
            magnitude *= 2;
        }
        CharacterRoundEffect scalesEffect;
        CharacterRoundEffect disharmonyEffect = null;

        switch (favour)
        {
            case Scales.FavourMode.Balanced:
                scalesEffect = CreateScalesEffect("Scales: Balanced", StoryCharacterData.AttributeMode.Charisma, magnitude);
                break;

            case Scales.FavourMode.Ishtar:
                scalesEffect = CreateScalesEffect("Scales: Might of Ishtar", StoryCharacterData.AttributeMode.Might, magnitude);
                disharmonyEffect = CreateScalesEffect("Disharmony: Shamash", StoryCharacterData.AttributeMode.Intellect, -magnitude);
                break;

            case Scales.FavourMode.Shamash:
                scalesEffect = CreateScalesEffect("Scales: Wisdom of Shamash", StoryCharacterData.AttributeMode.Intellect, magnitude);
                disharmonyEffect = CreateScalesEffect("Disharmony: Ishtar", StoryCharacterData.AttributeMode.Might, -magnitude);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(favour), favour, null);
        }

        CharacterData.DoEffectInitial(scalesEffect);
        if (disharmonyEffect != null)
        {
            CharacterData.DoEffectInitial(disharmonyEffect);
        }
    }

    private CharacterRoundEffect CreateScalesEffect(string name, StoryCharacterData.AttributeMode attribute, int magnitude)
    {
        return new CharacterRoundEffect(
            name: name,
            attributeAffected: attribute,
            statAffected: StoryCharacterData.StatMode.Endurance,
            effectType: CharacterRoundEffect.EffectTypeMode.Attribute,
            rounds: 3,
            permanent: false,
            cumulative: false,
            magnitude: magnitude,
            animName: "todo!",
            fromSpell: SpellEffectManager.SpellMode.None,
            from: magnitude >= 0 ? "Scales" : "Disharmony"
        );
    }

    internal void ApplyPerk(Perk p)
    {
        switch (p.Category)
        {
            case Perk.PerkCategory.ArmourBonus:
                CharacterData.ArmourClass += p.Magnitude;
                CharacterData.Perks.Remove(p.CurrentPerk);
                break;
            case Perk.PerkCategory.DamageBonus:
                CharacterData.MeleeWeaponDamageBonus += p.Magnitude;
                CharacterData.RangedWeaponDamageBonus += p.Magnitude;
                CharacterData.Perks.Remove(p.CurrentPerk);
                break;
            case Perk.PerkCategory.AttributeBonus:
                CharacterData.Attributes[p.AssociatedAttribute] += p.Magnitude;
                CharacterData.Perks.Remove(p.CurrentPerk);
                break;
            case Perk.PerkCategory.MeleeWeapon:
                CharacterData.Perks.Where(x => PerkFactory.GeneratePerk(x).Category == Perk.PerkCategory.MeleeWeapon && x != p.CurrentPerk).ToList().ForEach(mP => CharacterData.Perks.Remove(mP));
                CharacterData.MeleeWeaponEquipped = p.AssociatedMeleeWeapon;
                break;
            case Perk.PerkCategory.RangedWeapon:
                CharacterData.Perks.Where(x => PerkFactory.GeneratePerk(x).Category == Perk.PerkCategory.RangedWeapon && x != p.CurrentPerk).ToList().ForEach(mP => CharacterData.Perks.Remove(mP));
                CharacterData.RangedWeaponEquipped = p.AssociatedRangedWeapon;
                break;
            case Perk.PerkCategory.Spell:
                if (!CharacterData.KnownSpells.Contains(p.AssociatedSpell))
                {
                    CharacterData.KnownSpells.Add(p.AssociatedSpell);
                }
                break;
        }
    }

    internal void SetFromData(CharacterCheckpointData data)
    {
        CharacterData = new()
        {
            Name = data.Name,
            Description = data.Description,
            PatronGod = data.PatronGod,
            BodyPath = data.BodyPath,
            PortraitPath = data.PortraitPath,
            AudioWalkPath = data.AudioWalkPath,
            AudioHurtPath = data.AudioHurtPath,
            AudioDiePath = data.AudioDiePath,
            AudioMeleePath = data.AudioMeleePath,
            Barks = data.Barks,
            CharacterBtnNormalPath = data.CharacterBtnNormalPath,
            CharacterBtnPressedPath = data.CharacterBtnPressedPath,
            MeleeWeaponEquipped = (StoryCharacterData.MeleeWeaponMode)data._meleeWeaponEquipped,
            RangedWeaponEquipped = (StoryCharacterData.RangedWeaponMode)data._rangedWeaponEquipped,
            Perks = data.Perks.Select(x => (Perk.PerkMode)x).ToList(),
            Level = data.Level,
            Might = data.Might,
            Resilience = data.Resilience,
            Precision = data.Precision,
            Speed = data.Speed,
            Intellect = data.Intellect,
            Charisma = data.Charisma,
            Luck = data.Luck,
            ArmourClass = data.ArmourClass,

            Attributes = new() { { StoryCharacterData.AttributeMode.Might, data.Might },
            { StoryCharacterData.AttributeMode.Resilience, data.Resilience },
            { StoryCharacterData.AttributeMode.Precision, data.Precision },
            { StoryCharacterData.AttributeMode.Speed, data.Speed },
            { StoryCharacterData.AttributeMode.Intellect, data.Intellect },
            { StoryCharacterData.AttributeMode.Charisma, data.Charisma },
            { StoryCharacterData.AttributeMode.Luck, data.Luck }, }

        };
        CharacterData.Initialise(RoundEffectAnim, CharacterDataTreeLink);
        _body = GD.Load<PackedScene>(CharacterData.BodyPath);
    }
}
