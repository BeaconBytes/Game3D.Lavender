using Godot;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity.GameEntities;

public partial class PlayerSoulEntity : PlayerEntity
{
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
        
    }
}