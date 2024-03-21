using Lavender.Common.Data;

namespace Lavender.Common.Entity.Data;

public class EntityStats
{
    public float Health = 100f;
    public float Stamina = 100f;

    public float MovementJumpImpulse = 14f;
    public float MovementFallMultiplier = 11f;
    public float MovementJumpDampenMultiplier = 3f;
    public float MovementAcceleration = 35f;
    public float MovementSpeedBase = 7f;
    public float MovementSpeedMultiplier = 1.0f;
    public float MovementSprintMultiplier = 1.4f;
}