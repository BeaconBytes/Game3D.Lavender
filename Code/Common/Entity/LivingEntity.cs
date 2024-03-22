using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity;

public partial class LivingEntity : BasicEntity, IControllableEntity
{
    private double _deltaTimer = 0;
    public uint CurrentTick { get; protected set; } = 0;
    protected const float NET_TICK_TIME = 1f / EnvManager.SERVER_TICK_RATE;

    protected const uint BUFFER_SIZE = 1024;

    public override void _Ready()
    {
        base._Ready();
        Stats = new EntityStats();
        
        Register.Packets.Subscribe<EntityStatePayloadPacket>(OnStatePayloadPacket);
        if (IsClient)
        {
            Register.Packets.Subscribe<EntityTeleportPacket>(OnEntityTeleportPacket);
            Register.Packets.Subscribe<EntityRotatePacket>(OnEntityRotatePacket);
        }
        else
        {
            Register.Packets.Subscribe<EntityInputPayloadPacket>(OnInputPayloadPacket);
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        if (!IsSetup) 
            return;
        
        _deltaTimer += delta;

        while (_deltaTimer >= NET_TICK_TIME)
        {
            _deltaTimer -= NET_TICK_TIME;
            HandleTick();
            CurrentTick++;
        }
    }

    protected virtual Vector3 ProcessMovementVelocity(Vector3 moveInput, EntityMoveFlags moveFlags = EntityMoveFlags.None, float deltaTime = NET_TICK_TIME)
    {
        Vector3 vel = Velocity;

        float gravityVector = vel.Y;
        
        if (IsOnFloor())
        {
            gravityVector = 0f;
        }
        else
        {
            gravityVector -= GravityVal * deltaTime;
        }

        float speed = GetMoveSpeed();
        
        if (IsOnFloor())
        {
            vel = vel.MoveToward(moveInput * speed, Stats.MovementAcceleration * deltaTime);
        }

        vel.Y = gravityVector;
        
        if (moveFlags.HasFlag(EntityMoveFlags.Jump) && IsOnFloor())
        {
            vel.Y += Stats.MovementJumpImpulse;
        }

        return vel;
    }
    
    /// <summary>
    /// Process InputPayload and then ApplyMovementChanges(). input's with moveInput.Y==0 will have gravity applied.
    /// </summary>
    protected virtual StatePayload ProcessMovement(InputPayload input)
    {
        Vector3 newVel = ProcessMovementVelocity(input.moveInput, input.flagsInput);

        Vector3 lookInput = input.lookInput;
        lookInput *= NET_TICK_TIME;

        Tuple<Vector3, Vector3> movementResult = ApplyMovementChanges(newVel, lookInput);
        
        return new StatePayload()
        {
            tick = input.tick,
            position = movementResult.Item1,
            rotation = movementResult.Item2,
        };
    }
    /// <summary>
    /// Applies given movement inputs to the current entity and returns a Tuple containing the new GlobalPosition and GlobalRotation of this entity
    /// </summary>
    protected Tuple<Vector3, Vector3> ApplyMovementChanges(Vector3 newVelocity, Vector3 inputRotate)
    {
        Velocity = newVelocity;
        MoveAndSlide(NET_TICK_TIME);
        
        Vector3 newRot = ApplyMovementRotation(inputRotate);

        return new Tuple<Vector3, Vector3>(GlobalPosition, newRot);
    }
    protected void HandleServerReconciliation()
    {
        LastProcessedState = LatestServerState;

        uint serverStateBufferIndex = LatestServerState.tick % BUFFER_SIZE;
        float posError = LatestServerState.position.DistanceTo(StateBuffer[serverStateBufferIndex].position);

        if (posError > 0.005f)
        {
            // Rewind
            GlobalPosition = LatestServerState.position;
            ReconciliationRotateTo(LatestServerState.rotation);
			
            // Update buffer at index of latest server state
            StateBuffer[serverStateBufferIndex] = LatestServerState;
			
            // Re-simulate the rest of the ticks up to the current tick client-side
            uint tickToProcess = LatestServerState.tick + 1;

            while (tickToProcess < CurrentTick)
            {
                uint bufferIndex = tickToProcess % BUFFER_SIZE;
				
                // Process the new movement with reconciled state
                StatePayload statePayload = ProcessMovement(InputBuffer[bufferIndex]);
				
                // Update buffer with recalculated state
                StateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }
        }
    }

    /// <summary>
    /// Used during reconciliation to "snap" rotation values to the given rot
    /// </summary>
    protected virtual void ReconciliationRotateTo(Vector3 rot)
    {
        GlobalRotation = rot;
    }

    /// <summary>
    /// Applies given input-rotation to this entity and returns the new global-rotation values
    /// </summary>
    protected virtual Vector3 ApplyMovementRotation(Vector3 inputRotate)
    {
        RotateX(inputRotate.X);
        RotateY(inputRotate.Y);
        RotateZ(inputRotate.Z);
        return GlobalRotation;
    }
    
    
    public void SnapRotationTo(Vector3 rotation)
    {
        GlobalRotation = rotation;
        if (!IsClient)
        {
            Manager.BroadcastPacketToClients(new EntityRotatePacket()
            {
                NetId = NetId,
                Rotation = GlobalRotation,
            });
        }
    }

    protected virtual void HandleTick()
    {
        if (NetId == (uint)StaticNetId.Null && CurrentTick % (EnvManager.SERVER_TICK_RATE * 5f) == 0)
            GD.PrintErr("NetId of Entity is NULL!");
    }

    protected float GetMoveSpeed()
    {
        return (Stats.MovementSpeedBase * Stats.MovementSpeedMultiplier);
    }
    
    // PACKET EVENT HANDLERS //
    private void OnEntityTeleportPacket(EntityTeleportPacket packet, uint sourceNetId)
    {
        if (packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;
        Teleport(packet.Position);
    }
    private void OnEntityRotatePacket(EntityRotatePacket packet, uint sourceNetId)
    {
        if (packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        GlobalRotation = packet.Rotation;
    }
    private void OnInputPayloadPacket(EntityInputPayloadPacket packet, uint sourceNetId)
    {
        if(packet.NetId == NetId && sourceNetId == NetId)
            InputQueue.Enqueue(packet.InputPayload);
    }
    private void OnStatePayloadPacket(EntityStatePayloadPacket packet, uint sourceNetId)
    {
        if(packet.NetId == NetId && sourceNetId == (uint)StaticNetId.Server)
            LatestServerState = packet.StatePayload;
    }
    
    

    protected EntityStats Stats;

    public virtual void SetControllerParent(uint netId)
    {
        ControllerParentNetId = netId;
    }

    public uint ControllerParentNetId { get; private set; }


    protected bool EnableAutoMoveSlide = false;
    
    
    // Network Syncing //
    protected readonly Queue<InputPayload> InputQueue = new();
    
    protected StatePayload LatestServerState;
    protected StatePayload LastProcessedState;
    
    protected readonly StatePayload[] StateBuffer = new StatePayload[BUFFER_SIZE];
    protected readonly InputPayload[] InputBuffer= new InputPayload[BUFFER_SIZE];
}