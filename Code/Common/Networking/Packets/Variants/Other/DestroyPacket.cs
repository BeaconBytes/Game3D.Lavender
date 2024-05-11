using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Other;

public class DestroyPacket : GamePacket
{
    public uint NetId { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
    }
}