// 1. sort out character selection.
// - start with a list of CharacterUnits
// - populate the HBoxContainer with new BaseTextureButtons based on this list (free children first)
// - create a dict as well to connect each button with a characterunit
// - the first character starts selected (so the button is grayed out)
// - when a character is selected, set _activeCharacter to this one

// 2. sort out perk selection
// - each character is tied to an array (1 or 2) of selected perks
// - adjust the perk selection screen whenever a character is selected based on this array
// - when a perk is toggled, update this for the selected character, and vice versa. write the name of the perk, and the name of the character (if chosen) under the perk
// - only allow a perk to be toggled if it is not chosen by another character
// - when either all perks have been allocated, or all characters have had 1 / 2 perks allocated each,, un-disable click continue


using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CntCharacterUpgrade : Control
{
	[Export]
	private PnlCharacterSelect _pnlCharacterSelect;

	[Export]
	private PnlPerkSelect _pnlPerkSelect;

	// private List<Perk> _availablePerks;
	// private List<CharacterUnit> _companions;
	[Export]
	private BaseTextureButton _btnContinue;
	// [Signal]
	// public delegate void UpgradeFinishedEventHandler(Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>> characterPerks);

	public override void _Ready()
	{
		_pnlCharacterSelect.CharacterSelected += OnCharacterSelected;
		_btnContinue.Pressed += () => Visible = false;
		// _pnlPerkSelect.UpgradeFinished += (Godot.Collections.Dictionary<CharacterUnit, Godot.Collections.Array<Perk>> result) =>
		// {
		//     EmitSignal(SignalName.UpgradeFinished, result);
		// };

		Visible = false;

		// Test();
	}

	public void Start(List<CharacterUnit> companions, List<Perk> availablePerks, int maxPerCharacter)
	{
		Visible = true;
		_pnlPerkSelect.Start(companions, availablePerks, maxPerCharacter); // tolist to prevent sid eeffects

		_pnlCharacterSelect.Start(companions);
	}

	private void OnCharacterSelected(CharacterUnit cUnit, bool toggled)
	{
		// GD.Print(cUnit.CharacterData.Name);
		_pnlPerkSelect.SetActiveCharacter(cUnit);

		// GD.Print("Selected: ", toggled);
		// GD.Print("_availablePerks: ", _availablePerks.Count);

	}
	private void Test()
	{
		// _pnlPerkSelect.UpgradeFinished += (result) =>
		// {
		//     foreach (CharacterUnit key in result.Keys)
		//     {
		//         GD.Print(key.CharacterData.Name);
		//         GD.Print("Perks:");

		//         foreach (Perk perk in result[key])
		//         {
		//             if (perk != null)
		//             {
		//                 GD.Print(perk.Name);
		//             }
		//         }
		//     }
		//     // Handle the result dictionary here
		// };

		var availablePerks = new List<Perk>
	{
		new Perk
		{
			CurrentPerk = Perk.PerkMode.SolarFlare,
			AssociatedSpell = SpellEffectManager.SpellMode.SolarFlare,
			Magnitude = 5,
			Name = "Flame Mastery",
			Description = "Increases fire spell damage and strength.",
			Category = Perk.PerkCategory.Spell,
			Stackable = true,
			Powerful = false,
			BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0000.png",
			BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0001-a.png",
		},
		new Perk
		{
			CurrentPerk = Perk.PerkMode.SolarBlast,
			Magnitude = 2,
			Name = "Precision Shooter",
			Description = "Improves accuracy and damage for ranged attacks.",
			Category = Perk.PerkCategory.RangedWeapon,
			Stackable = false,
			Powerful = true,
			BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0011-a.png",
			BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/0011-b.png",
		},
		new Perk
		{
			CurrentPerk = Perk.PerkMode.LesserArmor,
			Magnitude = 3,
			Name = "Defensive Stance",
			Description = "Provides additional armor for better defense.",
			Category = Perk.PerkCategory.ArmourBonus,
			Stackable = true,
			Powerful = false,
			BtnNormalPath = "res://Source/Utils/Terrain/Examples/SophTest/images/1101.png",
			BtnPressedPath = "res://Source/Utils/Terrain/Examples/SophTest/images/1110.png",
		},
		new Perk
		{
			CurrentPerk = Perk.PerkMode.SolarBlast,
			Magnitude = 2,
			Name = "Crossbow Mastery",
			Description = "Enhances accuracy and damage for crossbow attacks.",
			Category = Perk.PerkCategory.RangedWeapon,
			Stackable = false,
			Powerful = true,
			BtnNormalPath = "res://Source/Utils/Terrain/Examples/ExampleTilesheet/Output/Snow/3300.PNG",
			BtnPressedPath = "res://Source/Utils/Terrain/Examples/ExampleTilesheet/Output/Snow/3303.PNG",
		},
		new Perk
		{
			CurrentPerk = Perk.PerkMode.SolarFlare,
			Name = "Healing Touch",
			Description = "Improves the effectiveness of healing spells.",
			Category = Perk.PerkCategory.Spell,
			Stackable = true,
			Powerful = true,
			BtnNormalPath = "res://Source/Utils/Terrain/Examples/ExampleTilesheet/Output/Dirt/2002.PNG",
			BtnPressedPath = "res://Source/Utils/Terrain/Examples/ExampleTilesheet/Output/Dirt/2020.PNG",
		},
		new Perk
		{
			CurrentPerk = Perk.PerkMode.GreaterArmor,
			Magnitude = 2,
			Name = "Light Armor",
			Description = "Provides additional armor for better defense.",
			Category = Perk.PerkCategory.ArmourBonus,
			Stackable = true,
			Powerful = false,
			BtnNormalPath = "res://Source/Utils/Terrain/Examples/ExampleTilesheet/Output/Snow/0030.PNG",
			BtnPressedPath = "res://Source/Utils/Terrain/Examples/ExampleTilesheet/Output/Snow/0003.PNG",
		},
	};

		var companions = new List<CharacterUnit>
	{
		new CharacterUnit
		{
			CharacterData = new StoryCharacterData
			{
				Name = "Sandro",
				CharacterBtnNormalPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
				CharacterBtnPressedPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait2.png",
				PortraitPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
			},
		},
		new CharacterUnit
		{
			CharacterData = new StoryCharacterData
			{
				Name = "Charity",
				CharacterBtnNormalPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AMABPortrait.png",
				CharacterBtnPressedPath = "res://Assets/Graphics/Sprites/Actors/Portraits/GigaPortraitv1.png",
				PortraitPath = "res://Assets/Graphics/Sprites/Actors/Portraits/GigaPortraitv1.png",
			},
		},
		new CharacterUnit
		{
			CharacterData = new StoryCharacterData
			{
				Name = "Maximus",
				CharacterBtnNormalPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
				CharacterBtnPressedPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait2.png",
				PortraitPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
			},
		},
		new CharacterUnit
		{
			CharacterData = new StoryCharacterData
			{
				Name = "Falagar",
				CharacterBtnNormalPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
				CharacterBtnPressedPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait2.png",
				PortraitPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
			},
		},
		new CharacterUnit
		{
			CharacterData = new StoryCharacterData
			{
				Name = "Fafner",
				CharacterBtnNormalPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
				CharacterBtnPressedPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait2.png",
				PortraitPath = "res://Assets/Graphics/Sprites/Actors/Portraits/AFABPortrait1.png",
			},
		},
	};
		Start(companions, availablePerks, 1);
		// _pnlPerkSelect.Start(companions, availablePerks.ToList(), 2); // tolist to prevent sid eeffects

		// _pnlCharacterSelect.Start(companions);


	}


	public override void _Process(double delta)
	{
	}

	internal void Exit()
	{
		_pnlCharacterSelect.Exit();
		// _pnlPerkSelect.Exit();
	}
}
