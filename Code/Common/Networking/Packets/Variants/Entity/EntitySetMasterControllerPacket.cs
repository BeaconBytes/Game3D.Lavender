using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity;

public class EntitySetMasterControllerPacket : GamePacket
{
    public uint TargetEntityNetId { get; set; }
    public uint MasterControllerNetId { get; set; }


    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(TargetEntityNetId);
        writer.Put(MasterControllerNetId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        TargetEntityNetId = reader.GetUInt();
        MasterControllerNetId = reader.GetUInt();
    }
}