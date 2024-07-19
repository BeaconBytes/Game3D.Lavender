using System;
using System.Collections.Generic;
using Godot;
using Lavender.Client.Managers;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.Buffs;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Exceptions;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;
using Lavender.Common.Utils;

namespace Lavender.Common.Entity.GameEntities;

public partial class BasicEntityBase : CharacterBody3D, IGameEntity
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

        SetDisplayName("");

        if (IsClient)
        {
            Register.Packets.Subscribe<EntitySetMasterControllerPacket>(OnSetMasterControllerPacket);
        }
    }


    public virtual void RecalculateVisibility()
    {
        if (ServerHiddenNodes == null)
        {
            throw new Exception("PostSetup#ServerHiddenNodes() is null on this entity!");
        }
        
        IGameEntity receiverEntity = Manager.ClientController?.ReceiverEntity;

        // Return if we're Client-Side and the ClientController OR the ClientController's
        // ReceiverEntity isn't set yet.
        if (receiverEntity is null && IsClient)
            return;
        
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


    /// <summary>
    /// Enable or Disable collision entirely and, when enabling, do so depending on client/server sidedness
    /// </summary>
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
    
    public virtual void HandleControllerInputs(IController source, InputPayload input)
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
            Manager.BroadcastPacketToClientsOrdered(new EntityTeleportPacket()
            {
                NetId = NetId,
                Position = GlobalPosition,
                Rotation = GlobalRotation,
            });
        }

        TeleportedEvent?.Invoke(this);
    }

    public virtual void NetworkProcess(double delta)
    {
        if (NetId == (uint)StaticNetId.Null && Manager.CurrentTick % (GameManager.SERVER_TICK_RATE * 10f) == 0)
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

    public Transform3D GetGlobalTransform()
    {
        return GlobalTransform;
    }

    public virtual IController GetMasterController()
    {
        if (AttachedControllers.Count == 0)
            return null;
        return AttachedControllers[0];
    }
    public virtual void SetMasterController(IController controller)
    {
        if (controller is null)
            throw new Exception("Invalid Master Controller given: null");
        
        GD.Print($"[{(IsClient ? "CLIENT" : "SERVER")}] Set MasterController on Entity[{NetId}] to Controller[{controller.NetId}]");

        if (!AttachedControllers.Contains(controller))
        {
            throw new UnknownNetIdException("Controller given not added to AttachedControllers");
        }

        if (!IsClient)
        {
            Manager.BroadcastPacketToClientsOrdered(new EntitySetMasterControllerPacket()
            {
                TargetEntityNetId = NetId,
                MasterControllerNetId = controller.NetId,
            });
        }
    }
    public virtual void AddController(IController controller, bool insertFirst = false)
    {
        if (AttachedControllers.Contains(controller))
            throw new Exception("Tried to add same controller multiple times to a BasicEntity!");
        
        GD.Print($"AddController[{(IsClient ? "CLIENT" : "SERVER")}] Controller[{controller.NetId}] to Entity[{NetId}]");
        
        if (insertFirst)
        {
            AttachedControllers.Insert(0, controller);
            return;
        }
        AttachedControllers.Add(controller);
    }
    public virtual void RemoveController(IController controller)
    {
        if (!AttachedControllers.Contains(controller))
            throw new Exception("Tried to remove a non-existent controller on a BasicEntity!");
        AttachedControllers.Remove(controller);
    }

    public void SetNavTarget(Vector3 pos)
    {
        NavAgent.TargetPosition = pos;
        LookAt(new Vector3(pos.X, GlobalPosition.Y, pos.Z));
        SnapRotationTo(GlobalRotation);
    }

    public virtual List<IGameEntity> RaycastEntityHit()
    {
        if (_raycast3D is null)
            return null;

        if (_raycast3D.IsColliding())
        {
            if (_raycast3D.GetCollider() is IGameEntity gameEntity)
            {
                return new List<IGameEntity>() { gameEntity };
            }
        }

        return null;
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

    protected void TriggerCompletedNavPathEvent()
    {
        OnCompletedNavPathEvent?.Invoke(this);
    }
    
    
    
    // EVENT HANDLERS //
    private void OnSetMasterControllerPacket(EntitySetMasterControllerPacket packet, uint sourceNetId)
    {
        if (packet.TargetEntityNetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        IController controller = Manager.GetControllerFromNetId(packet.MasterControllerNetId);
        if (controller is null)
        {
            throw new UnknownNetIdException();
        }

        SetMasterController(controller);
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

    /// <summary>
    /// Sets the display name(includeing node-name and network name) to the given string.
    /// </summary>
    public virtual void SetDisplayName(string name)
    {
        string sanitizedName = StringUtils.Sanitize(name, 24, false);
        DisplayName = sanitizedName;
        Name = $"{sanitizedName}[#{NetId}]";
    }
    public GameManager Manager { get; private set; } = null;
    public MapManager MapManager { get; private set; } = null;
    public string DisplayName { get; private set; }
    
    public bool IsClient { get; private set; }

    public bool Dead { get; private set; }

    public bool IsSetup => (Manager != null);

    public bool Destroyed { get; private set; } = false;

    public bool Enabled { get; set; } = true;

    public EntityStats Stats { get; protected set; }

    public List<IController> AttachedControllers { get; private set; } = new();
    public List<IEntityBuff> AppliedBuffs { get; } = new();

    public List<IEntityBuff> TickingAppliedBuffs { get; } = new();
    
    
    
    public bool IsControlsFrozen
    {
        get => _isControlsFrozenVal;
        set
        {
            _isControlsFrozenVal = value;
            TriggerValueChangedEvent( EntityValueChangedType.ControlsFrozen);
        }
    }
    private bool _isControlsFrozenVal = true;


    [Export]
    public NavigationAgent3D NavAgent { get; protected set; }
    
    [Export]
    private RayCast3D _raycast3D;
    public RayCast3D ActiveRaycast3D
    {
        get
        {
            return _raycast3D;
        }
    }


    /// <summary>
    /// An array of things we should hide if this entity is set to being controlled by this client(Models, Particles, etc.)
    /// </summary>
    [Export]
    protected Godot.Collections.Array<Node> ServerHiddenNodes;

    // EVENT SIGNATURES //
    public delegate void EntityDestroyEventHandler(IGameEntity sourceEntity);

    public delegate void EntityTeleportedEventHandler(IGameEntity sourceEntity);
    public delegate void EntityValueChangedHandler(BasicEntityBase entity, EntityValueChangedType type);
    
    
    // EVENTS //
    public event EntityDestroyEventHandler DestroyedEvent;
    public event EntityTeleportedEventHandler TeleportedEvent;
    public event EntityValueChangedHandler OnValueChangedEvent;
    public event GameManager.SimpleNetNodeEventHandler OnCompletedNavPathEvent;
}