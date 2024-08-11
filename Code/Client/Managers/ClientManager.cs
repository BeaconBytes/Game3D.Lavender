using System.Net;
using System.Net.Sockets;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Enums.Types;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Controller;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Other;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using Lavender.Common.Registers;
using LiteNetLib;

namespace Lavender.Client.Managers;

public partial class ClientManager : GameManager
{
	protected override void Load()
	{
		base.Load();
		
		TickingDisabled = false;
		
		_netManager.Start();
		_netManager.Connect(EnvManager.ClientTargetIp, EnvManager.ClientTargetPort, NETWORK_KEY);

		GD.Print($"Connecting to {EnvManager.ClientTargetIp}:{EnvManager.ClientTargetPort}...");
		
		_netListener.NetworkReceiveEvent += OnNetReceived;
		_netListener.PeerConnectedEvent += OnNetPeerConnected;
		_netListener.PeerDisconnectedEvent += OnNetPeerDisconnected;
		_netListener.NetworkErrorEvent += OnNetError;

		Register.Packets.Subscribe<IdentifyPacket>(OnIdentifyPacket);
		
		Register.Packets.Subscribe<SpawnEntityPacket>(OnSpawnEntityPacket);
		Register.Packets.Subscribe<SpawnControllerPacket>(OnSpawnControllerPacket);
		Register.Packets.Subscribe<DestroyPacket>(OnDestroyPacket);
		
		Register.Packets.Subscribe<WorldSetupPacket>(OnSetupWorldPacket);
	}


	/// <summary>
	/// Called when this Manager is unloaded
	/// </summary>
	protected override void Unload()
	{
		_netListener.NetworkReceiveEvent -= OnNetReceived;
		_netListener.PeerConnectedEvent -= OnNetPeerConnected;
		_netListener.PeerDisconnectedEvent -= OnNetPeerDisconnected;
		_netListener.NetworkErrorEvent -= OnNetError;
		
		_netManager.Stop();
		base.Unload();
	}

	public override void BroadcastNotification(string message, float showTime = 4)
	{
		ClientController.ClientHud.QueueNotification(message, showTime);
	}


	// EVENT HANDLERS //
	private void OnNetReceived(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
	{
		PacketType packetType = (PacketType)reader.GetByte();

		Register.Packets.InvokeSubscriberEvent(packetType, reader, (uint)StaticNetId.Server);
	}
	private void OnNetPeerConnected(NetPeer peer)
	{
		ServerPeer = peer;

		GD.Print("Connected to server.");
	}
	private void OnNetPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		GD.Print($"Lost server connection: {disconnectInfo.Reason}");
		GetTree().Quit();
	}
	private void OnNetError(IPEndPoint endPoint, SocketError socketError)
	{
		GD.Print("Lost server connection due to socket error!");
		GD.PrintErr($"SocketError thrown: {socketError}");
		GetTree().Quit();
	}

	private void OnIdentifyPacket(IdentifyPacket packet, uint sourceNetId)
	{
		if (sourceNetId != (uint)StaticNetId.Server)
			return;
		
		ClientNetId = packet.NetId;
		
		SendPacketToServer(new AuthMePacket()
		{
			Username = $"User_{ ClientNetId }",
			Password = "12345678",
		});
		
		GD.Print($"Identified as {ClientNetId}");
	}
	private void OnSetupWorldPacket(WorldSetupPacket packet, uint sourceNetId)
	{
		GD.Print($"Loading world {packet.worldName}...");
		LoadMapByName(packet.worldName);
	}
	private void OnSpawnEntityPacket(SpawnEntityPacket packet, uint sourceNetId)
	{
		EntityType toSpawnType = packet.EntityType;
		
		SpawnEntity(toSpawnType, packet.NetId);
	}
	private void OnSpawnControllerPacket(SpawnControllerPacket packet, uint sourceNetId)
	{
		ControllerType toSpawnType = packet.ControllerType;
		IController controller = SpawnController(toSpawnType, packet.NetId);
		
		if (packet.NetId == ClientNetId && controller is PlayerController playerController)
		{
			ClientController = playerController;
		}
	}
	private void OnDestroyPacket(DestroyPacket packet, uint sourceNetId)
	{
		DestroyNode(GetEntityFromNetId(packet.NetId));
	}
	
}
