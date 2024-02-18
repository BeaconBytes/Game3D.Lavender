using System.Net;
using System.Net.Sockets;
using Godot;
using Lavender.Common.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Enums.Types;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity;
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
		_netManager.Connect(Overseer.ClientTargetIp, Overseer.ClientTargetPort, NETWORK_KEY);

		GD.Print($"Connecting to {Overseer.ClientTargetIp}:{Overseer.ClientTargetPort}...");
		
		_netListener.NetworkReceiveEvent += OnNetReceived;
		_netListener.PeerConnectedEvent += OnNetPeerConnected;
		_netListener.PeerDisconnectedEvent += OnNetPeerDisconnected;
		_netListener.NetworkErrorEvent += OnNetError;

		Register.Packets.Subscribe<IdentifyPacket>(OnIdentifyPacket);
		
		Register.Packets.Subscribe<SpawnEntityPacket>(OnSpawnEntityPacket);
		Register.Packets.Subscribe<DestroyEntityPacket>(OnDestroyEntityPacket);
		
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
		
		IGameEntity gameEntity = SpawnEntity(toSpawnType, packet.NetId);
		
		if (packet.NetId == ClientNetId)
		{
			ClientEntity = gameEntity;
		}
		
		GD.Print($"Spawned EntityType.{toSpawnType}");
	}
	private void OnDestroyEntityPacket(DestroyEntityPacket packet, uint sourceNetId)
	{
		DestroyEntity(GetEntityFromNetId(packet.NetId));
	}
	
}
