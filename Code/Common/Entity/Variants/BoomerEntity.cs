using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Interfaces.Management.Waves;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Server.Managers;

namespace Lavender.Common.Entity.Variants;

public partial class BoomerEntity : BrainEntity, IWaveEnemy
{
    public void SetupWave(Marker3D[] botPathPoints, WaveManager waveManager)
    {
        WaveManager = waveManager;
        _botPathPointsCache = botPathPoints;

        Teleport(_botPathPointsCache[0].GlobalPosition);
        _targetedPointIndex = 0;
        TargetNextPoint();
    }

    private void TargetNextPoint()
    {
        if (_targetedPointIndex + 1 >= _botPathPointsCache.Length)
        {
            return;
        }

        _targetedPointIndex++;
        SetDesiredPathLocation(_botPathPointsCache[_targetedPointIndex].GlobalPosition);
    }
    
    protected override void HandleTick()
    {
        base.HandleTick();

        if (!Enabled)
            return;

        if (IsClient) 
            return;
        
        if (NavAgent.IsTargetReached())
        {
            // Reached the targeted path position/point
            if (_targetedPointIndex + 1 >= _botPathPointsCache.Length)
            {
                // Finished the path.
                TriggerPathCompletedEvent();
            }
            TargetNextPoint();
        }

    }


    private int _targetedPointIndex = 1;
    
    private Vector3 _targetedLerpPosition = Vector3.Zero;

    public WaveManager WaveManager { get; protected set; }
    private Marker3D[] _botPathPointsCache;

}