using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Protocol;

public class WorldSetupPacket : GamePacket
{
    public string worldName;

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(worldName);
    }

    public override void Deserialize(NetDataReader reader)
    {
        worldName = reader.GetString();
    }
}