using Lavender.Common.Enums.Net;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Controller;

public class SetControllingPacket : GamePacket
{
    public uint ControllerNetId { get; set; }
    public uint? ReceiverNetId { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(ControllerNetId);
        writer.Put(ReceiverNetId ?? (uint)StaticNetId.Null);
    }

    public override void Deserialize(NetDataReader reader)
    {
        ControllerNetId = reader.GetUInt();
        uint readReceiverNetId = reader.GetUInt();
        ReceiverNetId = readReceiverNetId == (uint)StaticNetId.Null ? null : readReceiverNetId;
    }
}