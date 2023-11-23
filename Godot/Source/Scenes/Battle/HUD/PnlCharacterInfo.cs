using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PnlCharacterInfo : Control
{
    public override void _Ready()
    {
        PlaceableArea = GetViewportRect().Size;
        _btnClose.Pressed += this.CommonHide;
        _btnAttributesStats.Pressed += OnBtnAttStatsClicked;
        CommonHide();
    }

    private DisplayMode _currentDisplayMode = DisplayMode.Attributes;
    private StoryCharacterData _currentData;

    [Export]
    private Label _lblCharacterName;
    [Export]
    private Label _lblAttributesStats;
    [Export]
    private PackedScene _pnlCharacterInfoElementScene;
    [Export]
    private VBoxContainer _vBoxStatsDisplay;
    [Export]
    private Label _lblPerks;
    [Export]
    private Label _lblStatuses;
    [Export]
    private BaseTextureButton _btnClose;
    [Export]
    private BaseTextureButton _btnAttributesStats;

    [Signal]
    public delegate void HintClickCharacterEndedEventHandler();
    [Signal]
    public delegate void MouseOverAttributeEnteredEventHandler(string description);
    [Signal]
    public delegate void MouseOverAttributeExitedEventHandler();
    [Export]
    private Panel _pnlClose;
    [Export]
    private TextureRect _portraitRect;

    private Dictionary<string, Texture2D> _portraits = new(); // in future, replace string key with integer IDs (would need to implement in all jsons)

    public Vector2 PlaceableArea { get; set; } = new();


    private enum DisplayMode { Attributes, Stats }

    private Dictionary<StoryCharacterData.AttributeMode, string> _attributeNames = new() {
        {StoryCharacterData.AttributeMode.Might, "Might"}, {StoryCharacterData.AttributeMode.Precision, "Precision"},
        {StoryCharacterData.AttributeMode.Resilience, "Resilience"}, {StoryCharacterData.AttributeMode.Speed, "Speed"},
        {StoryCharacterData.AttributeMode.Intellect, "Intellect"}, {StoryCharacterData.AttributeMode.Charisma, "Charisma"},
        {StoryCharacterData.AttributeMode.Luck, "Luck"}
    };
    private Dictionary<StoryCharacterData.StatMode, string> _statNames = new Dictionary<StoryCharacterData.StatMode, string>
    {
        {StoryCharacterData.StatMode.Health, "Health"},
        // {StoryCharacterData.StatMode.Endurance, "Endurance"},
        {StoryCharacterData.StatMode.HealthRegen, "Health Regen"},
        {StoryCharacterData.StatMode.PhysicalDamageStrength, "PhysicalDamageStrength"},
        {StoryCharacterData.StatMode.PhysicalDamageRanged, "PhysicalDamageRanged"},
        {StoryCharacterData.StatMode.HitBonusStrength, "HitBonusStrength"},
        // {StoryCharacterData.StatMode.HitBonusPrecision, "HitBonusPrecision"},
        {StoryCharacterData.StatMode.CriticalThreshold, "Critical Threshold"},
        {StoryCharacterData.StatMode.Mysticism, "Mysticism"},
        // {StoryCharacterData.StatMode.EnduranceRegen, "EnduranceRegen"},
        {StoryCharacterData.StatMode.MysticResist, "Mystical Resist"},
        {StoryCharacterData.StatMode.PhysicalResist, "Physical Resist"},
        {StoryCharacterData.StatMode.Dodge, "Dodge"},
        {StoryCharacterData.StatMode.Initiative, "Initiative"},
        {StoryCharacterData.StatMode.ActionPoints, "ActionPoints"},
        {StoryCharacterData.StatMode.Leadership, "Leadership"},
        {StoryCharacterData.StatMode.Reagents, "Reagents"},
        {StoryCharacterData.StatMode.FocusCharge, "FocusCharge"},
        // {StoryCharacterData.StatMode.Persuasion, "Persuasion"},
        // {StoryCharacterData.StatMode.PersuasionResist, "PersuasionResist"},
        // {StoryCharacterData.StatMode.MoveSpeed, "MoveSpeed"},
        // {StoryCharacterData.StatMode.HitBonusWeapon, "HitBonusWeapon"},
        // {StoryCharacterData.StatMode.MaxEndurance, "MaxEndurance"},
        // {StoryCharacterData.StatMode.MaxHealth, "MaxHealth"},
        // {StoryCharacterData.StatMode.MaxActionPoints, "MaxActionPoints"},
        // {StoryCharacterData.StatMode.MaxReagents, "MaxReagents"},
        // {StoryCharacterData.StatMode.MaxFocusCharge, "MaxFocusCharge"}
    };

    // private Dictionary<StoryCharacterData.PerkMode, string> _perkNames = new Dictionary<StoryCharacterData.PerkMode, string>
    // {
    //     {StoryCharacterData.PerkMode.SolarFlare, "Solar Flare"},
    //     {StoryCharacterData.PerkMode.SolarBlast, "Solar Blast"},
    //     {StoryCharacterData.PerkMode.JudgementOfFlame, "Judgement of Flame"},
    //     {StoryCharacterData.PerkMode.BlindingLight, "Blinding Light"},
    //     {StoryCharacterData.PerkMode.VialOfFury, "Vial of fury"},
    //     {StoryCharacterData.PerkMode.ElixirOfVigour, "Elixir of Vigour"},
    //     {StoryCharacterData.PerkMode.ElixirOfSwiftness, "Elixir of Swiftness"},
    //     {StoryCharacterData.PerkMode.RegenerativeOintment, "Regenerative Ointment"},
    //     {StoryCharacterData.PerkMode.WeaponSpikes, "Weapon Spikes"},
    //     {StoryCharacterData.PerkMode.EnchantedWeapon, "Weapon Enchantment"},
    //     {StoryCharacterData.PerkMode.LesserArmor, "Lesser Armor"},
    //     {StoryCharacterData.PerkMode.GreaterArmor, "Greater Armor"}
    // };

    public void CommonHide()
    {
        Visible = false;
        EmitSignal(SignalName.HintClickCharacterEnded);
    }

    private void ClearStatsDisplay()
    {
        foreach (Node n in _vBoxStatsDisplay.GetChildren())
        {
            n.QueueFree();
        }
    }

    public void OnRightClick(StoryCharacterData data)
    {
        _pnlClose.Visible = false;
        OnCommonClick(data);
    }

    public void OnLeftClick(StoryCharacterData data)
    {
        _pnlClose.Visible = true;
        OnCommonClick(data);
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventMouseButton btn)
        {
            if (btn.ButtonIndex == MouseButton.Right)
            {
                if (!btn.Pressed && !_pnlClose.Visible)
                {
                    CommonHide();
                }
            }
        }
    }

    private void OnCommonClick(StoryCharacterData data)
    {
        _currentData = data;
        PopulateStatsAttDisplay(DisplayMode.Stats, data);
        _lblCharacterName.Text = data.Name;
        PopulatePerksDisplay(data);
        PopulateStatusDisplay(data);
        SetPortrait(_portraitRect, data);
        SetLocation();
        Visible = true;
    }

    public void StoreAllPortraits(Dictionary<string, Texture2D> allPortraits)
    {
        _portraits.Clear();
        _portraits = allPortraits;
    }

    // WHEN REFACTORING, consider putting this in a separate node, as it is useful outside of the character info panel
    // Then spawn it in the deepest child of the current scene tree so it is accessible multiple times
    public void SetPortrait(TextureRect portraitContainer, StoryCharacterData data)
    {
        Texture2D tex;
        if (!_portraits.ContainsKey(data.Name))
        {
            tex = GD.Load<Texture2D>(data.PortraitPath);
            _portraits[data.Name] = tex;
        }
        else
        {
            tex = _portraits[data.Name];
        }
        portraitContainer.Texture = tex;
    }

    private void SetLocation()
    {
        // aim to centre on the mouse position
        Vector2 loc = GetGlobalMousePosition() - (Size / 2);
        // correct for sides
        if (loc.X < 0)
        {
            loc = new Vector2(0, loc.Y);
        }
        else if (loc.X + Size.X > PlaceableArea.X)
        {
            loc = new Vector2(PlaceableArea.X - Size.X, loc.Y);
        }
        if (loc.Y < 0)
        {
            loc = new Vector2(loc.X, 0);
        }
        else if (loc.Y + Size.Y > PlaceableArea.Y)
        {
            loc = new Vector2(loc.X, PlaceableArea.Y - Size.Y);
        }
        GlobalPosition = loc;
    }

    private void OnBtnAttStatsClicked()
    {
        PopulateStatsAttDisplay(_currentDisplayMode == DisplayMode.Attributes ? DisplayMode.Stats : DisplayMode.Attributes, _currentData);
    }

    private void PopulatePerksDisplay(StoryCharacterData data)
    {
        _lblPerks.Text = string.Join(", ", data.Perks.Select(i => PerkFactory.GeneratePerk(i).Name));
    }

    private void PopulateStatusDisplay(StoryCharacterData data)
    {
        _lblStatuses.Text = "Status effects: " + string.Join(", ", data.CurrentEffects.Select(i => i.Name));
    }

    private void OnPnlElementAttributeMouseEntered(StoryCharacterData.AttributeMode att)
    {
        EmitSignal(SignalName.MouseOverAttributeEntered, _attributeNames[att] + ": " + _attributeDescriptions[att]);
    }

    private void OnPnlElementAttributeMouseExited()
    {
        EmitSignal(SignalName.MouseOverAttributeExited);
    }

    private Dictionary<StoryCharacterData.AttributeMode, string> _attributeDescriptions = new() {
            {StoryCharacterData.AttributeMode.Might, "Boosts health, as well as skill and damage with strength weapons."},
            {StoryCharacterData.AttributeMode.Precision, "Improves skill and damage with precision weapons, overall accuracy, as well as critical chance."},
            {StoryCharacterData.AttributeMode.Resilience, "Enhances health, health regeneration, and both physical and mystical resistance."},
            {StoryCharacterData.AttributeMode.Speed, "Improves chance to dodge, number of action points, and allows to act earlier in combat."},
            {StoryCharacterData.AttributeMode.Intellect, "Elevates mysticism, mystical resist, number of reagents, and mana capacity."},
            {StoryCharacterData.AttributeMode.Charisma, "Augments leadership, boosting abilities of nearby allies."},
            {StoryCharacterData.AttributeMode.Luck, "Improves ability to dodge, resist mystical attacks, and land critical blows."}
    };

    private void PopulateStatsAttDisplay(DisplayMode displayMode, StoryCharacterData data)
    {
        ClearStatsDisplay();
        _currentDisplayMode = displayMode;

        _lblAttributesStats.Text = displayMode == DisplayMode.Attributes ? "Attributes" : "Derived Stats";
        if (displayMode == DisplayMode.Attributes)
        {
            foreach (KeyValuePair<StoryCharacterData.AttributeMode, string> kv in _attributeNames)
            {
                PnlCharacterInfoElement pnlCharacterInfoElement = _pnlCharacterInfoElementScene.Instantiate<PnlCharacterInfoElement>();
                pnlCharacterInfoElement.Set(kv.Value, data.Attributes[kv.Key].ToString());
                pnlCharacterInfoElement.MouseEntered += () => OnPnlElementAttributeMouseEntered(kv.Key);
                pnlCharacterInfoElement.MouseExited += OnPnlElementAttributeMouseExited;
                _vBoxStatsDisplay.AddChild(pnlCharacterInfoElement);
            }
        }
        else
        {
            foreach (KeyValuePair<StoryCharacterData.StatMode, string> kv in _statNames)
            {
                PnlCharacterInfoElement pnlCharacterInfoElement = _pnlCharacterInfoElementScene.Instantiate<PnlCharacterInfoElement>();
                string keyLabel;
                string keyValue;
                if (kv.Key == StoryCharacterData.StatMode.Health)
                {
                    keyLabel = "Health";
                    keyValue = string.Format("{0} / {1}", data.Stats[StoryCharacterData.StatMode.Health], data.Stats[StoryCharacterData.StatMode.MaxHealth]);
                }
                else if (kv.Key == StoryCharacterData.StatMode.PhysicalDamageStrength)
                {
                    keyLabel = "Melee Dmg Bonus";
                    keyValue = data.GetCorrectMeleeWeaponDamageBonus().ToString();
                }
                else if (kv.Key == StoryCharacterData.StatMode.PhysicalDamageRanged)
                {
                    keyLabel = "Ranged Dmg Bonus";
                    keyValue = data.GetCorrectRangedWeaponDamageBonus().ToString();
                }
                else if (kv.Key == StoryCharacterData.StatMode.ActionPoints)
                {
                    keyLabel = "Action Points";
                    keyValue = string.Format("{0} / {1}", data.Stats[StoryCharacterData.StatMode.ActionPoints], data.Stats[StoryCharacterData.StatMode.MaxActionPoints]);
                }
                else if (kv.Key == StoryCharacterData.StatMode.Reagents)
                {
                    keyLabel = "Reagents";
                    keyValue = string.Format("{0} / {1}", data.Stats[StoryCharacterData.StatMode.Reagents], data.Stats[StoryCharacterData.StatMode.MaxReagents]);
                }
                else if (kv.Key == StoryCharacterData.StatMode.FocusCharge)
                {
                    keyLabel = "Mana";
                    keyValue = string.Format("{0} / {1}", data.Stats[StoryCharacterData.StatMode.FocusCharge], data.Stats[StoryCharacterData.StatMode.MaxFocusCharge]);
                }
                else if (kv.Key == StoryCharacterData.StatMode.HitBonusStrength)
                {
                    keyLabel = "Melee Hit Bonus";
                    keyValue = data.GetCorrectHitBonusMelee(false).ToString();
                }
                else
                {
                    keyLabel = kv.Value;
                    keyValue = data.Stats[kv.Key].ToString();
                }
                pnlCharacterInfoElement.Set(keyLabel, keyValue);
                _vBoxStatsDisplay.AddChild(pnlCharacterInfoElement);
            }

            PnlCharacterInfoElement meleeDiceElement = _pnlCharacterInfoElementScene.Instantiate<PnlCharacterInfoElement>();
            string weaponDiceKey = "Melee Weapon Dice";
            string weaponDiceVal = string.Format("{0}d{1}", data.WeaponDiceMelee[0].Item1, data.WeaponDiceMelee[0].Item2);
            meleeDiceElement.Set(weaponDiceKey, weaponDiceVal);
            _vBoxStatsDisplay.AddChild(meleeDiceElement);

            if ((StoryCharacterData.RangedWeaponMode)data.RangedWeaponEquipped != StoryCharacterData.RangedWeaponMode.None)
            {
                PnlCharacterInfoElement rangedHitElement = _pnlCharacterInfoElementScene.Instantiate<PnlCharacterInfoElement>();
                string rangedHitKey = "Ranged Hit Bonus";
                string rangedHitVal = data.GetCorrectHitBonusRanged(false).ToString();
                rangedHitElement.Set(rangedHitKey, rangedHitVal);
                _vBoxStatsDisplay.AddChild(rangedHitElement);
                PnlCharacterInfoElement rangedDiceElement = _pnlCharacterInfoElementScene.Instantiate<PnlCharacterInfoElement>();
                string rangedDiceKey = "Ranged Weapon Dice";
                string rangedDiceVal = string.Format("{0}d{1}", data.WeaponDiceRanged[0].Item1, data.WeaponDiceRanged[0].Item2);
                rangedDiceElement.Set(rangedDiceKey, rangedDiceVal);
                _vBoxStatsDisplay.AddChild(rangedDiceElement);
            }

        }

    }
}
