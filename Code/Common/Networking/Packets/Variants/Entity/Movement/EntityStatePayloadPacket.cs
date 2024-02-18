using Lavender.Common.Entity.Data;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity.Movement;

public class EntityStatePayloadPacket : GamePacket
{
    public uint NetId;
    public StatePayload StatePayload;

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(StatePayload.tick);
        writer.Put(StatePayload.position);
        writer.Put(StatePayload.rotation);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        StatePayload = new()
        {
            tick = reader.GetUInt(),
            position = reader.GetVector3(),
            rotation = reader.GetVector3(),
        };
    }
}