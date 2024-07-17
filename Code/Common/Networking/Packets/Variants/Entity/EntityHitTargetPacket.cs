using Lavender.Common.Enums.Items;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity;

public class EntityHitTargetPacket : GamePacket
{
    public uint NetId { get; set; }
    public uint TargetNetId { get; set; }
    public uint Tick { get; set; }
    public WeaponType WeaponType { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(TargetNetId);
        writer.Put(Tick);
        writer.Put((byte)WeaponType);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        TargetNetId = reader.GetUInt();
        Tick = reader.GetUInt();
        WeaponType = (WeaponType)reader.GetByte();
    }
}