using System;
using Godot;
using Lavender.Client.Managers;
using Lavender.Common.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Controller;
using Lavender.Common.Registers;

namespace Lavender.Common.Controllers;

public partial class BasicController : Node, IController
{
    public virtual void Setup(uint netId, GameManager gameManager)
    {
        NetId = netId;
        Manager = gameManager;
        MapManager = Manager.MapManager;
        IsClient = Manager is ClientManager;

        AttachReceiverEvent += OnReceiverAttached;
        DetachReceiverEvent += OnReceiverDetached;

        if (IsClient)
        {
            Register.Packets.Subscribe<SetControllingPacket>(OnSetControllingPacket);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        AttachReceiverEvent -= OnReceiverAttached;
        DetachReceiverEvent -= OnReceiverDetached;
    }


    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (!IsSetup) 
            return;
        
        _deltaTimer += delta;

        while (_deltaTimer >= GameManager.NET_TICK_TIME)
        {
            _deltaTimer -= GameManager.NET_TICK_TIME;
            NetworkProcess(GameManager.NET_TICK_TIME);
            CurrentTick++;
        }
    }
    /// <summary>
    /// Called every network tick
    /// </summary>
    protected virtual void NetworkProcess(double delta)
    {
        if (NetId == (uint)StaticNetId.Null && CurrentTick % (GameManager.SERVER_TICK_RATE * 10f) == 0)
            GD.PrintErr("NetId of Entity is NULL!");
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
        if (ReceiverEntity == gameEntity)
            return;
        
        if (ReceiverEntity != null)
        {
            DetachReceiverEvent?.Invoke(this, ReceiverEntity);
        }
        ReceiverEntity = gameEntity;
        if (gameEntity == null)
            return;

        AttachReceiverEvent?.Invoke(this, ReceiverEntity);
    }

    public virtual void RespawnReceiver() { }

    // EVENT HANDLERS //
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
                ReceiverNetId = target.NetId == (uint)StaticNetId.Null ? null : target.NetId,
            });
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
                ReceiverNetId = target.NetId == (uint)StaticNetId.Null ? null : target.NetId,
            });
        }
    }

    public uint NetId { get; private set; }
    public bool IsSetup => (Manager != null);
    public bool IsClient { get; private set; }
    public GameManager Manager { get; private set; }
    public MapManager MapManager { get; private set; }

    public IGameEntity ReceiverEntity { get; protected set; }
    protected uint CurrentTick { get; private set; }
    
    private double _deltaTimer = 0;

    public bool Destroyed { get; private set; }
    
    // EVENTS //
    public event GameManager.SimpleNetNodeEventHandler DestroyedEvent;
    public event GameManager.SourcedNetNodeEventHandler DetachReceiverEvent;
    public event GameManager.SourcedNetNodeEventHandler AttachReceiverEvent;
}