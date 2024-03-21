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
    
    public void WaveSetup(Marker3D[] botPathPoints, WaveManager waveManager)
    {
        WaveManager = waveManager;
        _botPathPointsCache = botPathPoints;

        Teleport(_botPathPointsCache[0].GlobalPosition);
        SetDesiredPathLocation(_botPathPointsCache[1].GlobalPosition);
    }

    private void TargetNextPoint()
    {
        if (_targetedPointIndex + 1 >= _botPathPointsCache.Length)
        {
            return;
        }

        _targetedPointIndex++;
        SetDesiredPathLocation(_botPathPointsCache[_targetedPointIndex].GlobalPosition);
        LookAt(new Vector3(_botPathPointsCache[_targetedPointIndex].GlobalPosition.X, GlobalPosition.Y, _botPathPointsCache[_targetedPointIndex].GlobalPosition.Z));
        SnapRotationTo(GlobalRotation);
    }

    protected override void HandleTick()
    {
        base.HandleTick();

        if (!Enabled)
            return;

        if (IsClient) 
            return;
        if (_botPathPointsCache[_targetedPointIndex].GlobalPosition.DistanceSquaredTo(GlobalPosition) <= 4f)
        {
            // Reached the targeted path position/point
            if (_targetedPointIndex + 1 >= _botPathPointsCache.Length)
            {
                // Finished the path.
                OnCompletedPathEvent?.Invoke(this);
            }
            TargetNextPoint();
        }

    }

    public void SnapRotationTo(Vector3 rotation)
    {
        GlobalRotation = rotation;
        if (Manager is ServerManager)
        {
            Manager.BroadcastPacketToClients(new EntityRotatePacket()
            {
                NetId = NetId,
                Rotation = GlobalRotation,
            });
        }
    }

    private int _targetedPointIndex = 1;
    
    private Vector3 _targetedLerpPosition = Vector3.Zero;

    public WaveManager WaveManager { get; protected set; }
    private Marker3D[] _botPathPointsCache;

    public delegate void BoomerCompletedPathHandler(BoomerEntity boomerEntity);

    public event BoomerCompletedPathHandler OnCompletedPathEvent;

}