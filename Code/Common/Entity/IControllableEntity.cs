namespace Lavender.Common.Entity;

public interface IControllableEntity : IGameEntity
{
    public void SetControllerParent(uint netId);
    public uint ControllerParentNetId { get; }
}