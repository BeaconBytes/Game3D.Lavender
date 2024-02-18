using Lavender.Common.Data;

namespace Lavender.Common.Entity.Data;

public class EntityStats
{
    public float Health = 100f;
    public float Stamina = 100f;

    public float MovementJumpImpulse = 45f;
    public float MovementFallMultiplier = 12f;
    public float MovementJumpDampenMultiplier = 3f;
    public float MovementAcceleration = 100f;
    public float MovementSpeedBase = 30f;
    public float MovementSpeedMultiplier = 1.0f;
    public float MovementSprintMultiplier = 1.4f;
}