using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Data;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity;

public partial class LivingEntity : BasicEntity
{
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
        
        Stats = new EntityStats();
        
        Register.Packets.Subscribe<EntityStatePayloadPacket>(OnStatePayloadPacket);
        if (IsClient)
        {
            Register.Packets.Subscribe<EntityTeleportPacket>(OnEntityTeleportPacket);
            Register.Packets.Subscribe<EntityRotatePacket>(OnEntityRotatePacket);
            Register.Packets.Subscribe<EntityValueChangedPacket>(OnValueChangedPacket);
        }
        else
        {
            Register.Packets.Subscribe<EntityInputPayloadPacket>(OnInputPayloadPacket);
            OnValueChangedEvent += OnValueChanged;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (!IsClient)
        {
            OnValueChangedEvent -= OnValueChanged;
        }
    }

    protected virtual Vector3 ProcessMovementVelocity(Vector3 moveInput, EntityMoveFlags moveFlags = EntityMoveFlags.None, float deltaTime = GameManager.NET_TICK_TIME)
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
    /// Process InputPayload and then ApplyMovementChanges().
    /// </summary>
    protected virtual StatePayload ProcessMovement(InputPayload input)
    {
        Vector3 newVel = ProcessMovementVelocity(input.moveInput, input.flagsInput);
        if (input.flagsInput.HasFlag(EntityMoveFlags.Frozen))
        {
            newVel = Vector3.Zero;
        }
        
        Vector3 lookInput = input.lookInput;
        lookInput *= GameManager.NET_TICK_TIME;
        
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
        MoveAndSlide(GameManager.NET_TICK_TIME);
        
        Vector3 newRot = ApplyMovementRotation(inputRotate);

        return new Tuple<Vector3, Vector3>(GlobalPosition, newRot);
    }
    protected void HandleServerReconciliation()
    {
        LastProcessedState = LatestServerState;

        uint serverStateBufferIndex = LatestServerState.tick % GameManager.NET_BUFFER_SIZE;
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
                uint bufferIndex = tickToProcess % GameManager.NET_BUFFER_SIZE;
				
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


    protected float GetMoveSpeed()
    {
        return (Stats.MovementSpeedBase * Stats.MovementSpeedMultiplier);
    }
    
    // EVENT HANDLERS //
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
    private void OnValueChangedPacket(EntityValueChangedPacket packet, uint sourceNetId)
    {
        if (packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        if (packet.ValueType is EntityValueChangedType.ControlsFrozen)
        {
            IsControlsFrozen = Mathf.IsEqualApprox(packet.NewValue, 1.0f);
        }
    }
    
    private void OnValueChanged(BasicEntity entity, EntityValueChangedType type)
    {
        if (entity.NetId == NetId && !IsClient && entity is LivingEntity livingEntity)
        {
            float singleValue;
            if (type is EntityValueChangedType.ControlsFrozen)
            {
                singleValue = (livingEntity.IsControlsFrozen ? 1.0f : -1.0f);
            }
            else
                return;
            
            
            Manager.BroadcastPacketToClients(new EntityValueChangedPacket()
            {
                NetId = entity.NetId,
                ValueType = type,
                NewValue = singleValue,
            });
            GD.PrintErr($"OnValueChanged sent packet!");
        }
    }
    
    

    protected EntityStats Stats;


    protected bool EnableAutoMoveSlide = false;

    public bool IsControlsFrozen
    {
        get
        {
            return _isControlsFrozenVal;
        }
        set
        {
            _isControlsFrozenVal = value;
            TriggerValueChangedEvent( EntityValueChangedType.ControlsFrozen);
        }
    }
    private bool _isControlsFrozenVal = true;


    // Network Syncing //
    protected readonly Queue<InputPayload> InputQueue = new();
    
    protected StatePayload LatestServerState;
    protected StatePayload LastProcessedState;
    
    protected readonly StatePayload[] StateBuffer = new StatePayload[GameManager.NET_BUFFER_SIZE];
    protected readonly InputPayload[] InputBuffer= new InputPayload[GameManager.NET_BUFFER_SIZE];
    
    
    
    // EVENT HANDLERS //
    
    
    // EVENTS //
}