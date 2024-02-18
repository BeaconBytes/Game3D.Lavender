using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;

namespace Lavender.Common.Entity.Variants;

public partial class DevEntity : HumanoidEntity
{
    protected override void HandleTick()
    {
        base.HandleTick();
        if (CurrentTick % 30 == 0)
        {
            if(OwnerEntity != null && !Manager.IsClient)
                _navAgent.TargetPosition = OwnerEntity.WorldPosition;
        }

        if (Manager.IsClient)
        {
            if (LatestServerState.Equals(default(StatePayload)) ||
                (!LastProcessedState.Equals(default(StatePayload)) && LatestServerState.Equals(LastProcessedState)))
            {
                HandleServerReconciliation();
            }
            
            LastProcessedState = LatestServerState;
            GlobalPosition = LatestServerState.position;
            RotateHead(LatestServerState.rotation);
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

    public void SetOwner(IGameEntity ownerEntity)
    {
        OwnerEntity = ownerEntity;
    }
    
    public IGameEntity OwnerEntity { get; private set; }
    
    [Export]
    private NavigationAgent3D _navAgent;
    
}