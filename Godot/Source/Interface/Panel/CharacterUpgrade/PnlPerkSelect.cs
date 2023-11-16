using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PnlPerkSelect : Panel
{

    [Export]
    private GridContainer _gridContainer;
    [Export]
    private ButtonGroup _perkSelectButtonGroup;
    [Export]
    private BaseTextureButton _btnContinue;

    [Signal]
    public delegate void UpgradeFinishedEventHandler(Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>> characterPerks);
    [Export]
    private Label _lblPerkSelect;
    private CharacterUnit _activeCharacter;

    private Dictionary<CharacterUnit, Perk[]> _characterPerks = new();
    private Dictionary<Perk, BaseTextureButton> _perkBtns = new();

    private int _numberOfPerksPerCharacter = 1;

    private List<Perk> _perkPool { get; set; }
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _btnContinue.Pressed += () =>
        {
            EmitSignal(SignalName.UpgradeFinished, GetParsedCharacterPerkDict(_characterPerks));
        };
    }

    public Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>> GetParsedCharacterPerkDict(Dictionary<CharacterUnit, Perk[]> input)
    {
        var output = new Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>>();
        foreach (KeyValuePair<CharacterUnit, Perk[]> kv in input)
        {
            var godotList = new Godot.Collections.Array<Perk>();
            foreach (Perk p in kv.Value)
            {
                godotList.Add(p);
            }
            output[kv.Key] = godotList;
        }
        return output;
    }


    public void SetActiveCharacter(CharacterUnit activeCharacter)
    {
        _activeCharacter = activeCharacter;
    }

    public void Start(List<CharacterUnit> companions, List<Perk> perks, int maxPerCharacter)
    {
        _numberOfPerksPerCharacter = maxPerCharacter;
        _lblPerkSelect.Text = string.Format($"Allocate perks amongst the heroes!\nYou may select a maximum of {maxPerCharacter} per hero.");
        _characterPerks.Clear();
        _perkPool = perks;
        PopulateGrid(perks);

        companions.ForEach(x =>
        {
            _characterPerks[x] = new Perk[maxPerCharacter];
            for (int i = 0; i < maxPerCharacter; i++)
            {
                _characterPerks[x][i] = null;
            }
        });
    }


    private bool TryGivingCharacterPerk(Perk perk, CharacterUnit cUnit)
    {
        if (IsPerkTaken(perk))
        {
            return false;
        }
        if (_numberOfPerksPerCharacter == 1)
        {
            // GD.Print(1);
            ClearCharacterPerks(cUnit);
        }
        if (_characterPerks.TryGetValue(cUnit, out Perk[] characterPerks))
        {
            int nullIndex = Array.FindIndex(characterPerks, x => x == null);

            if (nullIndex != -1)
            {

                characterPerks[nullIndex] = perk;
                UpdatePerkButtonText(perk, cUnit);
                return true;
            }
        }

        return false;
    }

    private void ClearCharacterPerks(CharacterUnit cUnit)
    {
        foreach (Perk perk in _characterPerks[cUnit])
        {
            if (perk != null)
            {
                TakePerkFromCharacter(perk);
                _perkBtns[perk].ButtonPressed = false;
            }
            // GD.Print("clear!");
        }
    }

    private bool IsPerkTaken(Perk perk)
    {
        return _characterPerks.Values.Any(perkList => perkList.Contains(perk));
    }

    private void TakePerkFromCharacter(Perk perk)
    {
        foreach (Perk[] perkList in _characterPerks.Values)
        {
            for (int i = 0; i < perkList.Length; i++)
            {
                if (perkList[i] == perk)
                {
                    UpdatePerkButtonText(perk, null);
                    perkList[i] = null;
                }
            }
        }
        SetButtonContinue();
    }

    private void UpdatePerkButtonText(Perk perk, CharacterUnit character)
    {
        if (perk == null)
        {
            return;
        }
        if (_perkBtns.TryGetValue(perk, out BaseTextureButton btn))
        {
            btn.GetChild<Label>(0).Text = character != null
                ? $"{perk.Name} ({character.CharacterData.Name})"
                : perk.Name;
        }
        SetButtonContinue();
    }
    private bool AreAllPerksAllocated()
    {
        return _characterPerks.Values.Sum(perkList => perkList.Count(perk => perk != null)) == _perkPool.Count || DoAllCharactersHaveMaxPerksAllocated();
    }

    private bool DoAllCharactersHaveMaxPerksAllocated()
    {
        return _characterPerks.Values.All(perkList => perkList.Count(x => x != null) >= _numberOfPerksPerCharacter);
    }


    private void SetButtonContinue()
    {
        _btnContinue.Disabled = !AreAllPerksAllocated();
    }

    private void PopulateGrid(List<Perk> perks)
    {
        _gridContainer.GetChildren().ToList().ForEach(x =>
        {
            x.QueueFree();
        });

        perks.ToList().ForEach(x =>
        {
            NewPerkButton(x);
        });
    }

    private BaseTextureButton NewPerkButton(Perk perk)
    {
        BaseTextureButton btn = new()
        {
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            TextureNormal = GD.Load<Texture2D>(perk.BtnNormalPath),
            TexturePressed = GD.Load<Texture2D>(perk.BtnPressedPath),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ToggleMode = true,
            TooltipText = perk.Description,
        };
        _perkBtns[perk] = btn;
        btn.AddChild(new Label()
        {
            LayoutMode = 1,
            AnchorBottom = 1,
            AnchorLeft = 0,
            AnchorTop = 0.75f,
            AnchorRight = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            AutowrapMode = TextServer.AutowrapMode.Word,
            Text = perk.Name,
        });
        _gridContainer.AddChild(btn);
        btn.Toggled += (bool toggled) =>
        {
            if (toggled)
            {
                if (!TryGivingCharacterPerk(perk, _activeCharacter))
                {
                    btn.ButtonPressed = false;
                }
            }
            else
            {
                TakePerkFromCharacter(perk);
            }
        };
        return btn;

    }
}
public partial class Perk : RefCounted
{
    public enum PerkMode
    {
        SolarFlare, SolarBlast, JudgementOfFlame, BlindingLight, VialOfFury, ElixirOfVigour, ElixirOfSwiftness, RegenerativeOintment,
        LesserArmor, GreaterArmor, BlessedWeapon, EnchantedMeleeWeapon
    }
    public PerkMode CurrentPerk;
    public SpellEffectManager.SpellMode AssociatedSpell { get; set; } = SpellEffectManager.SpellMode.None;
    public Scales.FavourMode Patron { get; set; }
    public int Magnitude { get; set; } = 0;
    // public StoryCharacterData.StatMode AssociatedStat { get; set; }
    public StoryCharacterData.AttributeMode AssociatedAttribute { get; set; }
    public StoryCharacterData.MeleeWeaponMode AssociatedMeleeWeapon { get; set; }
    public StoryCharacterData.RangedWeaponMode AssociatedRangedWeapon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public enum PerkCategory { Spell, AttributeBonus, ArmourBonus, MeleeWeapon, RangedWeapon }
    public PerkCategory Category = PerkCategory.Spell;
    public bool Stackable { get; set; } = false;
    public bool Powerful { get; set; } = false;
    public string BtnNormalPath { get; set; }
    public string BtnPressedPath { get; set; }
}

// hey this style turned out more useful than i thought, i should do the same for spelleffects and ?spells, ??items
public static class PerkFactory
{
    // create perks from a json file using newtonsoft IN FUTURE

    public static Perk GeneratePerk(Perk.PerkMode perkType)
    {
        switch (perkType)
        {
            case Perk.PerkMode.SolarFlare:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.SolarFlare,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    Magnitude = 5,
                    Name = "Solar Flare",
                    Description = "Launch a jolt of solar energy at an enemy. Patron God Shamash. Costs charge.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.SolarBlast:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.SolarBlast,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarBlast,
                    Magnitude = 5,
                    Name = "Solar Blast",
                    Description = "Blast your enemies with the power of the sun. Patron God Shamash. Costs charge.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.BlindingLight:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.BlindingLight,
                    AssociatedSpell = SpellEffectManager.SpellMode.BlindingLight,
                    Magnitude = 3,
                    Name = "Blinding Light",
                    Description = $"Reduces an opponent's hit chance. Patron God Shamash. Costs charge.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Shamash
                };

            case Perk.PerkMode.JudgementOfFlame:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.JudgementOfFlame,
                    AssociatedSpell = SpellEffectManager.SpellMode.JudgementOfFlame,
                    Magnitude = 3,
                    Name = "Judgement of Flame",
                    Description = $"Burn an enemy over time and reduce their might and precision. Patron God Shamash. Costs charge.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Shamash
                };


            case Perk.PerkMode.ElixirOfVigour:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.ElixirOfVigour,
                    AssociatedSpell = SpellEffectManager.SpellMode.ElixirOfVigour,
                    Magnitude = 3,
                    Name = "Elixir of Vigour",
                    Description = $"Enhances an ally's strength and resilience. Patron God Ishtar. Costs reagents.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Ishtar
                };

            case Perk.PerkMode.ElixirOfSwiftness:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.ElixirOfSwiftness,
                    AssociatedSpell = SpellEffectManager.SpellMode.ElixirOfSwiftness,
                    Magnitude = 3,
                    Name = "Elixir of Swiftness",
                    Description = $"Grants an ally improved precision and speed. Patron God Ishtar. Costs reagents.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Ishtar
                };

            case Perk.PerkMode.RegenerativeOintment:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.RegenerativeOintment,
                    AssociatedSpell = SpellEffectManager.SpellMode.RegenerativeOintment,
                    Magnitude = 3,
                    Name = "Regenerative Ointment",
                    Description = $"Improves an ally's health regeneration. Patron God Ishtar. Costs reagents.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Ishtar
                };

            case Perk.PerkMode.VialOfFury:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.VialOfFury,
                    AssociatedSpell = SpellEffectManager.SpellMode.VialOfFury,
                    Magnitude = 3,
                    Name = "Vial of Fury",
                    Description = $"Burn an enemy over time and reduce their might and precision. Patron God Ishtar. Costs reagents.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Ishtar
                };

            case Perk.PerkMode.LesserArmor:
                int armorMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserArmor,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    Magnitude = armorMagnitude,
                    Name = "Lesser Armor Bonus",
                    Description = $"Reinforce your armor by {armorMagnitude}. Reduces physical damage taken.",
                    Category = Perk.PerkCategory.ArmourBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.GreaterArmor:
                int armorGreaterMagnitude = 6;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserArmor,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    Magnitude = armorGreaterMagnitude,
                    Name = "Greater Armor Bonus",
                    Description = $"Reinforce your armor by {armorGreaterMagnitude}. Reduces physical damage taken.",
                    Category = Perk.PerkCategory.ArmourBonus,
                    Stackable = true,
                    Powerful = true,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.EnchantedMeleeWeapon:
                int weaponGreaterMagnitude = 6;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.EnchantedMeleeWeapon,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    Magnitude = weaponGreaterMagnitude,
                    Name = "Enchanted Weapons",
                    Description = $"Hone your melee weapon attacks with Ishtar's fury, improving damage by {weaponGreaterMagnitude}.",
                    Category = Perk.PerkCategory.MeleeWeapon,
                    Stackable = true,
                    Powerful = true,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.BlessedWeapon:
                int weaponLesserMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.BlessedWeapon,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    Magnitude = weaponLesserMagnitude,
                    Name = "Blessed Weapons",
                    Description = $"Secret Ishtari rites bless your attacks, improving damage by {weaponLesserMagnitude}.",
                    Category = Perk.PerkCategory.MeleeWeapon,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
                    BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
                    Patron = Scales.FavourMode.Ishtar
                };

        }

        return null;
    }
}