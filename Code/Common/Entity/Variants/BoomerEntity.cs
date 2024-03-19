using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Interfaces.Management.Waves;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;

namespace Lavender.Common.Entity.Variants;

public partial class BoomerEntity : LivingEntity, IWaveEnemy
{
    
    public void WaveSetup(Marker3D[] botPathPoints, WaveManager waveManager)
    {
        WaveManager = waveManager;
        _botPathPointsCache = botPathPoints;

        Teleport(_botPathPointsCache[0].GlobalPosition);
        SetDesiredPathLocation(_botPathPointsCache[5].GlobalPosition);
    }
    
    protected override void HandleTick()
    {
        base.HandleTick();

        if (Manager.IsClient)
        {
            if (LatestServerState.Equals(default(StatePayload)) ||
                (!LastProcessedState.Equals(default(StatePayload)) && LatestServerState.Equals(LastProcessedState)))
            {
                HandleServerReconciliation();
            }
            
            LastProcessedState = LatestServerState;
            GlobalPosition = LatestServerState.position;
            GlobalRotation = LatestServerState.rotation;
        }
        else
        {
            Vector3 curPos = GlobalPosition;
            Vector3 nextPos = _navAgent.GetNextPathPosition();

            Vector3 moveDirVec = (nextPos - curPos).Normalized();
            moveDirVec.Y = 0;
            InputPayload inputPayload = new InputPayload()
            {
                tick = CurrentTick,
                lookInput = new Vector3(0f, 0f, 0f),
                moveInput = new Vector3(moveDirVec.X, 0f, moveDirVec.Z),
                flagsInput = EntityMoveFlags.None,
            };
            uint bufferIndex = inputPayload.tick % BUFFER_SIZE;

            StatePayload statePayload = ProcessMovement(inputPayload);
            StateBuffer[bufferIndex] = statePayload;
            
            Manager.BroadcastPacketToClients(new EntityStatePayloadPacket()
            {
                NetId = NetId,
                StatePayload = StateBuffer[bufferIndex],
            });
        }
    }
    
    public void SetDesiredPathLocation(Vector3 pos)
    {
        _navAgent.TargetPosition = pos;
    }
    
    [Export]
    private NavigationAgent3D _navAgent;

    public WaveManager WaveManager { get; protected set; }
    private Marker3D[] _botPathPointsCache;

}