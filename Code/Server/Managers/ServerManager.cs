using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Entity;
using Lavender.Common.Entity.Variants;
using Lavender.Common.Enums.Types;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Networking.Packets.Variants.Other;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using Lavender.Common.Registers;
using Lavender.Common.Utils;
using LiteNetLib;

namespace Lavender.Server.Managers;

public partial class ServerManager : GameManager
{
	private const int MAX_PLAYERS_COUNT = 10;

	protected override void Load()
	{
		base.Load();
		
		TickingDisabled = false;
		
		if (!_netManager.Start(EnvManager.ServerPort))
		{
			throw new Exception($"Failed to start server: port {EnvManager.ServerPort} is likely already in use!");
		}

		_netListener.NetworkReceiveEvent += OnNetReceived;
		_netListener.ConnectionRequestEvent += OnNetConnRequested;
		_netListener.PeerConnectedEvent += OnNetPeerConnected;
		_netListener.PeerDisconnectedEvent += OnNetPeerDisconnected;

		Register.Packets.Subscribe<AuthMePacket>(OnAuthMePacket);
		
		EntitySpawnedEvent += OnEntitySpawned;
		EntityDestroyedEvent += OnEntityDestroyed;
		
		GD.Print($"Server started on port {EnvManager.ServerPort}");

		MapName = "default";
		
		// Register.Packets.Subscribe<DebugActionPacket>(OnDebugActionPacket);

		LoadMapByName(MapName);
		WaveManager.Setup(this);
		WaveManager.StartWave();
	}

	/// <summary>
	/// Called when this Manager is unloaded
	/// </summary>
	protected override void Unload()
	{
		_netListener.NetworkReceiveEvent -= OnNetReceived;
		_netListener.ConnectionRequestEvent -= OnNetConnRequested;
		_netListener.PeerConnectedEvent -= OnNetPeerConnected;
		_netListener.PeerDisconnectedEvent -= OnNetPeerDisconnected;
		
		EntitySpawnedEvent -= OnEntitySpawned;
		EntityDestroyedEvent -= OnEntityDestroyed;

		_netManager.Stop();
		base.Unload();
	}

	protected override void ApplyRegistryDefaults()
	{
		base.ApplyRegistryDefaults();
	}

	private void OnNetReceived(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
	{
		PacketType packetType = (PacketType)reader.GetByte();
		
		uint sourceNetId = GetNetIdFromPeer(peer);
		
		Register.Packets.InvokeSubscriberEvent(packetType, reader, sourceNetId);
	}

	private void OnNetConnRequested(ConnectionRequest request)
	{
		if (EnvManager.IsSinglePlayer && PlayerPeers.Count > 0)
		{
			request.RejectForce();
			return;
		}
		if (PlayerPeers.Count < MAX_PLAYERS_COUNT)
		{
			request.AcceptIfKey(NETWORK_KEY);
			return;
		}

		request.Reject();
	}
	private void OnNetPeerConnected(NetPeer peer)
	{
		uint clientNetId = GenerateNetId();
		while (SpawnedEntities.ContainsKey(clientNetId))
		{
			clientNetId = GenerateNetId();
		}

		GD.Print("Connection started...");

		PlayerPeers.Add(peer, clientNetId);

		SendPacketToClient(new IdentifyPacket()
		{
			NetId = clientNetId,
		}, peer);
	}
	private void OnNetPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		IGameEntity ent = GetEntityFromPeer(peer);
		if (ent == null)
			return;
		
		GD.Print($"Player({ent.DisplayName})[{ent.NetId}] Disconnected...");
		uint entNetId = ent.NetId;

		PlayerEntities.Remove(entNetId);
		PlayerPeers.Remove(peer);

		DestroyEntity(ent);
		
		BroadcastPacketToClients( new DestroyEntityPacket()
		{
			NetId = entNetId,
		});
	}

	
	
	
	private void OnAuthMePacket(AuthMePacket packet, uint sourceNetId)
	{
		string name = StringUtils.Sanitize(packet.Username);

		PlayerEntity playerSpawned = SpawnEntity<PlayerEntity>(EntityType.Player, sourceNetId);
		playerSpawned.SetName(name); 
		
		SetupPlayer(playerSpawned);
		
		GD.Print($"Player AuthMe'd with name of {playerSpawned.DisplayName}");
	}
	private void OnDebugActionPacket(DebugActionPacket packet, uint sourceNetId)
	{
		// if (packet.Message.ToLower().Equals("debug"))
		// {
		// 	if (packet.Augment == 0)
		// 	{
		// 		LighthouseEntity ent = SpawnEntity<LighthouseEntity>(EntityType.Lighthouse);
		// 		ent.Teleport(new Vector3(1, 3, 1));
		// 		ent.SetDesiredPathLocation(new Vector3(0f, -10f, 0f));
		// 	}
		// }
	}
	
	

	/// <summary>
	/// Initializes a given player to sync them with the current world state on their initial join
	/// </summary>
	protected void SetupPlayer(PlayerEntity newPlayer)
	{
		// Tell the just-joined-player AKA newPlayer about all other existing entities
		// and their position, rotation, etc.
		if (!IsDualManager)
		{
			SendPacketToClient(new WorldSetupPacket()
			{
				worldName = MapName,
			}, newPlayer.NetId);
		}
		newPlayer.Teleport(MapManager.GetRandomPlayerSpawnPoint());

		foreach (KeyValuePair<uint, IGameEntity> pair in SpawnedEntities)
		{
			uint pairNetId = pair.Key;
			if (newPlayer.NetId == pairNetId)
				continue;
			
			Node3D pairNode = (Node3D)pair.Value;
			
			Vector3 sendingRotation = pair.Value.WorldRotation;
			if (pairNode is HumanoidEntity pairHumanoid)
			{
				sendingRotation = pairHumanoid.GetRotationWithHead();
			}
			
			SendPacketToClient(new SpawnEntityPacket()
			{
				NetId = pairNetId,
				EntityType = Register.Entities.GetEntityType(pair.Value),
			}, newPlayer.NetId);

			
			SendPacketToClient(new EntityTeleportPacket()
			{
				NetId = pairNetId,
				Position = pairNode.GlobalPosition,
			}, newPlayer.NetId);
			SendPacketToClient(new EntityRotatePacket()
			{
				NetId = pairNetId,
				Rotation = sendingRotation,
			}, newPlayer.NetId);
		}

		// DevEntity devEnt = SpawnEntity<DevEntity>(EntityType.Dev);
		// devEnt.SetOwner(newPlayer);
		// devEnt.Teleport(CurrentMapManager.GetRandomPlayerSpawnPoint());
	}

	/// <summary>
	/// Event response method responsible for updating all clients when a entity is spawned server-side
	/// </summary>
	private void OnEntitySpawned(IGameEntity gameEntity)
	{
		uint netId = gameEntity.NetId;

		BroadcastPacketToClients(new SpawnEntityPacket()
		{
			NetId = netId,
			EntityType = Register.Entities.GetEntityType(gameEntity),
		});
		BroadcastPacketToClients(new EntityTeleportPacket()
		{
			NetId = netId,
			Position = gameEntity.WorldPosition,
		});

		if (gameEntity is HumanoidEntity humanoidEntity)
		{
			Vector3 sendingRotation = humanoidEntity.GetRotationWithHead();
			BroadcastPacketToClients(new EntityRotatePacket()
			{
				NetId = netId,
				Rotation = sendingRotation,
			});
		}
		else if (gameEntity is LivingEntity livingEntity)
		{
			BroadcastPacketToClients(new EntityRotatePacket()
			{
				NetId = netId,
				Rotation = livingEntity.GlobalRotation,
			});
		}

		if (gameEntity is PlayerEntity playerEntity)
		{
			PlayerEntities.Add(netId, playerEntity);
		}
	}
	private void OnEntityDestroyed(IGameEntity gameEntity)
	{
		uint netId = gameEntity.NetId;

		BroadcastPacketToClients(new DestroyEntityPacket()
		{
			NetId = netId,
		});
	}

	protected string MapName = "default";
}
