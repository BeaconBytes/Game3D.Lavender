using Lavender.Common.Entity;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Managers;

namespace Lavender.Common.Controllers;

public interface IController : INetNode
{
    public void SetControlling(IGameEntity gameEntity);

    /// <summary>
    /// Asks this controller to respawn its receiver and handle events and setup as needed.
    /// </summary>
    public void RespawnReceiver();

    /// <summary>
    /// Called once every Network Tick
    /// </summary>
    public void NetworkProcess(double delta);
    
    public IGameEntity ReceiverEntity { get; }
    public bool Destroyed { get; }

    public event GameManager.SimpleNetNodeEventHandler DestroyedEvent;
}