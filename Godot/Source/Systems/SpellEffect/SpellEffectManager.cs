using Godot;
using System;
using System.Collections.Generic;

public partial class SpellEffectManager : Node
{
    public enum BattleSpellMode { Arrow }
    public BattleLevel CurrentLevel { get; set; }

    public Dictionary<BattleSpellMode, List<SpellEffectData>> AllSpells = new();

    [Export]
    private Godot.Collections.Array<PackedScene> _spellEffectScns = new();

    [Signal]
    public delegate void SpellEffectFinishedEventHandler(BattleSpellData battleSpellData);



    public override void _Ready()
    {
        GenerateSpells();
    }

    // todo - construct via json data
    private void GenerateSpells()
    {
        AllSpells[BattleSpellMode.Arrow] = new List<SpellEffectData>() {
            new()
            {
                Target = SpellEffectData.TargetMode.Enemy,
                Name = "Arrow",
                BattleEffect = SpellEffectData.BattleEffectMode.Shoot
                // MagnitudeBonus = 0, // all the damage is from the CharacterData (equipped weapon)
            }
        };
    }

    public void GenerateEffect(BattleSpellData battleSpellData)
    {
        BattleSpellMode effect = (BattleSpellMode)battleSpellData.AssociatedSpellEffect;
        switch (effect)
        {
            case BattleSpellMode.Arrow:
                SpellEffect newEffect = _spellEffectScns[(int)effect].Instantiate<SpellEffect>();

                CurrentLevel.AddChild(newEffect); // consdier adding under an entity group
                newEffect.GlobalPosition = battleSpellData.Origin;
                newEffect.SetSpellEffectState(SpellEffect.SpellEffectMode.Projectile);
                newEffect.Finished += this.OnSpellFinished;
                newEffect.Start(battleSpellData);
                break;
        }
    }

    public void OnCastingSpell(BattleSpellData battleSpellData)
    {
        GenerateEffect(battleSpellData);
        // SpellEffectMode spellEffect = (SpellEffectMode)spellEffectNum;

    }

    public void OnSpellFinished(BattleSpellData battleSpellData)
    {
        // GD.Print(2, battleSpellData.TargetCharacter.CharacterData.Name);
        List<SpellEffectData> spells = AllSpells[(SpellEffectManager.BattleSpellMode)battleSpellData.AssociatedSpellEffect];
        foreach (SpellEffectData spell in spells)
        {
            DoBattleEffect(battleSpellData, spell);
        }
        battleSpellData.OriginCharacter.OnSpellEffectFinished();
        // EmitSignal(SignalName.SpellEffectFinished, battleSpellData);
    }

    private void DoBattleEffect(BattleSpellData battleSpellData, SpellEffectData spell)
    {
        switch (spell.BattleEffect)
        {
            case SpellEffectData.BattleEffectMode.Shoot:
                DoShootEffect(battleSpellData);
                break;
        }
    }

    private void DoShootEffect(BattleSpellData battleSpellData)
    {
        // do into roll system
        CharacterUnit originCharacter = battleSpellData.OriginCharacter;
        CharacterUnit targetCharacter = battleSpellData.TargetCharacter;
        // GD.Print(1, targetCharacter.CharacterData.Name);

        StoryCharacterData attackerData = originCharacter.CharacterData;
        StoryCharacterData defenderData = targetCharacter.CharacterData;

        BattleRoller.RollerInput shootAttack = new(
            attackerHitModifier: attackerData.GetCorrectHitBonus(),
            defenderDodgeModifier: defenderData.Dodge,
            attackerDamageModifier: attackerData.GetCorrectWeaponDamageBonus(),
            defenderDamageResist: defenderData.PhysicalResist,
            damageDice: attackerData.WeaponDice,
            criticalThreshold: attackerData.CriticalThreshold
        );

        BattleRoller.RollerOutcomeInformation res = BattleRoller.CalculateAttack(originCharacter.Rand, shootAttack); // can potentially return this to improve the battle log!
        targetCharacter.TakeDamageOrder(res);
    }
}

public partial class BattleSpellData : RefCounted
{
    public Vector2 Origin;
    public Vector2 Destination;
    public CharacterUnit TargetCharacter;
    public CharacterUnit OriginCharacter;
    public int AssociatedSpellEffect;
}