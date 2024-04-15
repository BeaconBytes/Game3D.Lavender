using Lavender.Common.Enums.Entity;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity.Data;

public class EntityValueChangedPacket : GamePacket
{
    public uint NetId { get; set; }
    public EntityValueChangedType ValueType { get; set; }
    public float NewValue { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put((byte)ValueType);
        writer.Put(NewValue);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        ValueType = (EntityValueChangedType)reader.GetByte();
        NewValue = reader.GetFloat();
    }
}