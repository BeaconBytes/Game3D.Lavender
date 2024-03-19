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
    private float _deltaTimer = 0;
    public uint CurrentTick { get; protected set; }
    protected float MinTimeBetweenTicks { get; private set; } = 1f / EnvManager.SERVER_TICK_RATE;

    protected const uint BUFFER_SIZE = 512;

    public override void _Ready()
    {
        base._Ready();
        Stats = new EntityStats();
        
        Register.Packets.Subscribe<EntityStatePayloadPacket>(OnStatePayloadPacket);
        if (Manager.IsClient)
        {
            Register.Packets.Subscribe<EntityTeleportPacket>(OnEntityTeleportPacket);
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
        
        _deltaTimer += (float)delta;

        while (_deltaTimer >= MinTimeBetweenTicks)
        {
            _deltaTimer -= MinTimeBetweenTicks;
            HandleTick();
            CurrentTick++;
        }
    }
    
    /// <summary>
    /// Process InputPayload and then ApplyMovementChanges(). input's with moveInput.Y==0 will have gravity applied.
    /// </summary>
    protected virtual StatePayload ProcessMovement(InputPayload input)
    {
        Vector3 vel = Velocity;

        if (input.moveInput.Y == 0)
        {
            vel.Y -= GravityVal * MinTimeBetweenTicks;
        
            if (vel.Y < 0)
            {
                vel.Y -= GravityVal * (Stats.MovementFallMultiplier - 1f) * MinTimeBetweenTicks;
            }
            else if (vel.Y > 0)
            {
                vel.Y -= GravityVal * (Stats.MovementFallMultiplier - 1f) * MinTimeBetweenTicks;
            }
        }

        float drag = (Stats.MovementSpeedBase * Stats.MovementSpeedMultiplier);
        float speed = (drag * Stats.MovementSprintMultiplier);

        Vector3 cleanDirection = input.moveInput;
        Vector3 realDirection = cleanDirection.Rotated( Vector3.Up, GlobalTransform.Basis.GetEuler( ).Y ).Normalized( );
        

        float gravityBuffer = vel.Y;
        if (realDirection != Vector3.Zero)
        {
            vel = vel.MoveToward(speed * realDirection, Stats.MovementAcceleration * MinTimeBetweenTicks);
        }
        else
        {
            vel = vel.MoveToward(Vector3.Zero, Stats.MovementAcceleration * MinTimeBetweenTicks);
        }

        // If we arnt flying/swimming, override our velocity's Y to the previously set gravityBuffer
        if(input.moveInput.Y == 0)
            vel.Y = gravityBuffer;


        if (input.flagsInput.HasFlag(EntityMoveFlags.Jump) && IsOnFloor())
        {
            vel.Y += Stats.MovementJumpImpulse;
        }


        Vector3 lookInput = new Vector3(input.lookInput.X, input.lookInput.Y, input.lookInput.Z);
        lookInput *= MinTimeBetweenTicks;

        Tuple<Vector3, Vector3> movementResult = ApplyMovementChanges(vel, lookInput);
        
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
        MoveAndSlide();

        Vector3 newRot=ApplyMovementRotation(inputRotate);

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

    protected virtual void HandleTick()
    {
        if (NetId == (uint)StaticNetId.Null && CurrentTick % (EnvManager.SERVER_TICK_RATE * 5f) == 0)
            GD.PrintErr("NetId of Entity is NULL!");
    }
    
    // PACKET EVENT HANDLERS //
    private void OnEntityTeleportPacket(EntityTeleportPacket packet, uint sourceNetId)
    {
        if (packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;
        Teleport(packet.Position);
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
    
    
    // Network Syncing //
    protected readonly Queue<InputPayload> InputQueue = new();
    
    protected StatePayload LatestServerState;
    protected StatePayload LastProcessedState;
    
    protected readonly StatePayload[] StateBuffer = new StatePayload[BUFFER_SIZE];
    protected readonly InputPayload[] InputBuffer= new InputPayload[BUFFER_SIZE];
}