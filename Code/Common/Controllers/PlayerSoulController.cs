using Lavender.Common.Enums.Entity;
using Lavender.Common.Managers;

namespace Lavender.Common.Controllers;

public partial class PlayerSoulController : PlayerController
{
    public override void Setup(uint netId, GameManager gameManager)
    {
        base.Setup(netId, gameManager);

        MovementMode = EntityMovementMode.Flight;
    }
}