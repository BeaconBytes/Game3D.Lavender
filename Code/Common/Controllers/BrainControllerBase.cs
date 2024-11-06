using System;
using Godot;
using Lavender.Client.Menus;
using Lavender.Common.Entity;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using Lavender.Common.Registers;

namespace Lavender.Common.Controllers;

public partial class BrainControllerBase : BasicControllerBase
{
    private Vector3 _moveInput = Vector3.Zero;
    private EntityMoveFlags _flagsInput = EntityMoveFlags.None;
    
    
    public override void Setup(uint netId, GameManager gameManager)
    {
        base.Setup(netId, gameManager);
    }

    /// <summary>
    /// Sets the target position on our ReceiverEntity's NavAgent.
    /// Then, somewhat handles rotation(initially) and does a network sync via Teleport() call
    /// on our ReceiverEntity.
    /// </summary>
    public bool SetNavigationTarget(Vector3 position)
    {
        if (ReceiverEntity?.NavAgent != null)
        {
            ReceiverEntity.SetNavTarget(position);
            
            if (ReceiverEntity is not Node3D node3d)
                throw new Exception("ReceiverEntity this BrainControllerBase is connected to IS NOT a Node3D type!");
            
            node3d.LookAt(new Vector3(position.X, node3d.GlobalPosition.Y, position.Z));
            ReceiverEntity.Teleport(ReceiverEntity.WorldPosition, ReceiverEntity.WorldRotation);
            return true;
        }

        return false;
    }
    
}