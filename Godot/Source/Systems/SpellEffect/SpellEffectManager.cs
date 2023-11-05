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
                BattleEffect = SpellEffectData.BattleEffectMode.Damage,
                Magnitude = 2,
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
            case SpellEffectData.BattleEffectMode.Damage:
                DoHarmEffect(battleSpellData, spell);
                break;
        }
    }

    private void DoHarmEffect(BattleSpellData battleSpellData, SpellEffectData spell)
    {
        // do into roll system
        CharacterUnit originCharacter = battleSpellData.OriginCharacter;
        CharacterUnit targetCharacter = battleSpellData.TargetCharacter;
        int magnitude = spell.Magnitude;

        // roll it!
        GD.Print("damage dealt");
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