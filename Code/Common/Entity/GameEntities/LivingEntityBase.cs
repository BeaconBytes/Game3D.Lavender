using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Items;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Data;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity.GameEntities;

public partial class LivingEntityBase : BasicEntityBase
{
    public uint GrabbedById { get; protected set; }

    protected bool EnableAutoMoveSlide = false;

    private bool _lastNavAgentCompleted = true;
    
    
    // EVENT SIGNATURES //
    public delegate void EntitySourceTargetEventHandler(IGameEntity source, IGameEntity target);

    // EVENTS //
    public event EntitySourceTargetEventHandler OnEntityGrabbedByEvent;
    public event EntitySourceTargetEventHandler OnEntityReleasedByEvent;
    
    
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);

        Stats = new EntityStats();
        
        if (IsClient)
        {
            Register.Packets.Subscribe<EntityTeleportPacket>(OnEntityTeleportPacket);
            Register.Packets.Subscribe<EntityRotatePacket>(OnEntityRotatePacket);
            Register.Packets.Subscribe<EntityValueChangedPacket>(OnValueChangedPacket);
            Register.Packets.Subscribe<EntitySetGrabPacket>(OnSetGrabbedPacket);
            
            TeleportedEvent += OnTeleported;
        }
        else
        {
            OnValueChangedEvent += OnValueChanged;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (IsClient)
        {
            TeleportedEvent -= OnTeleported;
        }
        else
        {
            OnValueChangedEvent -= OnValueChanged;
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Destroyed)
            return;

        if (!IsClient)
        {
            if (NavAgent != null && NavAgent.IsNavigationFinished())
            {
                if (!_lastNavAgentCompleted)
                {
                    _lastNavAgentCompleted = true;
                    TriggerCompletedNavPathEvent();
                }
                
                Vector3 newVel = ProcessMovementVelocity(Vector3.Zero, delta: (float)delta);
                OnVelocityComputed(newVel);
            }
            else if (NavAgent != null)
            {
                Vector3 curPos = GlobalPosition;
                
                Vector3 nextPos = NavAgent.GetNextPathPosition();

                EntityMoveFlags moveFlags = EntityMoveFlags.None;

                if (IsControlsFrozen)
                    moveFlags |= EntityMoveFlags.Frozen;

                Vector3 newDirVector = curPos.DirectionTo(nextPos);
                Vector3 newVel = ProcessMovementVelocity(newDirVector, moveFlags: moveFlags, delta: (float)delta);

                if (NavAgent.AvoidanceEnabled)
                    NavAgent.Velocity = newVel;
                else
                    OnVelocityComputed(newVel);
            }
            
        }
    }

    public virtual Vector3 ProcessMovementVelocity(Vector3 moveInput, EntityMoveFlags moveFlags = EntityMoveFlags.None, double delta = GameManager.NET_TICK_TIME)
    {
        Vector3 vel = Velocity;

        double gravityVector = vel.Y;
        
        if (GetMasterController()?.MovementMode is EntityMovementMode.Flight)
        {
            gravityVector = 0f;
        }
        else if(!IsOnFloor())
        {
            gravityVector -= GravityVal * delta;
        }

        bool isSprinting = moveFlags.HasFlag(EntityMoveFlags.Sprint);

        float speed = isSprinting ? (Stats.FullMoveSpeed * Stats.MovementSprintMultiplier) : Stats.FullMoveSpeed;
        
        if (IsOnFloor() || GetMasterController().MovementMode is EntityMovementMode.Flight)
        {
            vel = vel.MoveToward(moveInput * speed, Stats.MovementAcceleration * (float)delta);
        }

        vel.Y = Mathf.Lerp(vel.Y, moveInput.Y * speed, Stats.MovementAcceleration * (float)delta); 
        
        if (GetMasterController().MovementMode is EntityMovementMode.Ground)
            vel.Y = (float)gravityVector;
        
        if (moveFlags.HasFlag(EntityMoveFlags.Jump) && IsOnFloor() && GetMasterController().MovementMode is EntityMovementMode.Ground)
        {
            vel.Y += Stats.MovementJumpImpulse;
        }

        return vel;
    }
    
    
    
    /// <summary>
    /// Process InputPayload and then ApplyMovementChanges().
    /// </summary>
    public virtual StatePayload ProcessMovement(InputPayload input, double delta = GameManager.NET_TICK_TIME)
    {
        Vector3 newVel = ProcessMovementVelocity(input.moveInput, input.flagsInput);
        if (input.flagsInput.HasFlag(EntityMoveFlags.Frozen))
        {
            newVel = Vector3.Zero;
        }
        
        Vector3 lookInput = input.lookInput;
        lookInput *= (float)GameManager.NET_TICK_TIME;
        
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
    protected Tuple<Vector3, Vector3> ApplyMovementChanges(Vector3 newVelocity, Vector3 inputRotate, double delta = GameManager.NET_TICK_TIME)
    {
        Velocity = newVelocity;
        MoveAndSlide(delta);
        
        Vector3 newRot = ApplyMovementRotation(inputRotate);

        return new Tuple<Vector3, Vector3>(GlobalPosition, newRot);
    }

    /// <summary>
    /// Used during reconciliation to "snap" rotation values to the given rot
    /// </summary>
    public virtual void ReconciliationRotateTo(Vector3 rot)
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
        return Rotation;
    }
    
    
    public void TriggerGrabbedBy(LivingEntityBase sourceEntity)
    {
        if (sourceEntity == null)
        {
            GrabbedById = (uint)StaticNetId.Null;
            ChangeCollisionEnabled(true);
            IsControlsFrozen = false;
            return;
        }
        
        ChangeCollisionEnabled(false);
        IsControlsFrozen = true;
        GrabbedById = sourceEntity.NetId;
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        Velocity = safeVelocity;
        MoveAndSlide();
    }

    public void SetNavTarget(Vector3 pos)
    {
        _lastNavAgentCompleted = false;
        NavAgent.TargetPosition = pos;
    }

    public override void ChangeCollisionEnabled(bool isEnabled)
    {
        base.ChangeCollisionEnabled(isEnabled);


        if (IsClient)
        {
            ActiveRaycast3D.SetCollisionMaskValue(1, true);
            ActiveRaycast3D.SetCollisionMaskValue(8, false);
        }
        else
        {
            ActiveRaycast3D.SetCollisionMaskValue(1, false);
            ActiveRaycast3D.SetCollisionMaskValue(8, true);
        }

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
    private void OnValueChangedPacket(EntityValueChangedPacket packet, uint sourceNetId)
    {
        if (packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        if(Manager.IsDebugMode)
            GD.Print($"[OnValueChangedPacked]({packet.ValueType.ToString()}): '{packet.NewValue}'");
        
        if (packet.ValueType is EntityValueChangedType.ControlsFrozen)
        {
            IsControlsFrozen = Mathf.IsEqualApprox(packet.NewValue, 1.0f);
        }
    }
    protected virtual void OnSetGrabbedPacket(EntitySetGrabPacket packet, uint sourceNetId)
    {
        if (packet.TargetNetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        if (Manager.GetEntityFromNetId(packet.SourceNetId) is not LivingEntityBase sourceLivingEntity)
            return;

        GrabbedById = packet.SourceNetId;
        Manager.BroadcastNotification($"{(packet.IsRelease ? "Released" : "Grabbed")} by id {packet.SourceNetId}", 2f);
    }

    private void OnTeleported(IGameEntity sourceEntity)
    {
        Velocity = Vector3.Zero;

        NavAgent?.SetVelocityForced(Vector3.Zero);
    }

    private void OnValueChanged(BasicEntityBase entity, EntityValueChangedType type)
    {
        if (entity.NetId == NetId && !IsClient && entity is LivingEntityBase livingEntity)
        {
            float singleValue;
            if (type is EntityValueChangedType.ControlsFrozen)
            {
                singleValue = (livingEntity.IsControlsFrozen ? 1.0f : -1.0f);
            }
            else
            {
                if(Manager.IsDebugMode)
                    GD.Print($"LivingEntityBase#OnValueChanged(): Unknown EntityValueChangedType of '{type.ToString()}'");
                return;
            }
            
            
            Manager.BroadcastPacketToClientsOrdered(new EntityValueChangedPacket()
            {
                NetId = entity.NetId,
                ValueType = type,
                NewValue = singleValue,
            });
        }
    }
    
}