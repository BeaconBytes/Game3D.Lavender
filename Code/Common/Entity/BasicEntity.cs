using System;
using Godot;
using Lavender.Client.Managers;
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

        SetCollisionLayerValue(1, false);
        SetCollisionLayerValue(8, false);

        SetCollisionMaskValue(1, false);
        SetCollisionMaskValue(8, false);
        
        if (Manager is ClientManager)
        {
            IsClient = true;
            SetCollisionLayerValue(1, true);
            SetCollisionMaskValue(1, true);
            //PlatformFloorLayers = 1;
        }
        else
        {
            IsClient = false;
            SetCollisionLayerValue(8, true);
            SetCollisionMaskValue(8, true);
            //PlatformFloorLayers = 128;
        }

        MapManager = Manager.MapManager;

        if (ServerHiddenNodes == null)
        {
            GD.PrintErr($"ServerHiddenNodes is null on this entity!");    
            return;
        }
        foreach (Node n in ServerHiddenNodes)
        {

            if (n is Camera3D cam)
            {
                if (NetId == Manager.ClientNetId)
                {
                    cam.MakeCurrent();
                }
                else
                {
                    cam.ClearCurrent();
                }
            }

            if (n is Node3D ntd)
            {
                if (Manager.IsClient)
                {
                    if (NetId == Manager.ClientNetId)
                    {
                        ntd.Visible = false;
                    }
                    else
                    {
                        ntd.Visible = true;
                    }
                }
                else
                {
                    ntd.Visible = false;
                }
            }

            if(n is Control con)
            {
                if (NetId == manager.ClientNetId)
                {
                    con.Visible = true;
                }
                else
                {
                    con.Visible = false;
                }
            }

        }
    }

    public GameManager Manager { get; private set; } = null;
    public MapManager MapManager { get; private set; } = null;

    public uint NetId { get; private set; } = (uint)StaticNetId.Null;
    public void Teleport(Vector3 position)
    {
        GlobalPosition = position;
        if (!Manager.IsClient)
        {
            Manager.BroadcastPacketToClients(new EntityTeleportPacket()
            {
                NetId = NetId,
                Position = position,
            });
        }
        else
        {
            Velocity = new Vector3(0, 0, 0);
        }
    }

    public void RotateTo(Vector3 rotation)
    {
        GlobalRotation = rotation;
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

    public Vector3 WorldPosition => GlobalPosition;

    public Vector3 WorldRotation => GlobalRotation;
    public virtual void SetName(string name)
    {
        DisplayName = name;
    }

    public string DisplayName { get; private set; }
    
    public bool IsClient { get; private set; }

    public bool Dead { get; private set; }

    public bool IsSetup => (Manager != null);

    public bool Destroyed { get; private set; } = false;

    // EVENT HANDLERS //
    public delegate void EntityDestroyEventHandler(IGameEntity sourceEntity);

    /// <summary>
    /// A array of things we should hide if this entity is set to being controlled by this client(Models, Particles, etc.)
    /// </summary>
    [Export]
    protected Godot.Collections.Array<Node> ServerHiddenNodes;
    
    // EVENTS //
    public event EntityDestroyEventHandler DestroyedEvent;
}