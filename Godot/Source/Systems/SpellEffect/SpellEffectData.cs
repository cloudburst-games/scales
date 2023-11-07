using System;
using System.Collections.Generic;
using Godot;

public partial class SpellEffectData : RefCounted
{
    public enum TargetMode { Ally, Enemy, Ground }
    public enum BattleEffectMode { Shoot, Heal, Teleport }
    public string Name { get; set; }
    public TargetMode Target { get; set; }
    public BattleEffectMode BattleEffect { get; set; }
    public int Area { get; set; }
    public int MagnitudeBonus { get; set; }
    public List<Tuple<int, int>> MagnitudeDice { get; set; } = new();

    public int Cost { get; set; }
}