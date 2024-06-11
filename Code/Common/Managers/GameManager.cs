using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Lavender.Client.Managers;
using Lavender.Common.Controllers;
using Lavender.Common.Entity;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.Net;
using Lavender.Common.Enums.Types;
using Lavender.Common.Exceptions;
using Lavender.Common.Networking.Packets;
using Lavender.Common.Networking.Packets.Variants.Controller;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using Lavender.Common.Registers;
using Lavender.Server.Managers;
using LiteNetLib;
using LiteNetLib.Utils;
using BasicEntityBase = Lavender.Common.Entity.GameEntities.BasicEntityBase;
using Environment = System.Environment;
using HumanoidEntityBase = Lavender.Common.Entity.GameEntities.HumanoidEntityBase;

namespace Lavender.Common.Managers;

/// <summary>
/// Essentially a Game-Play & Game-State manager class.
/// </summary>
public partial class GameManager : LoadableNode
{
    protected virtual void ApplyRegistryDefaults()
    {
        
    }
    
    
    protected override void Load()
    {
        base.Load();

        if (this is ClientManager)
        {
            IsClient = true;
            IsServer = false;
        }
        else if (this is ServerManager)
        {
            IsServer = true;
            IsClient = false;
        }
        
        EnvManager = GetTree().CurrentScene.GetNode<EnvManager>("EnvManager");
        if (EnvManager == null)
            throw new BadNodeSetupException("EnvManager not found!");
        
        IsDualManager = EnvManager.IsDualManagers;
        
        ApplyRegistryDefaults();

        TickingDisabled = true;
        
        _netManager = new NetManager(_netListener)
        {
            IPv6Enabled = false,
        };
    }

    protected override void Unload()
    {
        base.Unload();
        TickingDisabled = true;
    }


    /// <summary>
    /// Called on the very first tick that _Process is called, at HEAD
    /// </summary>
    protected virtual void Start() { }
    
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_isFirstTick)
        {
            _isFirstTick = false;
            Start();
        }
        
        if (TickingDisabled)
            return;
        
        _netManager.PollEvents();
        
        _deltaTimer += delta;

        while (_deltaTimer >= NET_TICK_TIME)
        {
            _deltaTimer -= NET_TICK_TIME;
            foreach (KeyValuePair<uint,IController> pair in TickingControllers)
            {
                if (pair.Value.Destroyed)
                    continue;
                pair.Value.NetworkProcess(NET_TICK_TIME);
            }
            CurrentTick++;
        }
    }

    protected void LoadMapByName(string name)
    {
        if (MapSocketNode == null)
            throw new Exception($"Failed to start: MapSocketNode isnt set!");
        
        var newMapPath = Register.SceneTable[$"map_{name.ToLower()}"];
        if (MapSocketNode != null && CurrentMapRootNode != null)
        {
            MapSocketNode.RemoveChild(CurrentMapRootNode);
            CurrentMapRootNode.Free();
        }
        
        PackedScene newScene = (PackedScene)GD.Load(newMapPath);
        if (newScene == null)
        {
            throw new Exception($"Failed to start server: No map found at path '{newMapPath}'");
        }

        CurrentMapRootNode = newScene.Instantiate<Node>();
        MapSocketNode.AddChild(CurrentMapRootNode);

        MapManager = (MapManager)CurrentMapRootNode;
    }
    
    /// <summary>
    /// Initializes a given player to sync them with the current world state on their initial join
    /// </summary>
    public void InitPlayerController(PlayerController playerController)
    {
        if (IsClient)
            return;
        
        // Tell the just-joined-player AKA newPlayer about all other existing entities
        // and their position, rotation, etc.
        if (!IsDualManager)
        {
            SendPacketToClientOrdered(new WorldSetupPacket()
            {
                worldName = DefaultMapName,
            }, playerController.NetId);
        }

        foreach (KeyValuePair<uint,IController> pair in SpawnedControllers)
        {
            uint pairNetid = pair.Key;
            if (playerController.NetId == pairNetid)
                continue;

            SendPacketToClientOrdered(new SpawnControllerPacket()
            {
                NetId = pairNetid,
                ControllerType = Register.Controllers.GetControllerType(pair.Value),
            }, playerController.NetId);
        }
		
        foreach (KeyValuePair<uint, IGameEntity> pairEntity in SpawnedEntities)
        {
            uint pairNetId = pairEntity.Key;
            if (playerController.NetId == pairNetId)
                continue;
			
            Node3D pairNode = (Node3D)pairEntity.Value;
			
            Vector3 sendingRotation = pairEntity.Value.WorldRotation;
            if (pairNode is HumanoidEntityBase pairHumanoid)
            {
                sendingRotation = pairHumanoid.GetRotationWithHead();
            }
			
            SendPacketToClientOrdered(new SpawnEntityPacket()
            {
                NetId = pairNetId,
                EntityType = Register.Entities.GetEntityType(pairEntity.Value),
            }, playerController.NetId);

			
            SendPacketToClientOrdered(new EntityTeleportPacket()
            {
                NetId = pairNetId,
                Position = pairNode.GlobalPosition,
            }, playerController.NetId);
            SendPacketToClientOrdered(new EntityRotatePacket()
            {
                NetId = pairNetId,
                Rotation = sendingRotation,
            }, playerController.NetId);
        }

        foreach (KeyValuePair<uint, IController> pair in SpawnedControllers)
        {
            SendPacketToClientOrdered(new SetControllingPacket()
            {
                ControllerNetId = pair.Value.NetId,
                ReceiverNetId = pair.Value.ReceiverEntity?.NetId,
            }, playerController.NetId);
        }
		
    }
    
    protected IController SpawnController(ControllerType controllerType, bool spawnLinkedEntity = false, uint presetNetId = (uint)StaticNetId.Null)
    {
        uint spawnedNetId = presetNetId;
        if (spawnedNetId == (uint)StaticNetId.Null)
        {
            spawnedNetId = GenerateNetId();
        }
        
        while (SpawnedNodes.ContainsKey(spawnedNetId))
        {
            spawnedNetId = GenerateNetId();
        }

        string resPath = Register.Controllers.GetResPath(controllerType);

        if (string.IsNullOrEmpty(resPath))
            throw new Exception($"Invalid resource path: '{resPath}'");

        Node spawnedNode = Register.Scenes.GetInstance<Node>(resPath);

        if (spawnedNode == null)
            throw new Exception("Null spawnedNode!");

        if (spawnedNode is not IController spawnedController)
            throw new Exception($"Spawned ControllerType.{controllerType.ToString()} doesn't inherit IController!{Environment.NewLine}Type name is '{spawnedNode.GetClass().GetBaseName()}'");

        spawnedController.Setup(spawnedNetId, this);

        SpawnedNodes.Add(spawnedNetId, spawnedController);
        SpawnedControllers.Add(spawnedNetId, spawnedController);
        if (spawnedController is PlayerController playerController)
        {
            SpawnedPlayerControllers.Add(spawnedController.NetId, playerController);
        }

        AddChild(spawnedNode);
        spawnedNode.Name = $"Controller#{spawnedNetId}";

        NodeSpawnedEvent?.Invoke(spawnedController);

        spawnedController.DestroyedEvent += OnDestroyedTriggered;

        if (spawnLinkedEntity && !IsClient)
        {
            EntityType entityType = Register.Entities.GetEntityType(spawnedController);
            IGameEntity spawnedEntity = SpawnEntity(entityType);
            spawnedController.SetControlling(spawnedEntity);
            spawnedController.RespawnReceiver();
        }

        return spawnedController;
    }
    protected IGameEntity SpawnEntity(EntityType entityType, uint presetNetId = (uint)StaticNetId.Null)
    {
        uint spawnedNetId = presetNetId;
        if (spawnedNetId == (uint)StaticNetId.Null)
        {
            spawnedNetId = GenerateNetId();
        }
        
        while (SpawnedNodes.ContainsKey(spawnedNetId))
        {
            spawnedNetId = GenerateNetId();
        }

        string resPath = Register.Entities.GetEntityResPath(entityType);

        if (string.IsNullOrEmpty(resPath))
            throw new Exception($"Invalid resource path: '{resPath}'");

        Node3D spawnedNode = Register.Scenes.GetInstance<Node3D>(resPath);

        if (spawnedNode == null)
            throw new Exception("Null spawnedNode!");

        if (spawnedNode is not IGameEntity gameEntity)
            throw new Exception($"Spawned EntityType.{entityType.ToString()} doesn't inherit IGameEntity!{Environment.NewLine}Type name is '{spawnedNode.GetClass().GetBaseName()}'");

        gameEntity.Setup(spawnedNetId, this);

        SpawnedEntities.Add(spawnedNetId, gameEntity);
        SpawnedNodes.Add(spawnedNetId, gameEntity);

        AddChild(spawnedNode);
        spawnedNode.Name = $"Entity#{spawnedNetId}";

        NodeSpawnedEvent?.Invoke(gameEntity);

        if (gameEntity is BasicEntityBase basicEntity)
        {
            basicEntity.DestroyedEvent += OnDestroyedTriggered;
        }
        
        return gameEntity;
    }

    public TEntity SpawnEntity<TEntity>(EntityType entityType, uint presetNetId = 0) where TEntity : IGameEntity
    {
        return (TEntity)SpawnEntity(entityType, presetNetId);
    }
    public TController SpawnController<TController>(ControllerType controllerType, bool spawnLinkedEntity = false, uint presetNetId = 0) where TController : IController
    {
        return (TController)SpawnController(controllerType, spawnLinkedEntity, presetNetId);
    }

    public void DestroyNode(INetNode netNode)
    {
        bool foundSpawned = SpawnedNodes.ContainsKey(netNode.NetId);

        if (netNode is IController)
        {
            if (netNode is PlayerController)
                SpawnedPlayerControllers.Remove(netNode.NetId);
            SpawnedControllers.Remove(netNode.NetId);
            TickingControllers.Remove(netNode.NetId);
        }
        else if (netNode is IGameEntity)
            SpawnedEntities.Remove(netNode.NetId);
        SpawnedNodes.Remove(netNode.NetId);
        
        if (netNode is BasicEntityBase basicEntity)
        {
            basicEntity.DestroyedEvent -= OnDestroyedTriggered;
        }
        NodeDestroyedEvent?.Invoke(netNode);

        // An attempt at preventing multiple destroy calls and potential future infinite looping
        if (foundSpawned)
        {
            if(netNode is IGameEntity gameEntity)
                gameEntity.Destroy();

            if (netNode is Node node)
            {
                RemoveChild(node);
                node.QueueFree();
            }
        }
    }
    
    public void SendPacketToClient(GamePacket packet, uint netId)
    {
        SendPacketToClient( packet, GetPeerFromNetId( netId ) );
    }

    public void SendPacketToClientOrdered(GamePacket packet, uint netId)
    {
        SendPacketToClient(packet, GetPeerFromNetId(netId));
    }
    public void SendPacketToClient(GamePacket packet, NetPeer peer)
    {
        if (IsClient)
            throw new Exception("GameManger#SendPacketToClient() was called client-side!");
        
        peer.Send( WritePacketSerial( packet ), DeliveryMethod.ReliableUnordered );
    }

    public void SendPacketToClientOrdered(GamePacket packet, NetPeer peer)
    {
        if (IsClient)
            throw new Exception("GameManger#SendPacketToClientOrdered() was called client-side!");
        
        peer.Send(WritePacketSerial(packet), DeliveryMethod.ReliableOrdered);
    }
    /// <summary>
    /// Sends given packet to ALL PlayerEntity in _playerEntities using RELIABLE/UNORDERED, skipping peerToSkip
    /// </summary>
    public void BroadcastPacketToClients(GamePacket packet, NetPeer peerToSkip = null)
    {
        foreach (var pair in PlayerPeers.Where(pair => pair.Key != peerToSkip))
        {
            SendPacketToClient( packet, pair.Key );
        }
    }
    
    /// <summary>
    /// Sends given packet to ALL PlayerEntity in _playerEntities using RELIABLE/ORDERED, skipping peerToSkip
    /// </summary>
    public void BroadcastPacketToClientsOrdered(GamePacket packet, NetPeer peerToSkip = null)
    {
        foreach (KeyValuePair<NetPeer,uint> pair in PlayerPeers.Where(pair => pair.Key != peerToSkip))
        {
            SendPacketToClientOrdered(packet, pair.Key);
        }
    }
    
    public void SendPacketToServer(GamePacket packet)
    {
        if (!IsClient)
            throw new Exception("GameManager#SendPacketToServer() was called in server-side!");
        
        ServerPeer?.Send(WritePacketSerial(packet), DeliveryMethod.ReliableUnordered);
    }


    private NetDataWriter WritePacketSerial(GamePacket packet)
    {
        _netWriterCached.Reset();
        _netWriterCached.Put((byte)Register.Packets.GetPacketType(packet));
        packet.Serialize(_netWriterCached);
        return _netWriterCached;
    }
    

    /// <summary>
    /// Generates a random NetId with 0 reserved as null, and 1 reserved as server
    /// </summary>
    /// <returns></returns>
    public uint GenerateNetId( )
    {
        return ( GD.Randi( ) % ( uint.MaxValue - 2 ) ) + 2;
    }
    
    
    protected IGameEntity GetEntityFromPeer(NetPeer peer)
    {
        return GetEntityFromNetId(PlayerPeers[peer]);
    }
    protected NetPeer GetPeerFromEntity(IGameEntity gameEntity)
    {
        if (gameEntity == null)
            return null;
			
        return PlayerPeers.First(x => x.Value == gameEntity.NetId).Key;
    }
    protected NetPeer GetPeerFromNetId(uint netId)
    {
        if ( netId == 0 )
            return null;

        return PlayerPeers.First(x => x.Value == netId).Key;
    }
    protected uint GetNetIdFromPeer(NetPeer peer)
    {
        return PlayerPeers[peer];
    }
    public IGameEntity GetEntityFromNetId(uint netId)
    {
        return SpawnedEntities.GetValueOrDefault(netId);
    }

    /// <summary>
    /// Gets the total count of players in this world. WARNING: May be inaccurate on client-side!
    /// </summary>
    public int GetPlayerCount()
    {
        // If server-side; return using PlayerPeers(the raw network connections count). Client-side returns entities of type PlayerEntity in world.
        return IsServer ? PlayerPeers.Count : SpawnedPlayerControllers.Count;
    }

    public virtual void BroadcastNotification(string message, float showTime = 4f) { }
    
    
    // EVENT LISTENERS //
    private void OnDestroyedTriggered(INetNode netNode)
    {
        if (netNode == null)
            return;

        DestroyNode(netNode);
    }
    
    

    protected const string NETWORK_KEY = "LavendarKey787";

    public const double SERVER_TICK_RATE = 30d;
    public const double NET_TICK_TIME = 1d / SERVER_TICK_RATE;
    public const int NET_BUFFER_SIZE = 1024;

    private double _deltaTimer = 0;
    public uint CurrentTick { get; private set; } = 0;
    
    public EnvManager EnvManager { get; private set; }

    public bool IsClient { get; private set; } = false;
    public bool IsServer { get; private set; } = false;
    public bool IsDualManager { get; protected set; } = true;

    protected readonly Dictionary<uint, INetNode> SpawnedNodes = new();
    protected readonly Dictionary<uint, IGameEntity> SpawnedEntities = new();
    protected readonly Dictionary<uint, IController> SpawnedControllers = new();
    protected readonly Dictionary<uint, IController> TickingControllers = new();
    protected readonly Dictionary<uint, PlayerController> SpawnedPlayerControllers = new();
    
    protected readonly Dictionary<NetPeer, uint> PlayerPeers = new();

    private readonly NetDataWriter _netWriterCached = new();
    protected readonly EventBasedNetListener _netListener = new();
    protected NetManager _netManager;
    
    // The NetPeer of the Server(for client-side)
    public NetPeer ServerPeer { get; protected set; }
    public uint ClientNetId { get; protected set; } = (uint)StaticNetId.Null;
    public PlayerController ClientController { get; protected set; }
    
    [Export]
    protected Node MapSocketNode;
    [Export]
    protected Node CurrentMapRootNode;
    public MapManager MapManager { get; protected set; }
    public PathManager PathManager { get; protected set; }

    public bool TickingDisabled { get; protected set; } = true;
    
    public string DefaultMapName { get; protected set; } = "default";

    private bool _isFirstTick = true;


    // EVENT SIGNATURES //
    public delegate void SimpleNetNodeEventHandler(INetNode target);
    public delegate void SourcedNetNodeEventHandler(INetNode source, INetNode target);
    
    
    // EVENTS //
    public event SimpleNetNodeEventHandler NodeSpawnedEvent;
    public event SimpleNetNodeEventHandler NodeDestroyedEvent;
}