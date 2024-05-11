using Lavender.Common.Enums.Types;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Controller;

public class SpawnControllerPacket : GamePacket
{
    public uint NetId { get; set; }
    public ControllerType ControllerType { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put((byte)ControllerType);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        ControllerType = (ControllerType)reader.GetByte();
    }
}