using Lavender.Common.Enums.Types;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity;

public class SpawnEntityPacket : GamePacket
{
    public uint NetId { get; set; }
    public EntityType EntityType { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put((byte)EntityType);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        EntityType = (EntityType)reader.GetByte();
    }
}