using Godot;
using Lavender.Client.Menus;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Networking.Packets.Variants.Other;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity.Variants;

public partial class PlayerEntity : HumanoidEntity
{
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
    }

    public override void HandleControllerInputs(RawInputs inputs)
    {
        if (!Enabled)
            return;
        
        if (Manager.IsClient && Manager.ClientController.ReceiverEntity == this)
        {
            if (!LatestServerState.Equals(default(StatePayload)) && 
             (LastProcessedState.Equals(default(StatePayload)) || !LatestServerState.Equals(LastProcessedState)))
            {
                HandleServerReconciliation();
            }
                
            uint bufferIndex = CurrentTick % GameManager.NET_BUFFER_SIZE;

            Vector3 realMoveDirection = inputs.MoveInput.Rotated( Vector3.Up, GlobalTransform.Basis.GetEuler( ).Y ).Normalized( );
                
            InputPayload inputPayload = new()
            {
                tick  = CurrentTick,
                moveInput = realMoveDirection,
                lookInput = inputs.LookInput,
                flagsInput = inputs.FlagsInput,
            };
            inputs.LookInput = Vector3.Zero;
                
            InputBuffer[bufferIndex] = inputPayload;
            StateBuffer[bufferIndex] = ProcessMovement(inputPayload);
		
            Manager.SendPacketToServer(new EntityInputPayloadPacket()
            {
                NetId = NetId,
                InputPayload = inputPayload,
            });
            
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (!IsClient)
            return;

        if (Manager.ClientController.ReceiverEntity != this)
        {
            LastProcessedState = LatestServerState;
            _targetedLerpPosition = LatestServerState.position;
            
            WorldPosition = WorldPosition.Lerp(_targetedLerpPosition, Stats.FullMoveSpeed * (float)delta);
            RotateHead(LatestServerState.rotation);
        }
    }

    protected override void NetworkProcess(double delta)
    {
        base.NetworkProcess(delta);

        if (IsClient)
            return;
        uint bufferIndex = 0;
        bool foundBufferIndex = ( InputQueue.Count > 0 );
        
        while (InputQueue.Count > 0)
        {
            InputPayload inputPayload = InputQueue.Dequeue();
            bufferIndex = inputPayload.tick % GameManager.NET_BUFFER_SIZE;

            StatePayload statePayload = ProcessMovement(inputPayload);
            StateBuffer[bufferIndex] = statePayload;
        }

        if (foundBufferIndex)
        {
            Manager.BroadcastPacketToClients(new EntityStatePayloadPacket()
            {
                NetId = NetId,
                StatePayload = StateBuffer[bufferIndex],
            });
        }
    }


    [Export]
    private Camera3D _camera;

    private Vector3 _targetedLerpPosition = Vector3.Zero;
}