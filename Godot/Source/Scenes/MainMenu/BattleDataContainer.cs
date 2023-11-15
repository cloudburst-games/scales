

public class BattleDataContainer : ISceneTransitionShareableData
{
    public int Difficulty { get; set; } = 0;
    // public int CharacterSelected { get; set; } = 0;
    public int AdventureSelected { get; set; } = 0;

    // TODO
    public object CheckpointData { get; set; } = null;
}
