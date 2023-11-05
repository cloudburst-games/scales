public interface ISceneTransitionable
{
    void OnReceivedSharedData(ISceneTransitionShareableData sharedData); // Before enters the scene tree
    void QueueFree();
}