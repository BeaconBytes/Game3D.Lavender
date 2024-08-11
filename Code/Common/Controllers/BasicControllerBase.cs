using System;
using System.Collections.Generic;
using Godot;
using Lavender.Client.Managers;
using Lavender.Common.Entity;
using Lavender.Common.Entity.Data;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Items;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Controller;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;
using Lavender.Common.Utils;

namespace Lavender.Common.Controllers;

public partial class BasicControllerBase : Node, IController
{
    public virtual void Setup(uint netId, GameManager gameManager)
    {
        NetId = netId;
        Manager = gameManager;
        MapManager = Manager.MapManager;
        IsClient = Manager is ClientManager;

        AttachReceiverEvent += OnReceiverAttached;
        DetachReceiverEvent += OnReceiverDetached;

        Register.Packets.Subscribe<EntityStatePayloadPacket>(OnStatePayloadPacket);
        if (IsClient)
        {
            Register.Packets.Subscribe<SetControllingPacket>(OnSetControllingPacket);
            Register.Packets.Subscribe<EntityMoveToPacket>(OnEntityMoveToPacket);
        }
        else
        {
            Register.Packets.Subscribe<EntityInputPayloadPacket>(OnInputPayloadPacket);
        }

        SetDisplayName($"{Register.Controllers.GetControllerType(this).ToString()}");
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        
        AttachReceiverEvent -= OnReceiverAttached;
        DetachReceiverEvent -= OnReceiverDetached;
    }

    
    public virtual void NetworkProcess(double delta)
    {
        if (NetId == (uint)StaticNetId.Null && Manager.CurrentTick % (GameManager.SERVER_TICK_RATE * 10f) == 0)
            GD.PrintErr("NetId of Entity is NULL!");
        
        uint bufferIndex = 0;
        
        if (ReceiverEntity is null)
            return;
        
        if (IsClient)
        {
            if (Manager.ClientController.NetId == NetId)
            {
                if (Manager.IsClient)
                {
                    if (ReceiverEntity is not LivingEntityBase livingEntity)
                        return;
                    
                    if (!LatestServerState.Equals(default(StatePayload)) && (LastProcessedState.Equals(default(StatePayload)) || !LatestServerState.Equals(LastProcessedState)))
                    {
                        HandleServerReconciliation();
                    }
                    
                    bufferIndex = Manager.CurrentTick % GameManager.NET_BUFFER_SIZE;

                    Vector3 realMoveDirection = MoveInput.Rotated( Vector3.Up, ReceiverEntity.GetGlobalTransform().Basis.GetEuler( ).Y ).Normalized( );
                    
                    InputPayload inputPayload = new()
                    {
                        tick  = Manager.CurrentTick,
                        moveInput = realMoveDirection,
                        lookInput = LookInput,
                        flagsInput = MoveFlagsInput,
                    };
                    LookInput = Vector3.Zero;
                    
                    InputBuffer[bufferIndex] = inputPayload;
                    StateBuffer[bufferIndex] = livingEntity.ProcessMovement(inputPayload);
	        
                    Manager.SendPacketToServer(new EntityInputPayloadPacket()
                    {
                        NetId = NetId,
                        InputPayload = inputPayload,
                    });
                    
                }
                return;
            }
            
            // We're client-side, but aren't the entity being controlled by OUR client
            LastProcessedState = LatestServerState;
            _targetedLerpPosition = LatestServerState.position;
            _targetedLerpRotation = LatestServerState.rotation;

            ReceiverEntity.WorldPosition = ReceiverEntity.WorldPosition.Lerp(_targetedLerpPosition, ReceiverEntity.Stats.FullMoveSpeed * (float)delta);
            ReceiverEntity.SyncRotationTo(_targetedLerpRotation);
            
            return;
        }
            
        if (ReceiverEntity is not LivingEntityBase livingServerEntity)
            return;
        
        bool foundBufferIndex = ( InputQueue.Count > 0 );
        
        while (InputQueue.Count > 0)
        {
            InputPayload inputPayload = InputQueue.Dequeue();
            bufferIndex = inputPayload.tick % GameManager.NET_BUFFER_SIZE;

            StatePayload statePayload = livingServerEntity.ProcessMovement(inputPayload);
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
    protected void HandleServerReconciliation()
    {
        if (ReceiverEntity is not LivingEntityBase livingEntity)
            return;
        
        LastProcessedState = LatestServerState;

        uint serverStateBufferIndex = LatestServerState.tick % GameManager.NET_BUFFER_SIZE;
        float posError = LatestServerState.position.DistanceTo(StateBuffer[serverStateBufferIndex].position);

        if (posError > 0.005f)
        {
            // Rewind
            ReceiverEntity.WorldPosition = LatestServerState.position;
            livingEntity.ReconciliationRotateTo(LatestServerState.rotation);
			
            // Update buffer at index of latest server state
            StateBuffer[serverStateBufferIndex] = LatestServerState;
			
            // Re-simulate the rest of the ticks up to the current tick client-side
            uint tickToProcess = LatestServerState.tick + 1;

            while (tickToProcess < Manager.CurrentTick)
            {
                uint bufferIndex = tickToProcess % GameManager.NET_BUFFER_SIZE;
				
                // Process the new movement with reconciled state
                StatePayload statePayload = livingEntity.ProcessMovement(InputBuffer[bufferIndex]);
				
                // Update buffer with recalculated state
                StateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }
        }
    }

    
    /// <summary>
    /// Destroy this controller without emitting DestroyedEvent
    /// </summary>
    public void SilentDestroy()
    {
        if (Destroyed)
            throw new Exception($"BasicController.SilentDestroy() called more than once on the same controller!");
        
        Destroyed = true;
    }

    /// <summary>
    /// Destroys this controller and emits DestroyedEvent
    /// </summary>
    public void Destroy()
    {
        if (Destroyed)
            throw new Exception($"BasicController.Destroy() called more than once on the same controller!");
        
        SilentDestroy();
        DestroyedEvent?.Invoke(this);
    }

    
    /// <summary>
    /// Sets which Entity this Controller is controlling
    /// </summary>
    public void SetControlling(IGameEntity gameEntity)
    {
            GD.Print($"[{(IsClient ? "CLIENT" : "SERVER")}] SetControlling on controller[{NetId}] to entity[{gameEntity?.NetId}] called!");
        if (ReceiverEntity == gameEntity)
            return;
        
        if (ReceiverEntity != null)
        {
            ReceiverEntity = null;
            DetachReceiverEvent?.Invoke(this, ReceiverEntity);
        }
        ReceiverEntity = gameEntity;
        if (gameEntity == null)
            return;

        gameEntity.AddController(this);

        AttachReceiverEvent?.Invoke(this, ReceiverEntity);
    }

    /// <summary>
    /// (re)spawn the receiver this controller is attached to.
    /// </summary>
    public virtual void RespawnReceiver() { }

    // EVENT HANDLERS //
    
    private void OnEntityMoveToPacket(EntityMoveToPacket packet, uint sourceNetId)
    {
        if (!IsClient || packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        _targetedLerpPosition = packet.Position;
        if (packet.Rotation.HasValue)
            _targetedLerpRotation = packet.Rotation.Value;
    }
    private void OnSetControllingPacket(SetControllingPacket packet, uint sourceNetId)
    {
        if (packet.ControllerNetId != NetId)
            return;

        if (!packet.ReceiverNetId.HasValue)
        {
            SetControlling(null);
            return;
        }
        SetControlling(Manager.GetEntityFromNetId(packet.ReceiverNetId.Value));
    }
    private void OnReceiverAttached(INetNode source, INetNode target)
    {
        if (source != this || target is not IGameEntity targetGameEntity)
            return;
        
        
        targetGameEntity.RecalculateVisibility();
        if (!IsClient)
        {
            Manager.BroadcastPacketToClientsOrdered(new SetControllingPacket()
            {
                ControllerNetId = NetId,
                ReceiverNetId = target.NetId,
            });
        }
        else
        {
            if (ReceiverEntity is BasicEntityBase basicEntity)
            {
                basicEntity.TeleportedEvent += OnReceiverTeleported;
            }
        }
    }

    private void OnReceiverDetached(INetNode source, INetNode target)
    {
        if (source != this || target is not IGameEntity targetGameEntity) 
            return;
        
        targetGameEntity.RecalculateVisibility();
        if (!IsClient)
        {
            Manager.BroadcastPacketToClientsOrdered(new SetControllingPacket()
            {
                ControllerNetId = NetId,
                ReceiverNetId = target.NetId,
            });
        }
        else
        {
            if (ReceiverEntity is BasicEntityBase basicEntity)
            {
                basicEntity.TeleportedEvent -= OnReceiverTeleported;
            }
        }
    }
    
    /// <summary>
    /// Sets the display name(includeing node-name and network name) to the given string.
    /// </summary>
    public virtual void SetDisplayName(string name)
    {
        string sanitizedName = StringUtils.Sanitize(name, 24, false);
        DisplayName = sanitizedName;
        Name = $"Plr:{sanitizedName}[#{NetId}]";
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
    private void OnReceiverTeleported(IGameEntity sourceEntity)
    {
        _targetedLerpPosition = ReceiverEntity.WorldPosition;
    }
    
    public uint NetId { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsSetup => (Manager != null);
    public bool IsClient { get; private set; }
    public GameManager Manager { get; private set; }
    public MapManager MapManager { get; private set; }

    public IGameEntity ReceiverEntity { get; protected set; }

    public bool Destroyed { get; private set; }
    
    
    // Inputs
    public Vector3 LookInput { get; protected set; }
    public Vector3 MoveInput { get; protected set; }
    public EntityMoveFlags MoveFlagsInput { get; protected set; }

    // Network Syncing //
    private Vector3 _lastSyncedPosition = Vector3.Zero;
    private Vector3 _lastSyncedRotation = Vector3.Zero;
    
    private Vector3 _targetedLerpPosition = Vector3.Zero;
    private Vector3 _targetedLerpRotation = Vector3.Zero;
    
    protected readonly Queue<InputPayload> InputQueue = new();
    
    protected StatePayload LatestServerState;
    protected StatePayload LastProcessedState;
    
    protected readonly StatePayload[] StateBuffer = new StatePayload[GameManager.NET_BUFFER_SIZE];
    protected readonly InputPayload[] InputBuffer= new InputPayload[GameManager.NET_BUFFER_SIZE];
    
    
    
    // EVENTS //
    public event GameManager.SimpleNetNodeEventHandler DestroyedEvent;
    public event GameManager.SourcedNetNodeEventHandler DetachReceiverEvent;
    public event GameManager.SourcedNetNodeEventHandler AttachReceiverEvent;
}