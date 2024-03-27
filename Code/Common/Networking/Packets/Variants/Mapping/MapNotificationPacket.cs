using Lavender.Common.Utils;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Mapping;

public class MapNotificationPacket : GamePacket
{
    public string Message { get; set; }
    public float TimeLengthSeconds { get; set; } = 4f;

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(StringUtils.Sanitize(Message, 64));
        writer.Put(TimeLengthSeconds);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Message = reader.GetString();
        TimeLengthSeconds = reader.GetFloat();
    }
}