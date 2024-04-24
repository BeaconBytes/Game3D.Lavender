using Godot;
using Lavender.Common.Entity;
using Lavender.Common.Managers;

namespace Lavender.Common.Controllers;

public partial class PlayerController : Node, IController
{
    public void Setup(uint netId, GameManager gameManager)
    {
        
    }

    public void NetworkTick(double delta)
    {
        
    }

    public void GameTick(double delta)
    {
        
    }

    public uint NetId { get; }

    public IGameEntity ReceiverEntity { get; }
}