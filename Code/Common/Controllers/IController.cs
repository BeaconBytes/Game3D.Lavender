using System.Collections.Generic;
using Godot;
using Lavender.Common.Entity;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Managers;

namespace Lavender.Common.Controllers;

public interface IController : INetNode
{
    public void SetControlling(IGameEntity gameEntity);

    /// <summary>
    /// Asks this controller to respawn its receiver and handle events and setup as needed.
    /// </summary>
    public void ServerRespawnReceiver();

    /// <summary>
    /// Called once every Network Tick
    /// </summary>
    public void NetworkProcess(double delta);
    
    
    public Vector3 LookInput { get; }
    public Vector3 MoveInput { get; }
    public EntityMoveFlags MoveFlagsInput { get; }
    public IGameEntity ReceiverEntity { get; }
    public bool Destroyed { get; }
    public GameManager Manager { get; }

    public event GameManager.SimpleNetNodeEventHandler DestroyedEvent;
}