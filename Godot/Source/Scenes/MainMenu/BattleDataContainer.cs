

public class BattleDataContainer : ISceneTransitionShareableData
{
    public int Difficulty { get; set; } = 0;
    // public int CharacterSelected { get; set; } = 0;
    public int AdventureSelected { get; set; } = 0;

    // TODO
    public CheckpointData CheckpointData { get; set; } = null;
}
