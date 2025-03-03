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
using Lavender.Common.Harvestables;
using Lavender.Common.Mapping;
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
    public bool IsDualManagerConnected { get; protected set; } = true;
    public bool IsDebugMode { get; protected set; } = false;

    protected readonly Dictionary<uint, INetNode> SpawnedNodes = new();
    
    protected readonly Dictionary<uint, IHarvestableNode> HarvestableNodes = new();
    
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
    public GameMap CurrentMap { get; protected set; }
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
        
        bool isProperlyExported = OS.HasFeature("server") || OS.HasFeature("client");

        if (isProperlyExported)
        {
            if (OS.HasFeature("server"))
                IsServer = true;

            if (OS.HasFeature("client"))
                IsServer = false;

            IsDebugMode = false;
        }
        else
        {
            IsDebugMode = true;
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

        try
        {
            _netManager.PollEvents();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ERR][GameManager._Process(): Networking Tick] {ex}");
        }
        
        _deltaTimer += delta;

        try
        {
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
        catch (Exception ex)
        {
            GD.PrintErr($"[ERR][GameManager._Process(): Networking Logic Tick] {ex}");
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

        CurrentMap = (GameMap)CurrentMapRootNode;
    }
    
    /// <summary>
    /// Initializes a given player to sync them with the current world state on their initial join
    /// </summar1y>
    public void InitPlayerController(PlayerController playerController)
    {
        if (IsClient)
            return;
        
        // Tell the just-joined-player AKA newPlayer about all other existing entities
        // and their position, rotation, etc.
        if (!IsDualManager || IsDualManagerConnected)
        {
            SendPacketToClientOrdered(new WorldSetupPacket()
            {
                worldName = DefaultMapName,
            }, playerController.NetId);
        }

        foreach (KeyValuePair<uint,IController> pair in SpawnedControllers)
        {
            uint pairNetId = pair.Key;
            if (playerController.NetId == pairNetId)
                continue;

            SendPacketToClientOrdered(new SpawnControllerPacket()
            {
                NetId = pairNetId,
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
            SendPacketToClientOrdered(new EntitySetMasterControllerPacket()
            {
                MasterControllerNetId = pair.Value.NetId,
                TargetEntityNetId = (pair.Value.ReceiverEntity == null ? (uint)StaticNetId.Null : pair.Value.ReceiverEntity.NetId),
            }, playerController.NetId);
        }

        if (IsDualManager && !IsDualManagerConnected)
        {
            IsDualManagerConnected = true;
            GD.Print("Single-player User Connected.");
        }
    }
    
    public IController SpawnController(ControllerType controllerType, uint presetNetId = (uint)StaticNetId.Null)
    {
        uint spawnedNetId = presetNetId;
        if (spawnedNetId == (uint)StaticNetId.Null)
        {
            spawnedNetId = GenerateNetId();
        }
        
        while (CheckNetIdExists(spawnedNetId))
        {
            spawnedNetId = GenerateNetId();
        }

        string resPath = Register.Controllers.GetResPath(controllerType);

        if (string.IsNullOrEmpty(resPath))
            throw new Exception($"Spawned ControllerType.{controllerType.ToString()} had unknown resource path: '{resPath}'");

        Node spawnedNode = Register.Scenes.GetInstance<Node>(resPath);

        if (spawnedNode == null)
            throw new Exception($"Spawned ControllerType.{controllerType.ToString()} gave a null spawnedNode!");
        
        
        if(spawnedNode is not IController controller)
            throw new Exception($"Spawned ControllerType.{controllerType.ToString()} doesnt implement IController!");

        if (spawnedNode.GetParent() != null)
            spawnedNode.GetParent().RemoveChild(spawnedNode);
        AddChild(spawnedNode);
        
        // Store controller indexed by the NetId
        SpawnedControllers.Add(spawnedNetId, controller);
        TickingControllers.Add(spawnedNetId, controller);
        SpawnedNodes.Add(spawnedNetId, controller);
        if (controller is PlayerController plrController)
            SpawnedPlayerControllers.Add(spawnedNetId, plrController);
        controller.Setup(spawnedNetId, this);
        NodeSpawnedEvent?.Invoke(controller);
        controller.DestroyedEvent += OnDestroyedTriggered;

        return controller;
    }

    public IGameEntity SpawnEntity(EntityType entityType, uint presetNetId = (uint)StaticNetId.Null)
    {
        uint spawnedNetId = presetNetId;
        if (spawnedNetId == (uint)StaticNetId.Null)
        {
            spawnedNetId = GenerateNetId();
        }
        
        while (CheckNetIdExists(spawnedNetId))
        {
            spawnedNetId = GenerateNetId();
        }

        string resPath = Register.Entities.GetResPath(entityType);

        if (string.IsNullOrEmpty(resPath))
            throw new Exception($"Spawned EntityType.{entityType.ToString()} had unknown resource path: '{resPath}'");

        Node spawnedNode = Register.Scenes.GetInstance<Node>(resPath);

        if (spawnedNode == null)
            throw new Exception($"Spawned EntityType.{entityType.ToString()} gave a null spawnedNode!");
        
        
        if(spawnedNode is not IGameEntity entity)
            throw new Exception($"Spawned EntityType.{entityType.ToString()} doesnt implement IGameEntity!");
        
        if (spawnedNode.GetParent() != null)
            spawnedNode.GetParent().RemoveChild(spawnedNode);
        AddChild(spawnedNode);
        
        // Store entity indexed by the NetId
        SpawnedEntities.Add(spawnedNetId, entity);
        SpawnedNodes.Add(spawnedNetId, entity);
        entity.Setup(spawnedNetId, this);
        NodeSpawnedEvent?.Invoke(entity);
        
        if (entity is BasicEntityBase basicEntity)
        {
            basicEntity.DestroyedEvent += OnDestroyedTriggered;
        }

        return entity;
    }

    public IController SpawnBundledEntity(EntityType entityType, uint presetNetId = (uint)StaticNetId.Null)
    {
        uint spawnedNetId = presetNetId;
        if (spawnedNetId == (uint)StaticNetId.Null)
        {
            spawnedNetId = GenerateNetId();
        }
        while (CheckNetIdExists(spawnedNetId))
        {
            spawnedNetId = GenerateNetId();
        }

        IController spawnedController = SpawnController(Register.ControlledEntities.GetControllerFor(entityType), presetNetId);
        IGameEntity spawnedEntity = SpawnEntity(entityType);

        if (!IsClient)
        {
            spawnedController.SetControlling(spawnedEntity);
            spawnedEntity.SetMasterController(spawnedController);
            spawnedController.RespawnReceiver();
        }

        return spawnedController;
    }

    public TController SpawnBundledEntity<TController>(EntityType entityType, uint presetNetId = 0) where TController : IController
    {
        return (TController)SpawnBundledEntity(entityType, presetNetId);
    }

    public TController SpawnController<TController>(ControllerType controllerType, uint presetNetId = 0) where TController : IController
    {
        return (TController)SpawnController(controllerType, presetNetId);
    }

    public TEntity SpawnEntity<TEntity>(EntityType entityType, uint presetNetId = 0) where TEntity : IGameEntity
    {
        return (TEntity)SpawnEntity(entityType, presetNetId);
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
        SendPacketToClientOrdered(packet, GetPeerFromNetId(netId));
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
    /// Finds and returns controller with given NetId or null
    /// </summary>
    public IController GetControllerFromNetId(uint netId)
    {
        return SpawnedControllers.GetValueOrDefault(netId);
    }

    /// <summary>
    /// Gets the total count of players in this world. WARNING: May be inaccurate on client-side!
    /// </summary>
    public int GetPlayerCount()
    {
        // If server-side; return using PlayerPeers(the raw network connections count). Client-side returns entities of type PlayerEntity in world.
        return IsServer ? PlayerPeers.Count : SpawnedPlayerControllers.Count;
    }

    public bool CheckNetIdExists(uint netId)
    {
        return HarvestableNodes.ContainsKey(netId) || SpawnedNodes.ContainsKey(netId) || (netId <= (uint)StaticNetId.Server);
    }
    
    /// <summary>
    /// Generates a new NetId and registers a harvestable node to the
    /// game manager for replication.
    /// </summary>
    public void SetupHarvestable(IHarvestableNode harvestableNode)
    {
        uint netId = GenerateNetId();
        while (CheckNetIdExists(netId))
        {
            netId = GenerateNetId();
        }

        harvestableNode.Setup(netId, CurrentMap);
        
        HarvestableNodes.Add(netId, harvestableNode);
    }

    public virtual void BroadcastNotification(string message, float showTime = 4f) { }
    
    
    // EVENT LISTENERS //
    private void OnDestroyedTriggered(INetNode netNode)
    {
        if (netNode == null)
            return;

        DestroyNode(netNode);
    }
    
}