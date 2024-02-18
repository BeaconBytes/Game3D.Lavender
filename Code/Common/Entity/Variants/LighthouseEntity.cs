using Godot;
using Lavender.Common.Globals;
using Lavender.Common.Utils;

namespace Lavender.Common.Entity.Variants;

public partial class LighthouseEntity : FlyingBrainEntity
{
    public override void _Ready()
    {
        base._Ready();
        CollisionSize = 2f;
    }

    protected override Vector3 DoAiMovementLogic()
    {
        if(CurrentTick % (Overseer.SERVER_TICK_RATE * 0.5f) == 0)
        {
            if (_targetEntity is { Destroyed: true })
            {
                _targetEntity = null;
            }
            if (!HasPath || PathPointsCount == 0)
            {
                if (Manager.GetPlayerCount() > 0)
                {
                    if (_targetEntity == null)
                    {
                        _targetEntity = SetTargetToRandomPlayer();
                    }
                }
                
            }
            // This is my first time seeing the following style of "pattern" in a if statement.
            // It was suggested by Rider IDE and seems neat. But since I dont 100% understand it,
            // there may need to be a change here later on.
            //              - StimzRx, 12/09/2023
            if (_targetEntity is { Destroyed: false } && CurrentTargetPosition != null)
            {
                if (MathUtils.FastDistance(_targetEntity.GlobalPosition, CurrentTargetPosition.Value) > 4f)
                {
                    SetDesiredPathLocation(_targetEntity.GlobalPosition);
                }
            }
            else if (_targetEntity == null || (_targetEntity.Dead || MathUtils.FastDistance(_targetEntity.GlobalPosition, GlobalPosition) <= 3.5f))
            {
                StopPathing();
            }
        }
        
        return base.DoAiMovementLogic();
    }

    protected override void OnReachedGoal()
    {
        base.OnReachedGoal();
        
    }


    private LivingEntity _targetEntity = null;
    
    private bool _didFirstPath = false;
    
}