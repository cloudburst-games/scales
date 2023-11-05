using Godot;

public partial class SpellEffectData : RefCounted
{
    public enum TargetMode { Ally, Enemy, Ground }
    public enum BattleEffectMode { Damage, Heal, Teleport }
    public string Name { get; set; }
    public TargetMode Target { get; set; }
    public BattleEffectMode BattleEffect { get; set; }
    public int Area { get; set; }
    public int Magnitude { get; set; }

    public int Cost { get; set; }
}