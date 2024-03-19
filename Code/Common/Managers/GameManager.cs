using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Lavender.Client.Managers;
using Lavender.Common.Entity;
using Lavender.Common.Entity.Variants;
using Lavender.Common.Enums.Net;
using Lavender.Common.Enums.Types;
using Lavender.Common.Exceptions;
using Lavender.Common.Managers.Variants;
using Lavender.Common.Networking.Packets;
using Lavender.Common.Registers;
using Lavender.Server.Managers;
using LiteNetLib;
using LiteNetLib.Utils;
using Environment = System.Environment;

namespace Lavender.Common.Managers;

/// <summary>
/// Essentially a Game-Play & Game-State manager class.
/// </summary>
public partial class GameManager : LoadableNode
{
    protected override void Preload()
    {
        base.Preload();
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
        
        foreach (KeyValuePair<uint,Node3D> pair in SpawnedNodes)
        {
            RemoveChild(pair.Value);
            pair.Value.QueueFree();
        }
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
        WaveManager = CurrentMapRootNode.GetNode<WaveManager>("WaveManager");
    }
    
    protected virtual void ApplyRegistryDefaults()
    {
        
    }
    
    protected IGameEntity SpawnEntity(EntityType entityType, uint presetNetId = (uint)StaticNetId.Null)
    {
        uint spawnedNetId = presetNetId;
        if (spawnedNetId == (uint)StaticNetId.Null)
        {
            spawnedNetId = GenerateNetId();
        }
        
        while (SpawnedEntities.ContainsKey(spawnedNetId))
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

        AddChild(spawnedNode);

        EntitySpawnedEvent?.Invoke(gameEntity);

        if (gameEntity is BasicEntity basicEntity)
        {
            basicEntity.DestroyedEvent += OnDestroyedTriggered;
        }

        return gameEntity;
    }

    public TEntity SpawnEntity<TEntity>(EntityType entityType, uint presetNetId = 0) where TEntity : IGameEntity
    {
        return (TEntity)SpawnEntity(entityType, presetNetId);
    }

    public void DestroyEntity(IGameEntity gameEntity)
    {
        SpawnedEntities.Remove(gameEntity.NetId);

        if (gameEntity is BasicEntity basicEntity)
        {
            basicEntity.DestroyedEvent -= OnDestroyedTriggered;
        }
        EntityDestroyedEvent?.Invoke(gameEntity);

        // An attempt at preventing multiple destroy calls and potential future infinite looping
        if (!gameEntity.Destroyed)
        {
            gameEntity.Destroy();
            Node3D tarNode = (Node3D)gameEntity;
            RemoveChild(tarNode);
            tarNode.QueueFree();
        }
        
    }
    
    public void SendPacketToClient(GamePacket packet, uint netId)
    {
        SendPacketToClient( packet, GetPeerFromNetId( netId ) );
    }
    public void SendPacketToClient(GamePacket packet, IGameEntity gameEntity)
    {
        SendPacketToClient( packet, GetPeerFromEntity( gameEntity ) );
    }
    public void SendPacketToClient(GamePacket packet, NetPeer peer)
    {
        if (IsClient)
        {
            GD.PrintErr("SendPacketToClient was called in client-side!");
            return;
        }
        peer.Send( WritePacketSerial( packet ), DeliveryMethod.ReliableUnordered );
    }
    /// <summary>
    /// Sends given packet to ALL PlayerEntity in _playerEntities using RELIABLE, skipping peerToSkip
    /// </summary>
    public void BroadcastPacketToClients(GamePacket packet, NetPeer peerToSkip = null)
    {
        foreach (var pair in PlayerPeers.Where(pair => pair.Key != peerToSkip))
        {
            SendPacketToClient( packet, pair.Key );
        }
    }
    
    public void SendPacketToServer(GamePacket packet)
    {
        if (!IsClient)
        {
            GD.PrintErr("SendPacketToServer was called in server-side!");
            return;
        }
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
    
    
    public IGameEntity GetEntityFromPeer(NetPeer peer)
    {
        return GetEntityFromNetId(PlayerPeers[peer]);
    }
    public NetPeer GetPeerFromEntity(IGameEntity gameEntity)
    {
        if (gameEntity == null)
            return null;
			
        return PlayerPeers.First(x => x.Value == gameEntity.NetId).Key;
    }
    public NetPeer GetPeerFromNetId(uint netId)
    {
        if ( netId == 0 )
            return null;

        return PlayerPeers.First(x => x.Value == netId).Key;
    }
    public uint GetNetIdFromPeer(NetPeer peer)
    {
        return PlayerPeers[peer];
    }
    public IGameEntity GetEntityFromNetId(uint netId)
    {
        return SpawnedEntities[netId];
    }

    
    /// <summary>
    /// Gets a array of all entity's of type PlayerEntity currently spawned in the world. WARNING: Inefficient
    /// </summary>
    public PlayerEntity[] GetPlayers()
    {
        if (PlayerEntities.Count == 0)
            return null;

        List<PlayerEntity> playersList = new();
        foreach (KeyValuePair<uint,PlayerEntity> pair in PlayerEntities)
        {
            playersList.Add(pair.Value);
        }

        return playersList.ToArray();
    }

    /// <summary>
    /// Gets the total count of players in this world. WARNING: May be inaccurate on client-side!
    /// </summary>
    public int GetPlayerCount()
    {
        // If server-side; return using PlayerPeers(the raw network connections count). Client-side returns entities of type PlayerEntity in world.
        return IsServer ? PlayerPeers.Count : PlayerEntities.Count;
    }
    
    
    // EVENT LISTENERS //
    private void OnDestroyedTriggered(IGameEntity sourceEntity)
    {
        if (sourceEntity == null)
            return;

        DestroyEntity(sourceEntity);
    }
    
    

    protected const string NETWORK_KEY = "LavendarKey787";

    public EnvManager EnvManager { get; private set; }

    public bool IsClient { get; private set; } = false;
    public bool IsServer { get; private set; } = false;
    public bool IsDualManager { get; protected set; } = true;

    protected readonly Dictionary<uint, Node3D> SpawnedNodes = new ();
    protected readonly Dictionary<uint, IGameEntity> SpawnedEntities = new();
    
    protected readonly Dictionary<NetPeer, uint> PlayerPeers = new();
    protected readonly Dictionary<uint, PlayerEntity> PlayerEntities = new();

    private readonly NetDataWriter _netWriterCached = new();
    protected readonly EventBasedNetListener _netListener = new();
    protected NetManager _netManager;
    
    // The NetPeer of the Server(for client-side)
    public NetPeer ServerPeer { get; protected set; }
    public uint ClientNetId { get; protected set; } = (uint)StaticNetId.Null;
    public IGameEntity ClientEntity { get; protected set; }
    
    [Export]
    protected Node MapSocketNode;
    [Export]
    protected Node CurrentMapRootNode;
    public MapManager MapManager { get; protected set; }
    public WaveManager WaveManager { get; protected set; }
    public PathManager PathManager { get; protected set; }

    public bool TickingDisabled { get; protected set; } = true;

    private bool _isFirstTick = true;


    // EVENT HANDLERS //
    public delegate void SimpleEntityEventHandler(IGameEntity gameEntity);
    
    
    // EVENTS //
    public event SimpleEntityEventHandler EntitySpawnedEvent;
    public event SimpleEntityEventHandler EntityDestroyedEvent;
}