using System;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets;

public class GamePacket
{
    public delegate void OnPacketEvent(GamePacket gamePacket, uint sourceNetId);

    public event OnPacketEvent PacketEvent;
    
    public virtual void TriggerHandler(NetDataReader reader, uint sourceNetId)
    {
        Deserialize(reader);
        NetPacketProcessor pro = new();
        PacketEvent?.Invoke(this, sourceNetId);
    }

    public virtual void Serialize(NetDataWriter writer)
    {
        throw new NotImplementedException();
    }

    public virtual void Deserialize(NetDataReader reader)
    {
        throw new NotImplementedException();
    }
}
