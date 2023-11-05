// Depending on requirements, construct a class (e.g. PlayerData) that implements this interface, to be passed between
// transition scenes.
public interface ISceneTransitionShareableData
{

}

// Example
public class PlayerDataExample: ISceneTransitionShareableData
{
    public string Name {get; set;} = "Panda";
    public float Health {get; set;} = 50f;
}