using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity;

public class EntitySetGrabPacket : GamePacket
{
    public uint SourceNetId { get; set; }
    public uint TargetNetId { get; set; }
    public bool IsRelease { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(SourceNetId);
        writer.Put(TargetNetId);
        writer.Put(IsRelease);
    }

    public override void Deserialize(NetDataReader reader)
    {
        SourceNetId = reader.GetUInt();
        TargetNetId = reader.GetUInt();
        IsRelease = reader.GetBool();
    }
}