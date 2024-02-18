using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity.Movement;

public class EntityInputPayloadPacket : GamePacket
{
    public uint NetId;
    public InputPayload InputPayload;

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(InputPayload.tick);
        writer.Put(InputPayload.moveInput);
        writer.Put(InputPayload.lookInput);
        writer.Put((byte)InputPayload.flagsInput);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        InputPayload = new InputPayload()
        {
            tick = reader.GetUInt(),
            moveInput = reader.GetVector3(),
            lookInput = reader.GetVector3(),
            flagsInput = (EntityMoveFlags)reader.GetByte(),
        };
    }
}