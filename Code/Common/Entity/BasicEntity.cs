using System;
using System.Collections.Generic;
using Godot;
using Lavender.Client.Managers;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.Buffs;
using Lavender.Common.Entity.Data;
using Lavender.Common.Entity.Variants;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;

namespace Lavender.Common.Entity;

public partial class BasicEntity : CharacterBody3D, IGameEntity
{
    private float _speed = 5.0f;
    private float _jumpVelocity = 4.5f;

    protected float GravityVal = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public virtual void Setup(uint netId, GameManager manager)
    {
        NetId = netId;
        Manager = manager;
        IsClient = Manager is ClientManager;
        
        ChangeCollisionEnabled(true);
        RecalculateVisibility();
        
        MapManager = Manager.MapManager;
    }

    public virtual void RecalculateVisibility()
    {
        if (ServerHiddenNodes == null)
        {
            throw new Exception("PostSetup#ServerHiddenNodes() is null on this entity!");
        }
        
        IGameEntity receiverEntity = Manager.ClientController?.ReceiverEntity;

        if (receiverEntity is null && IsClient)
        {
            //throw new Exception("BasicEntity#PostSetup(): ReceiverEntity is null on this entity!");
            return;
        }
        
        foreach (Node n in ServerHiddenNodes)
        {
            switch (n)
            {
                case Camera3D camera:
                {
                    if (receiverEntity == this)
                    {
                        camera.Visible = true;
                        camera.MakeCurrent();
                    }
                    else
                    {
                        camera.ClearCurrent();
                        camera.Visible = false;
                    }

                    continue;
                }
                case Node3D n3d when IsClient:
                    n3d.Visible = receiverEntity != this;
                    break;
                case Node3D n3d:
                    n3d.Visible = false;
                    break;
            }
        }
    }


    public virtual void ChangeCollisionEnabled(bool isEnabled)
    {
        SetCollisionLayerValue(1, false);
        SetCollisionLayerValue(8, false);

        SetCollisionMaskValue(1, false);
        SetCollisionMaskValue(8, false);

        
        if (IsClient)
        {
            PlatformFloorLayers = 1;
            if (!isEnabled)
                return;
            
            SetCollisionLayerValue(1, true);
            SetCollisionMaskValue(1, true);
        }
        else
        {
            PlatformFloorLayers = 128;
            if (!isEnabled)
                return;
            
            SetCollisionLayerValue(8, true);
            SetCollisionMaskValue(8, true);
        }
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

    public virtual void HandleControllerInputs(RawInputs inputs)
    {
        throw new NotImplementedException();
    }

    public void Teleport(Vector3 position, Vector3? rotation = null)
    {
        GlobalPosition = position;
        if (rotation.HasValue)
            GlobalRotation = rotation.Value;
        
        if (!IsClient)
        {
            Manager.BroadcastPacketToClients(new EntityTeleportPacket()
            {
                NetId = NetId,
                Position = GlobalPosition,
                Rotation = GlobalRotation,
            });
        }

        TeleportedEvent?.Invoke(this);
    }

    protected virtual void NetworkProcess(double delta)
    {
        if (NetId == (uint)StaticNetId.Null && CurrentTick % (GameManager.SERVER_TICK_RATE * 10f) == 0)
            GD.PrintErr("NetId of Entity is NULL!");
    }

    public uint NetId { get; private set; } = (uint)StaticNetId.Null;

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

    
    public virtual void AddController(IController controller, bool insertFirst = false)
    {
        if (ActiveControllers.Contains(controller))
            throw new Exception("Tried to add same controller multiple times to a BasicEntity!");
        
        if (insertFirst)
        {
            ActiveControllers.Insert(0, controller);
            return;
        }
        ActiveControllers.Add(controller);
    }
    public virtual void RemoveController(IController controller)
    {
        if (!ActiveControllers.Contains(controller))
            throw new Exception("Tried to remove a non-existent controller on a BasicEntity!");
        ActiveControllers.Remove(controller);
    }

    /// <summary>
    /// Destroy this entity without emitting DestroyedEvent
    /// </summary>
    public void SilentDestroy()
    {
        if (Destroyed)
            throw new Exception($"Entity.SilentDestroy() called more than once on the same entity!");
        
        Dead = true;
        Destroyed = true;
        Enabled = false;
    }

    /// <summary>
    /// Destroys this entity and emits DestroyedEvent
    /// </summary>
    public void Destroy()
    {
        if (Destroyed)
            throw new Exception($"Entity.Destroy() called more than once on the same entity!");
        
        SilentDestroy();
        DestroyedEvent?.Invoke(this);
    }

    private void TriggerValueChangedEvent(EntityValueChangedType type)
    {
        OnValueChangedEvent?.Invoke(this, type);
    }

    public Vector3 WorldPosition
    {
        get => GlobalPosition;
        set => GlobalPosition = value;
    }
    public Vector3 WorldRotation
    {
        get => GlobalRotation;
        set => GlobalRotation = value;
    }
    
    public virtual void SetName(string name)
    {
        DisplayName = name;
    }

    private double _deltaTimer = 0;
    protected uint CurrentTick { get; set; } = 0;
    public GameManager Manager { get; private set; } = null;
    public MapManager MapManager { get; private set; } = null;
    public string DisplayName { get; private set; }
    
    public bool IsClient { get; private set; }

    public bool Dead { get; private set; }

    public bool IsSetup => (Manager != null);

    public bool Destroyed { get; private set; } = false;

    public bool Enabled { get; set; } = true;

    public EntityStats Stats { get; protected set; }

    public bool AutomaticMoveAndSlide { get; protected set; }

    public List<IController> ActiveControllers { get; } = new();
    public List<IEntityBuff> AppliedBuffs { get; } = new();

    public List<IEntityBuff> TickingAppliedBuffs { get; } = new();
    
    
    
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
    
    
    
    /// <summary>
    /// A array of things we should hide if this entity is set to being controlled by this client(Models, Particles, etc.)
    /// </summary>
    [Export]
    protected Godot.Collections.Array<Node> ServerHiddenNodes;

    // EVENT SIGNATURES //
    public delegate void EntityDestroyEventHandler(IGameEntity sourceEntity);

    public delegate void EntityTeleportedEventHandler(IGameEntity sourceEntity);
    public delegate void EntityValueChangedHandler(BasicEntity entity, EntityValueChangedType type);
    
    
    // EVENTS //
    public event EntityDestroyEventHandler DestroyedEvent;
    public event EntityTeleportedEventHandler TeleportedEvent;
    public event EntityValueChangedHandler OnValueChangedEvent;
}