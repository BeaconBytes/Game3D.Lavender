using Lavender.Common.Data;

namespace Lavender.Common.Entity.Data;

public class EntityStats
{
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; set; } = 100f;
    public float Stamina { get; set; } = 100f;
    public float MaxStamina { get; set; } = 100f;

    public float MovementJumpImpulse { get; set; } = 7f;
    public float MovementAcceleration { get; set; } = 35f;
    public float MovementSpeedBase { get; set; } = 7f;
    public float MovementSpeedMultiplier { get; set; } = 1.0f;
    public float MovementSprintMultiplier { get; set; } = 1.4f;
}