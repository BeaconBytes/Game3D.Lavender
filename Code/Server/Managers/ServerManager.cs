using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common;
using Lavender.Common.Controllers;
using Lavender.Common.Entity;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.Types;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Controller;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Networking.Packets.Variants.Other;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using Lavender.Common.Registers;
using Lavender.Common.Utils;
using LiteNetLib;
using HumanoidEntityBase = Lavender.Common.Entity.GameEntities.HumanoidEntityBase;
using LivingEntityBase = Lavender.Common.Entity.GameEntities.LivingEntityBase;

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
		
		NodeSpawnedEvent += OnNodeSpawned;
		NodeDestroyedEvent += OnNodeDestroyed;
		
		GD.Print($"Server started on port {EnvManager.ServerPort}");

		DefaultMapName = "default";
		
		// Register.Packets.Subscribe<DebugActionPacket>(OnDebugActionPacket);

		LoadMapByName(DefaultMapName);
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
		
		NodeSpawnedEvent -= OnNodeSpawned;
		NodeDestroyedEvent -= OnNodeDestroyed;

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
		if (EnvManager.IsSinglePlayer && PlayerPeers.Count > 1)
		{
			request.Reject();
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
		while (CheckNetIdExists(clientNetId))
		{
			clientNetId = GenerateNetId();
		}

		GD.Print("Connection started...");

		PlayerPeers.Add(peer, clientNetId);

		SendPacketToClientOrdered(new IdentifyPacket()
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
		
		PlayerPeers.Remove(peer);

		DestroyNode(ent);
	}

	
	
	private void OnAuthMePacket(AuthMePacket packet, uint sourceNetId)
	{
		string name = StringUtils.Sanitize(packet.Username, 16, false);
		
		PlayerController playerController = SpawnBundledEntity<PlayerController>(EntityType.Player, sourceNetId);
		playerController.SetDisplayName(name);
	}
	// private void OnDebugActionPacket(DebugActionPacket packet, uint sourceNetId)
	// {
	// 	if (packet.Message.ToLower().Equals("debug"))
	// 	{
	// 		if (packet.Augment == 0)
	// 		{
	// 			// Debug button pressed?
	// 		}
	// 	}
	// 	
	// }
	
	

	/// <summary>
	/// Event response method responsible for updating all clients when a entity is spawned server-side
	/// </summary>
	private void OnNodeSpawned(INetNode netNode)
	{
		uint netId = netNode.NetId;

		if (netNode is IGameEntity gameEntity)
		{
			BroadcastPacketToClientsOrdered(new SpawnEntityPacket()
			{
				NetId = netId,
				EntityType = Register.Entities.GetEntityType(gameEntity),
			});
			BroadcastPacketToClientsOrdered(new EntityTeleportPacket()
			{
				NetId = netId,
				Position = gameEntity.WorldPosition,
			});
			
			if (gameEntity is HumanoidEntityBase humanoidEntity)
			{
				Vector3 sendingRotation = humanoidEntity.GetRotationWithHead();
				BroadcastPacketToClientsOrdered(new EntityRotatePacket()
				{
					NetId = netId,
					Rotation = sendingRotation,
				});
			}
			else if (gameEntity is LivingEntityBase livingEntity)
			{
				BroadcastPacketToClientsOrdered(new EntityRotatePacket()
				{
					NetId = netId,
					Rotation = livingEntity.GlobalRotation,
				});
			}
		}
		else if (netNode is IController controller)
		{
			BroadcastPacketToClientsOrdered(new SpawnControllerPacket()
			{
				NetId = netId,
				ControllerType = Register.Controllers.GetControllerType(controller),
			});
			if (controller is PlayerController playerController)
			{
				InitPlayerController(playerController);
			}
		}
		
	}
	
	
	// EVENT HANDLERS //
	private void OnNodeDestroyed(INetNode netNode)
	{
		uint netId = netNode.NetId;

		BroadcastPacketToClientsOrdered(new DestroyPacket()
		{
			NetId = netId,
		});
	}
}
