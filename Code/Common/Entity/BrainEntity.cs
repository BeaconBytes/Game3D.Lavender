using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;

namespace Lavender.Common.Entity;

public partial class BrainEntity : LivingEntity
{
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
                lookInput = Vector3.Zero,
                moveInput = moveDirVec,
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
}