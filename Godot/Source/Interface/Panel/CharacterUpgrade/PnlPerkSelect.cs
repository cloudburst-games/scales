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

    // [Signal]
    // public delegate void UpgradeFinishedEventHandler(Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>> characterPerks);
    [Export]
    private Label _lblPerkSelect;
    private CharacterUnit _activeCharacter;

    private Dictionary<CharacterUnit, Perk[]> _characterPerks = new();
    private Dictionary<Perk, BaseTextureButton> _perkBtns = new();

    private int _numberOfPerksPerCharacter = 1;

    private List<Perk> _perkPool { get; set; }

    public delegate void FinishedSelectingPerksEventHandler(Dictionary<CharacterUnit, Perk[]> characterPerks);
    public event FinishedSelectingPerksEventHandler FinishedSelectingPerks;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _btnContinue.Pressed += () =>
        {
            GD.Print(99);
            FinishedSelectingPerks?.Invoke(_characterPerks);
            // EmitSignal(SignalName.UpgradeFinished, GetParsedCharacterPerkDict(_characterPerks));
        };
    }

    public void Exit()
    {
        FinishedSelectingPerks = null;
    }

    // public Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>> GetParsedCharacterPerkDict(Dictionary<CharacterUnit, Perk[]> input)
    // {
    //     GD.Print(100);
    //     var output = new Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>>();
    //     GD.Print(200);
    //     foreach (KeyValuePair<CharacterUnit, Perk[]> kv in input)
    //     {
    //         GD.Print(300);
    //         var godotList = new Godot.Collections.Array<Perk>();
    //         GD.Print(400);
    //         foreach (Perk p in kv.Value)
    //         {
    //             GD.Print(500);
    //             godotList.Add(p);
    //             GD.Print(600);
    //         }
    //         output[kv.Key] = godotList;
    //         GD.Print(700);
    //     }
    //     GD.Print(800);
    //     return output;
    // }
    [Export]
    private AudioContainer _audioPerkSelect;

    public void SetActiveCharacter(CharacterUnit activeCharacter)
    {
        _activeCharacter = activeCharacter;
    }

    public void Start(List<CharacterUnit> companions, List<Perk> perks, int maxPerCharacter)
    {

        _numberOfPerksPerCharacter = maxPerCharacter;
        _lblPerkSelect.Text = string.Format($"Choose a hero and allocate a boon.\nSelect a maximum of {maxPerCharacter} per hero.");
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
        _audioPerkSelect.Play();
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
            TextureHover = GD.Load<Texture2D>(perk.BtnHoverPath),
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
            AnchorTop = 0.9f,
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
        LesserArmor, GreaterArmor, BlessedWeapon, EnchantedMeleeWeapon, WoodenKnuckles, BrassKnuckles, BluntKnife, JewelledDagger,
        Sling, Javelin, LesserMight, LesserResilience, LesserPrecision, LesserSpeed, LesserCharisma, LesserLuck, LesserIntellect,
        GreaterMight, GreaterResilience, GreaterPrecision, GreaterSpeed, GreaterCharisma, GreaterLuck, GreaterIntellect, None
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
    public enum PerkCategory
    {
        Spell, AttributeBonus, ArmourBonus, MeleeWeapon, RangedWeapon,
        DamageBonus
    }
    public PerkCategory Category = PerkCategory.Spell;
    public bool Stackable { get; set; } = false;
    public bool Powerful { get; set; } = false;
    public string BtnNormalPath { get; set; }
    public string BtnPressedPath { get; set; }
    public string BtnHoverPath { get; set; }
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
                    Description = "Launch a jolt of solar energy at an enemy. Patron God Shamash. Costs mana.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/SolarFlare2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/SolarFlare2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/SolarFlare2.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.SolarBlast:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.SolarBlast,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarBlast,
                    Magnitude = 5,
                    Name = "Solar Blast",
                    Description = "Blast your enemies with the power of the sun. Patron God Shamash. Costs mana.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/SolarBlast2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/SolarBlast2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/SolarBlast2.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.BlindingLight:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.BlindingLight,
                    AssociatedSpell = SpellEffectManager.SpellMode.BlindingLight,
                    Magnitude = 3,
                    Name = "Blinding Light",
                    Description = $"Reduces an opponent's hit chance. Patron God Shamash. Costs mana.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/BlindingLight2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/BlindingLight2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/BlindingLight2.png",
                    Patron = Scales.FavourMode.Shamash
                };

            case Perk.PerkMode.JudgementOfFlame:
                return new()
                {
                    CurrentPerk = Perk.PerkMode.JudgementOfFlame,
                    AssociatedSpell = SpellEffectManager.SpellMode.JudgementOfFlame,
                    Magnitude = 3,
                    Name = "Judgement of Flame",
                    Description = $"Burn an enemy over time and reduce their might and precision. Patron God Shamash. Costs mana.",
                    Category = Perk.PerkCategory.Spell,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/FlameJudgement2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/FlameJudgement2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/FlameJudgement2.png",
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
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/Vigor2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/Vigor2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/Vigor2.png",
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
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/Swift2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/Swift2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/Swift2.png",
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
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/Healing2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/Healing2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/Healing2.png",
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
                    BtnNormalPath = "res://Assets/Graphics/Icons/IconNormal/Fury2.png",
                    BtnPressedPath = "res://Assets/Graphics/Icons/IconPressed/Fury2.png",
                    BtnHoverPath = "res://Assets/Graphics/Icons/IconHover/Fury2.png",
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
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/LesserArmor.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/LesserArmor.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/LesserArmor.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.GreaterArmor:
                int armorGreaterMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterArmor,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    Magnitude = armorGreaterMagnitude,
                    Name = "Greater Armor Bonus",
                    Description = $"Reinforce your armor by {armorGreaterMagnitude}. Reduces physical damage taken.",
                    Category = Perk.PerkCategory.ArmourBonus,
                    Stackable = true,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/GreaterArmor.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/GreaterArmor.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/GreaterArmor.png",
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
                    Category = Perk.PerkCategory.DamageBonus,
                    Stackable = true,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/EnchantedWeapon.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/EnchantedWeapon.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/EnchantedWeapon.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.BlessedWeapon:
                int weaponLesserMagnitude = 2;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.BlessedWeapon,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    Magnitude = weaponLesserMagnitude,
                    Name = "Blessed Weapons",
                    Description = $"Secret Ishtari rites bless your attacks, improving damage by {weaponLesserMagnitude}.",
                    Category = Perk.PerkCategory.DamageBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/BlessedWeapon.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/BlessedWeapon.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/BlessedWeapon.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.WoodenKnuckles:
                // int weaponLesserMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.WoodenKnuckles,
                    AssociatedMeleeWeapon = StoryCharacterData.MeleeWeaponMode.WoodenKnuckles,
                    // Magnitude = weaponLesserMagnitude,
                    Name = "Fists of bark",
                    Description = $"Knuckles of carved wood. Strength weapon. 1d6 base damage.",
                    Category = Perk.PerkCategory.MeleeWeapon,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/WoodKnuckles.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/WoodKnuckles.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/WoodKnuckles.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.BrassKnuckles:
                // int weaponLesserMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.BrassKnuckles,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    AssociatedMeleeWeapon = StoryCharacterData.MeleeWeaponMode.BrassKnuckles,
                    // Magnitude = weaponLesserMagnitude,
                    Name = "Brass Knuckles",
                    Description = $"Reinforce your fists with hardened brass. Strength weapon. 1d8 base damage.",
                    Category = Perk.PerkCategory.MeleeWeapon,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/BrassKnuckles.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/BrassKnuckles.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/BrassKnuckles.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.BluntKnife:
                // int weaponLesserMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.BluntKnife,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    AssociatedMeleeWeapon = StoryCharacterData.MeleeWeaponMode.BluntKnife,
                    // Magnitude = weaponLesserMagnitude,
                    Name = "Dull Knife",
                    Description = $"A worn dagger. Precision weapon. 1d4 base damage.",
                    Category = Perk.PerkCategory.MeleeWeapon,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/AgileKnife.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/AgileKnife.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/AgileKnife.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.JewelledDagger:
                // int weaponLesserMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.JewelledDagger,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    AssociatedMeleeWeapon = StoryCharacterData.MeleeWeaponMode.JewelledDagger,
                    // Magnitude = weaponLesserMagnitude,
                    Name = "Jewelled Dagger",
                    Description = $"Unnaturally sharp and adorned with gems. Precision weapon. 2d4 base damage.",
                    Category = Perk.PerkCategory.MeleeWeapon,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/JewelKnife.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/JewelKnife.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/JewelKnife.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.Sling:
                // int weaponLesserMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.Sling,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    AssociatedRangedWeapon = StoryCharacterData.RangedWeaponMode.Sling,
                    // Magnitude = weaponLesserMagnitude,
                    Name = "Sling",
                    Description = $"A simple sling. 1d4 base damage.",
                    Category = Perk.PerkCategory.RangedWeapon,
                    Stackable = false,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Sling.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Sling.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Sling.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.Javelin:
                // int weaponLesserMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.Javelin,
                    AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
                    AssociatedRangedWeapon = StoryCharacterData.RangedWeaponMode.Javelin,
                    // Magnitude = weaponLesserMagnitude,
                    Name = "Javelin",
                    Description = $"An extraordinarily light weapon, blessed by Shamash. 1d8 base damage.",
                    Category = Perk.PerkCategory.RangedWeapon,
                    Stackable = false,
                    Powerful = true,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Javelin.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Javelin.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Javelin.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.LesserMight:
                int mightMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserMight,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Might,
                    Magnitude = mightMagnitude,
                    Name = "Lesser Might",
                    Description = $"Improve might by {mightMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/lesser_LesserMight.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/lesser_LesserMight.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/lesser_LesserMight.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.LesserResilience:
                int resMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserResilience,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Resilience,
                    Magnitude = resMagnitude,
                    Name = "Lesser Resilience",
                    Description = $"Improve resilience by {resMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/lesser_Resilience.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/lesser_Resilience.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/lesser_Resilience.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.LesserSpeed:
                int speedMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserSpeed,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Speed,
                    Magnitude = speedMagnitude,
                    Name = "Lesser Speed",
                    Description = $"Improve speed by {speedMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/lesser_Speed.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/lesser_Speed.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/lesser_Speed.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.LesserPrecision:
                int precMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserPrecision,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Precision,
                    Magnitude = precMagnitude,
                    Name = "Lesser Precision",
                    Description = $"Improve precision by {precMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/lesser_Precision.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/lesser_Precision.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/lesser_Precision.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.LesserIntellect:
                int intMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserIntellect,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Intellect,
                    Magnitude = intMagnitude,
                    Name = "Lesser Intellect",
                    Description = $"Improve intellect by {intMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/lesser_Intellect.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/lesser_Intellect.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/lesser_Intellect.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.LesserCharisma:
                int chaMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserCharisma,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Charisma,
                    Magnitude = chaMagnitude,
                    Name = "Lesser Charisma",
                    Description = $"Improve charisma by {chaMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/lesser_Charisma.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/lesser_Charisma.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/lesser_Charisma.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.LesserLuck:
                int luckMagnitude = 3;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.LesserLuck,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Luck,
                    Magnitude = luckMagnitude,
                    Name = "Lesser Luck",
                    Description = $"Improve luck by {luckMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/lesser_Luck.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/lesser_Luck.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/lesser_Luck.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.GreaterMight:
                int gmightMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterMight,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Might,
                    Magnitude = gmightMagnitude,
                    Name = "Greater Might",
                    Description = $"Improve might by {gmightMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/GreaterMight.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/GreaterMight.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/GreaterMight.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.GreaterResilience:
                int gresMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterResilience,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Resilience,
                    Magnitude = gresMagnitude,
                    Name = "Greater Resilience",
                    Description = $"Improve resilience by {gresMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Greater_Resilience.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Greater_Resilience.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Greater_Resilience.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.GreaterSpeed:
                int gspeedMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterSpeed,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Speed,
                    Magnitude = gspeedMagnitude,
                    Name = "Greater Speed",
                    Description = $"Improve speed by {gspeedMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Greater_Speed.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Greater_Speed.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Greater_Speed.png",
                    Patron = Scales.FavourMode.Ishtar
                };
            case Perk.PerkMode.GreaterPrecision:
                int gprecMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterPrecision,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Precision,
                    Magnitude = gprecMagnitude,
                    Name = "Greater Precision",
                    Description = $"Improve precision by {gprecMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Greater_Precision.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Greater_Precision.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Greater_Precision.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.GreaterIntellect:
                int gintMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterIntellect,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Intellect,
                    Magnitude = gintMagnitude,
                    Name = "Greater Intellect",
                    Description = $"Improve intellect by {gintMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Greater_Intellect.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Greater_Intellect.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Greater_Intellect.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.GreaterCharisma:
                int gchaMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterCharisma,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Charisma,
                    Magnitude = gchaMagnitude,
                    Name = "Greater Charisma",
                    Description = $"Improve charisma by {gchaMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Greater_Charisma.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Greater_Charisma.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Greater_Charisma.png",
                    Patron = Scales.FavourMode.Shamash
                };
            case Perk.PerkMode.GreaterLuck:
                int gluckMagnitude = 8;
                return new()
                {
                    CurrentPerk = Perk.PerkMode.GreaterLuck,
                    AssociatedAttribute = StoryCharacterData.AttributeMode.Luck,
                    Magnitude = gluckMagnitude,
                    Name = "Greater Luck",
                    Description = $"Improve luck by {gluckMagnitude}.",
                    Category = Perk.PerkCategory.AttributeBonus,
                    Stackable = true,
                    Powerful = false,
                    BtnNormalPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksNormal/Greater_Luck.png",
                    BtnPressedPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksPressed/Greater_Luck.png",
                    BtnHoverPath = "res://Assets/Graphics/Sprites/Perks/SpellsPerksHover/Greater_Luck.png",
                    Patron = Scales.FavourMode.Ishtar
                };
        }

        return null;
    }
}
// WoodenKnuckles, BrassKnuckles, AgileKnife, JewelledDagger,
//         Sling, Javelin, LesserMight, LesserResilience, LesserPrecision, LesserSpeed, LesserCharisma, LesserLuck, LesserIntellect,
//         GreaterMight, GreaterResilience, GreaterPrecision, GreaterSpeed, GreaterCharisma, GreaterLuck, GreaterIntellect

// ishtar
// might
// resilience
// speed

// shamash
// precision
// charisma
// intellect

// shamash lesser intellect

// ishtar greater intellect
